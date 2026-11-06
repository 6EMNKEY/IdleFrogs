using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // L'escena de la recerca: l'arbre sencer (es munta sol a partir de
    // Resources/Recerca, amb arestes entre cada node i els seus prerequisits)
    // dins d'un ScrollRect que es pot arrossegar i fer zoom, i a dalt els
    // punts de cada color. Tocar un node obre el seu panell.
    public class PantallaDeLaRecerca : MonoBehaviour
    {
        [Header("Dalt")]
        [Tooltip("Un per país, en ordre de zona: els punts del seu color")]
        [SerializeField] private TMP_Text[] textsDePunts = new TMP_Text[0];
        [SerializeField] private Button botoTancar;

        [Header("Arbre")]
        [Tooltip("El Content del ScrollRect")]
        [SerializeField] private RectTransform contingut;
        [Tooltip("El node de mostra, apagat")]
        [SerializeField] private NodeDeRecercaUI plantilla;
        [SerializeField] private Color colorDeLesArestes = new Color(0.3f, 0.2f, 0.1f, 0.8f);
        [SerializeField] private Color colorDeLesArestesObertes = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private float gruixDeLesArestes = 10f;
        [Tooltip("Espai al voltant de l'arbre, en píxels")]
        [SerializeField] private float marge = 250f;

        [Header("Panell")]
        [SerializeField] private PanellDeNode panellDeNode;

        private readonly List<NodeDeRecercaUI> _nodes = new List<NodeDeRecercaUI>();
        private readonly List<(Image linia, NodeDeRecerca de, NodeDeRecerca a)> _arestes = new List<(Image, NodeDeRecerca, NodeDeRecerca)>();

        private void Awake()
        {
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (plantilla != null) plantilla.gameObject.SetActive(false);
        }

        private void Start()
        {
            Muntar();
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.EnCanvi += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.EnCanvi -= Refrescar;
        }

        private void Muntar()
        {
            ArbreDeRecercaSO arbre = ArbreDeRecercaSO.Actual;
            if (arbre == null || contingut == null || plantilla == null) return;

            // La mida del contingut: on són els nodes, més el marge.
            Vector2 min = Vector2.zero, max = Vector2.zero;
            foreach (NodeDeRecerca n in arbre.nodes)
            {
                if (n == null) continue;
                min = Vector2.Min(min, n.posicio);
                max = Vector2.Max(max, n.posicio);
            }
            Vector2 centre = (min + max) / 2f;
            contingut.anchorMin = contingut.anchorMax = new Vector2(0.5f, 0.5f);
            contingut.pivot = new Vector2(0.5f, 0.5f);
            contingut.sizeDelta = max - min + Vector2.one * marge * 2f;
            contingut.anchoredPosition = Vector2.zero;

            // Les arestes primer: queden per sota dels nodes.
            var arestes = new GameObject("Arestes", typeof(RectTransform)).GetComponent<RectTransform>();
            arestes.SetParent(contingut, false);
            arestes.anchorMin = arestes.anchorMax = new Vector2(0.5f, 0.5f);
            arestes.sizeDelta = Vector2.zero;
            foreach (NodeDeRecerca n in arbre.nodes)
            {
                if (n?.prerequisits == null) continue;
                foreach (string id in n.prerequisits)
                {
                    NodeDeRecerca p = arbre.Node(id);
                    if (p != null) _arestes.Add((Linia(arestes, p.posicio - centre, n.posicio - centre), p, n));
                }
            }

            foreach (NodeDeRecerca n in arbre.nodes)
            {
                if (n == null) continue;
                NodeDeRecercaUI ui = Instantiate(plantilla, contingut);
                ui.gameObject.SetActive(true);
                var r = (RectTransform)ui.transform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = n.posicio - centre;
                ui.Omplir(n);
                if (ui.Boto != null) ui.Boto.onClick.AddListener(() => { if (panellDeNode != null) panellDeNode.Obrir(n); });
                _nodes.Add(ui);
            }
        }

        private Image Linia(RectTransform pare, Vector2 de, Vector2 a)
        {
            var go = new GameObject("Aresta", typeof(RectTransform));
            go.transform.SetParent(pare, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            Vector2 d = a - de;
            r.anchoredPosition = de;
            r.sizeDelta = new Vector2(d.magnitude, gruixDeLesArestes);
            r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = colorDeLesArestes;
            return img;
        }

        private void Refrescar()
        {
            GestorDeRecerca g = GestorDeRecerca.Instancia;
            ArbreDeRecercaSO arbre = ArbreDeRecercaSO.Actual;
            if (g == null) return;

            for (int z = 0; z < textsDePunts.Length; z++)
                if (textsDePunts[z] != null)
                    textsDePunts[z].text = Icones.Punts(g.Punts(z), arbre != null ? arbre.ColorDe(z) : Color.white);

            foreach (NodeDeRecercaUI n in _nodes) n.Refrescar();
            // Una aresta s'encén quan el node d'on surt ja té algun nivell.
            foreach (var (linia, de, _) in _arestes)
                linia.color = g.Nivell(de) > 0 ? colorDeLesArestesObertes : colorDeLesArestes;
        }

        private void Tancar()
        {
            GestorDAudio.Sonar(So.Tancar);
            if (GameManager.Instancia != null) GameManager.Instancia.TornarDeLEscena();
        }
    }
}
