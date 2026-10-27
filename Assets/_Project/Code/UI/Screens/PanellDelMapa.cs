using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El mapa: s'obre amb el botó del HUD. Tocar un país obert et hi porta;
    // la ciutat comercial obre la botiga; un de bloquejat que no és el següent
    // diu quin cal obrir abans; el següent pregunta si el vols comprar.
    public class PanellDelMapa : MonoBehaviour
    {
        public static PanellDelMapa Instancia { get; private set; }

        [Header("Obrir i tancar")]
        [SerializeField] private GameObject arrel;
        [Tooltip("El botó del mapa del HUD")]
        [SerializeField] private Button botoObrir;
        [SerializeField] private Button botoTancar;

        [Header("Països (els botons de damunt de la imatge)")]
        [SerializeField] private BotoDePais[] paisos;

        [Header("Missatge — \"Desbloqueja X abans\"")]
        [SerializeField] private TMP_Text textMissatge;
        [SerializeField] private float segonsDelMissatge = 2f;

        [Header("Ciutat comercial")]
        [Tooltip("El panell de la botiga. Buit de moment: surt un avís a la consola")]
        [SerializeField] private GameObject panellDeLaBotiga;

        private Coroutine _amagarMissatge;

        public bool EstaObert => arrel != null && arrel.activeSelf;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoObrir != null) botoObrir.onClick.AddListener(Obrir);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);

            if (paisos != null)
                foreach (BotoDePais b in paisos)
                    if (b != null) b.Inicialitzar(this);

            if (arrel != null) arrel.SetActive(false);
            if (textMissatge != null) textMissatge.gameObject.SetActive(false);
        }

        private void OnEnable() => Textos.EnCanviDIdioma += Refrescar;

        private void OnDisable() => Textos.EnCanviDIdioma -= Refrescar;

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public void Obrir()
        {
            // Un panell alhora.
            if (PanellDeMillora.Instancia != null) PanellDeMillora.Instancia.Tancar();
            if (PanellDEncarregats.Instancia != null) PanellDEncarregats.Instancia.Tancar();
            if (PanellDeBotiga.Instancia != null) PanellDeBotiga.Instancia.Tancar();

            if (arrel != null) arrel.SetActive(true);
            Refrescar();
        }

        public void Tancar()
        {
            if (arrel != null) arrel.SetActive(false);
        }

        private void Refrescar()
        {
            GestorDePaisos gestor = GestorDePaisos.Instancia;
            if (paisos == null || gestor == null) return;
            foreach (BotoDePais b in paisos)
                if (b != null) b.Refrescar(gestor);
        }

        public void EnTocarPais(BotoDePais boto)
        {
            GestorDePaisos gestor = GestorDePaisos.Instancia;
            PaisSO pais = boto != null ? boto.Pais : null;
            if (gestor == null || pais == null) return;

            if (pais.esCiutatComercial)
            {
                if (panellDeLaBotiga != null)
                {
                    Tancar();
                    panellDeLaBotiga.SetActive(true);
                }
                else Debug.Log("PanellDelMapa: la botiga de la ciutat comercial encara no existeix");
                return;
            }

            if (gestor.EstaDesbloquejat(pais))
            {
                if (gestor.PaisActual == pais) Tancar();   // ja hi ets
                else gestor.Viatjar(pais);
                return;
            }

            if (!gestor.EsElSeguent(pais))
            {
                PaisSO seguent = gestor.Seguent;
                Missatge(Textos.F("mapa.abans", seguent != null ? seguent.NomVisible : Textos.T("mapa.anterior")));
                return;
            }

            // El següent. Si no t'arriba, la mateixa finestra ofereix pagar el
            // que falta amb gemes.
            if (!gestor.PucComprar(pais))
            {
                CompraAmbGemes.Intentar(pais.preuDeDesbloqueig, Textos.T("conf.pais.titol"),
                    preu => Textos.F("conf.pais.text", pais.NomVisible, preu), () => Comprar(pais));
                return;
            }

            FinestraDeConfirmacio.Preguntar(
                Textos.T("conf.pais.titol"),
                Textos.F("conf.pais.text", pais.NomVisible, FormatadorDeNombres.Doblers(pais.preuDeDesbloqueig) + Icones.Moneda),
                () => Comprar(pais));
        }

        private static string Falten(PaisSO pais)
        {
            double falten = pais.preuDeDesbloqueig - (CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0);
            return Textos.F("mapa.falten", FormatadorDeNombres.Doblers(System.Math.Max(0, falten)) + Icones.Moneda);
        }

        // Entre preguntar i acceptar poden haver canviat els doblers.
        private void Comprar(PaisSO pais)
        {
            GestorDePaisos gestor = GestorDePaisos.Instancia;
            if (gestor == null || pais == null) return;

            if (!gestor.Comprar(pais))
            {
                Missatge(Falten(pais));
                return;
            }
            Refrescar();
        }

        private void Missatge(string text)
        {
            if (textMissatge == null) { Debug.Log(text); return; }
            textMissatge.text = text;
            textMissatge.gameObject.SetActive(true);
            if (_amagarMissatge != null) StopCoroutine(_amagarMissatge);
            _amagarMissatge = StartCoroutine(AmagarMissatge());
        }

        private IEnumerator AmagarMissatge()
        {
            yield return new WaitForSecondsRealtime(segonsDelMissatge);
            if (textMissatge != null) textMissatge.gameObject.SetActive(false);
            _amagarMissatge = null;
        }
    }
}
