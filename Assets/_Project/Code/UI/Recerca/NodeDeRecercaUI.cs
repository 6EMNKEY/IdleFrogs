using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un node de l'arbre: un quadrat amb la seva imatge i el nivell ("2/5").
    // El marc es pinta segons l'estat: bloquejat, disponible (o que ja el
    // pots pagar) i al màxim.
    public class NodeDeRecercaUI : MonoBehaviour
    {
        [SerializeField] private Button boto;
        [Tooltip("El marc: es pinta segons l'estat")]
        [SerializeField] private Image marc;
        [SerializeField] private Image icona;
        [SerializeField] private TMP_Text textNivell;

        [Header("Colors del marc")]
        [SerializeField] private Color bloquejat = new Color(0.35f, 0.35f, 0.35f);
        [SerializeField] private Color disponible = new Color(0.85f, 0.75f, 0.55f);
        [SerializeField] private Color pucComprar = new Color(0.4f, 0.85f, 0.3f);
        [SerializeField] private Color maxim = new Color(1f, 0.82f, 0.2f);

        public NodeDeRecerca Node { get; private set; }
        public Button Boto => boto;

        public void Omplir(NodeDeRecerca n)
        {
            Node = n;
            if (icona != null)
            {
                icona.sprite = n.icona;
                icona.enabled = n.icona != null;
            }
            Refrescar();
        }

        public void Refrescar()
        {
            GestorDeRecerca g = GestorDeRecerca.Instancia;
            if (Node == null || g == null) return;
            EstatDeNode estat = g.Estat(Node);
            if (marc != null)
                marc.color = estat == EstatDeNode.Maxim ? maxim
                           : estat == EstatDeNode.Bloquejat ? bloquejat
                           : g.LiArriba(Node) ? pucComprar : disponible;
            if (icona != null) icona.color = estat == EstatDeNode.Bloquejat ? new Color(1, 1, 1, 0.4f) : Color.white;
            if (textNivell != null) textNivell.text = $"{g.Nivell(Node)}/{Node.nivellMaxim}";
        }
    }
}
