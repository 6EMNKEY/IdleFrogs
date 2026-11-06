using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // L'escena del mapa: els països (comprar-los, viatjar-hi, recollir el que
    // han guanyat) i els llocs especials (la ciutat comercial, les missions,
    // els esdeveniments, el prestigi). Busca sol tots els PaisDelMapa i
    // PuntDelMapa de l'escena.
    public class GestorDelMapa : MonoBehaviour
    {
        public static GestorDelMapa Instancia { get; private set; }

        [Tooltip("Per centrar-la al país on ets en obrir el mapa. Buit: la de Camera.main")]
        [SerializeField] private ControladorDeCameraDelMapa cameraDelMapa;
        [Tooltip("Tornar al país on eres")]
        [SerializeField] private Button botoTornar;

        [Header("Missatge — \"Desbloqueja X abans\", \"Aviat\"...")]
        [SerializeField] private TMP_Text textMissatge;
        [SerializeField] private float segonsDelMissatge = 2.5f;

        private PaisDelMapa[] _paisos = new PaisDelMapa[0];
        private PuntDelMapa[] _punts = new PuntDelMapa[0];
        private Coroutine _amagarMissatge;

        private void Awake()
        {
            Instancia = this;
            if (botoTornar != null) botoTornar.onClick.AddListener(Tornar);
            if (textMissatge != null) textMissatge.gameObject.SetActive(false);
        }

        private void Start()
        {
            _paisos = FindObjectsByType<PaisDelMapa>(FindObjectsSortMode.None);
            _punts = FindObjectsByType<PuntDelMapa>(FindObjectsSortMode.None);
            foreach (PaisDelMapa p in _paisos) p.Inicialitzar(this);
            foreach (PuntDelMapa p in _punts) p.Inicialitzar(this);

            // Els guanys pugen mentre mires: es repinta cada segon.
            if (GameManager.Instancia != null) GameManager.Instancia.EnTickDeSegon += Refrescar;
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi += RefrescarPunts;
            if (GestorDePaisos.Instancia != null) GestorDePaisos.Instancia.EnCanviDePaisos += Refrescar;
            Textos.EnCanviDIdioma += Refrescar;
            Refrescar();
            CentrarAlPaisActual();
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.EnTickDeSegon -= Refrescar;
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi -= RefrescarPunts;
            if (GestorDePaisos.Instancia != null) GestorDePaisos.Instancia.EnCanviDePaisos -= Refrescar;
            Textos.EnCanviDIdioma -= Refrescar;
            if (Instancia == this) Instancia = null;
        }

        private void CentrarAlPaisActual()
        {
            ControladorDeCameraDelMapa c = cameraDelMapa != null ? cameraDelMapa
                : Camera.main != null ? Camera.main.GetComponent<ControladorDeCameraDelMapa>() : null;
            PaisSO actual = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            if (c == null || actual == null) return;
            foreach (PaisDelMapa p in _paisos)
                if (p.Pais == actual) { c.CentrarEn(p.transform.position); return; }
        }

        private void Refrescar()
        {
            foreach (PaisDelMapa p in _paisos) if (p != null) p.Refrescar();
            RefrescarPunts();
        }

        private void RefrescarPunts()
        {
            foreach (PuntDelMapa p in _punts) if (p != null) p.Refrescar();
        }

        private void Tornar()
        {
            GestorDAudio.Sonar(So.Tancar);
            if (GameManager.Instancia != null) GameManager.Instancia.TornarAlPais();
        }

        // ---------------------------------------------------------------
        // Països
        // ---------------------------------------------------------------

        public void EnTocarPais(PaisDelMapa boto)
        {
            GestorDePaisos gestor = GestorDePaisos.Instancia;
            GameManager gm = GameManager.Instancia;
            PaisSO pais = boto != null ? boto.Pais : null;
            if (gestor == null || gm == null || pais == null) return;

            if (pais.esCiutatComercial)
            {
                Obrir(AccioDelMapa.Botiga);
                return;
            }

            if (gestor.EstaDesbloquejat(pais))
            {
                GestorDAudio.Sonar(So.Acceptar);
                if (gestor.PaisActual == pais) gm.TornarAlPais();
                else gestor.Viatjar(pais);
                return;
            }

            if (!gestor.EsElSeguent(pais))
            {
                GestorDAudio.Sonar(So.Error);
                PaisSO seguent = gestor.Seguent;
                Missatge(Textos.F("mapa.abans", seguent != null ? seguent.NomVisible : Textos.T("mapa.anterior")));
                return;
            }

            // El següent: es paga amb la moneda del país on ets. Si no t'hi
            // arriba, la finestra ho diu (al mapa no s'ofereix pagar amb gemes:
            // sense el país carregat no se'n sap el ritme).
            string moneda = Icones.MonedaDe(gestor.PaisActual);
            if (!gestor.PucComprar(pais))
            {
                CompraAmbGemes.Intentar(pais.preuDeDesbloqueig, Textos.T("conf.pais.titol"),
                    preu => Textos.F("conf.pais.text", pais.NomVisible, preu), () => Comprar(pais));
                return;
            }

            FinestraDeConfirmacio.Preguntar(
                Textos.T("conf.pais.titol"),
                Textos.F("conf.pais.text", pais.NomVisible, FormatadorDeNombres.Doblers(pais.preuDeDesbloqueig) + moneda),
                () => Comprar(pais));
        }

        private void Comprar(PaisSO pais)
        {
            GestorDePaisos gestor = GestorDePaisos.Instancia;
            if (gestor == null || pais == null) return;

            if (!gestor.Comprar(pais))
            {
                GestorDAudio.Sonar(So.Error);
                double falta = pais.preuDeDesbloqueig - (CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0);
                Missatge(Textos.F("mapa.falten", FormatadorDeNombres.Doblers(System.Math.Max(0, falta)) + Icones.MonedaDe(gestor.PaisActual)));
                return;
            }
            GestorDAudio.Sonar(So.Desbloquejar);
            Refrescar();
        }

        // Els guanys d'un país, a la seva cartera.
        public void Recollir(PaisDelMapa boto)
        {
            GestorDePaisos gestor = GestorDePaisos.Instancia;
            if (boto == null || gestor == null || GameManager.Instancia == null) return;

            double guanys = GameManager.Instancia.RecollirDeLaZona(gestor.ZonaDe(boto.Pais));
            if (guanys <= 0) return;
            GestorDAudio.Sonar(So.Recompensa);
            Missatge(Textos.F("mapa.recollit", FormatadorDeNombres.Doblers(guanys) + Icones.MonedaDe(boto.Pais)));
            Refrescar();
        }

        // ---------------------------------------------------------------
        // Llocs
        // ---------------------------------------------------------------

        public void Obrir(AccioDelMapa accio)
        {
            if (accio == AccioDelMapa.Botiga && PanellDeBotiga.Instancia != null)
            {
                PanellDeBotiga.Instancia.Obrir();
                return;
            }
            if (GameManager.Instancia != null && (accio == AccioDelMapa.Prestigi || accio == AccioDelMapa.Recerca || accio == AccioDelMapa.Arena))
            {
                GestorDAudio.Sonar(So.Obrir);
                if (accio == AccioDelMapa.Prestigi) GameManager.Instancia.AnarAlPrestigi();
                else if (accio == AccioDelMapa.Recerca) GameManager.Instancia.AnarALaRecerca();
                else GameManager.Instancia.AnarALArena();
                return;
            }
            // Missions i esdeveniments encara no existeixen.
            GestorDAudio.Sonar(So.Toc);
            Missatge(Textos.T("mapa.aviat"));
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
        }
    }
}
