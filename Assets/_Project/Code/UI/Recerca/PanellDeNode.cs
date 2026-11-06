using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El detall d'un node: què fa, què faria al nivell següent, el preu en
    // punts (dels colors que demana) i el botó de comprar.
    public class PanellDeNode : MonoBehaviour
    {
        [SerializeField] private GameObject arrel;
        [SerializeField] private Image icona;
        [SerializeField] private TMP_Text textNom;
        [SerializeField] private TMP_Text textNivell;
        [SerializeField] private TMP_Text textDescripcio;
        [Tooltip("\"Cal abans: ...\" quan està bloquejat")]
        [SerializeField] private TMP_Text textRequisits;
        [SerializeField] private TMP_Text textCost;
        [SerializeField] private Button botoComprar;
        [SerializeField] private TMP_Text textComprar;
        [SerializeField] private Button botoTancar;

        private NodeDeRecerca _node;

        private void Awake()
        {
            if (textComprar == null && botoComprar != null) textComprar = botoComprar.GetComponentInChildren<TMP_Text>(true);
            if (botoComprar != null) botoComprar.onClick.AddListener(Comprar);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (arrel != null) arrel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.EnCanvi += Refrescar;
        }

        private void OnDisable()
        {
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.EnCanvi -= Refrescar;
        }

        public void Obrir(NodeDeRecerca n)
        {
            _node = n;
            GestorDAudio.Sonar(So.Obrir);
            if (arrel != null)
            {
                arrel.SetActive(true);
                arrel.transform.SetAsLastSibling();
            }
            Refrescar();
        }

        public void Tancar()
        {
            if (arrel != null && arrel.activeSelf) GestorDAudio.Sonar(So.Tancar);
            if (arrel != null) arrel.SetActive(false);
        }

        private void Refrescar()
        {
            GestorDeRecerca g = GestorDeRecerca.Instancia;
            if (_node == null || g == null || arrel == null || !arrel.activeSelf) return;
            int nivell = g.Nivell(_node);
            EstatDeNode estat = g.Estat(_node);

            if (icona != null)
            {
                icona.sprite = _node.icona;
                icona.enabled = _node.icona != null;
            }
            Posar(textNom, TextosDeRecerca.Nom(_node));
            Posar(textNivell, $"{nivell}/{_node.nivellMaxim}");
            Posar(textDescripcio, TextosDeRecerca.Descripcio(_node, nivell));
            Posar(textRequisits, estat == EstatDeNode.Bloquejat ? Textos.F("recerca.cal", Requisits()) : string.Empty);
            Posar(textCost, estat == EstatDeNode.Maxim ? string.Empty : TextosDeRecerca.Cost(_node, nivell, g.Arbre));

            if (botoComprar != null)
            {
                botoComprar.gameObject.SetActive(estat != EstatDeNode.Maxim);
                botoComprar.interactable = estat == EstatDeNode.Disponible;
                CompraAmbGemes.Pintar(botoComprar, estat == EstatDeNode.Disponible && g.LiArriba(_node));
            }
            Posar(textComprar, Textos.T(estat == EstatDeNode.Bloquejat ? "recerca.bloquejat" : "recerca.investigar"));
            if (estat == EstatDeNode.Maxim) Posar(textNivell, Textos.T("recerca.maxim"));
        }

        private string Requisits()
        {
            GestorDeRecerca g = GestorDeRecerca.Instancia;
            var noms = new System.Collections.Generic.List<string>();
            foreach (string id in _node.prerequisits)
            {
                NodeDeRecerca p = g != null && g.Arbre != null ? g.Arbre.Node(id) : null;
                if (p != null && g.Nivell(p) <= 0) noms.Add(TextosDeRecerca.Nom(p));
            }
            return string.Join(", ", noms);
        }

        private void Comprar()
        {
            GestorDeRecerca g = GestorDeRecerca.Instancia;
            if (_node == null || g == null) return;
            bool fet = g.Comprar(_node);
            GestorDAudio.Sonar(fet ? So.Millorar : So.Error);
        }

        private static void Posar(TMP_Text t, string text)
        {
            if (t != null) t.text = text ?? string.Empty;
        }
    }
}
