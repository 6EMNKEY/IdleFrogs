using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El panell del botó (i) d'una targeta: el nom, la icona i tot el que
    // dona el producte amb les seves condicions. Opcionalment, es pot
    // comprar des d'aquí mateix.
    public class PanellDInformacioDeProducte : MonoBehaviour
    {
        public static PanellDInformacioDeProducte Instancia { get; private set; }

        [Tooltip("El que s'encén i s'apaga. Amb un fons a pantalla completa amb Raycast Target")]
        [SerializeField] private GameObject arrel;
        [SerializeField] private Image icona;
        [SerializeField] private TMP_Text titol;
        [Tooltip("El detall, línia a línia")]
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button botoTancar;
        [Tooltip("Opcional: comprar des del panell")]
        [SerializeField] private Button botoComprar;
        [Tooltip("Opcional: el preu dins del botó de comprar")]
        [SerializeField] private TMP_Text textComprar;

        private TargetaDeProducte _targeta;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoComprar != null) botoComprar.onClick.AddListener(Comprar);
            if (arrel != null) arrel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public void Obrir(ProducteDeBotiga p, TargetaDeProducte targeta)
        {
            if (p == null) return;
            _targeta = targeta;
            if (icona != null)
            {
                icona.sprite = p.icona;
                icona.enabled = p.icona != null;
            }
            if (titol != null) titol.text = TextosDeBotiga.Nom(p);
            if (text != null) text.text = TextosDeBotiga.Detall(p);
            if (botoComprar != null)
            {
                botoComprar.gameObject.SetActive(targeta != null);
                botoComprar.interactable = targeta != null && targeta.PotComprar;
            }
            if (textComprar != null) textComprar.text = targeta != null ? targeta.TextDelBoto : string.Empty;
            if (arrel != null)
            {
                arrel.SetActive(true);
                arrel.transform.SetAsLastSibling();
            }
        }

        public void Tancar()
        {
            _targeta = null;
            if (arrel != null) arrel.SetActive(false);
        }

        private void Comprar()
        {
            TargetaDeProducte t = _targeta;
            Tancar();
            if (t != null) t.Comprar();
        }
    }
}
