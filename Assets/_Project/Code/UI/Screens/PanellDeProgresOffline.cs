using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // "Offline Progress!!": el que has guanyat mentre no hi eres, idle × temps.
    //
    // Els doblers no es donen en carregar: es queden pendents (i guardats) fins
    // que el jugador accepta. Tant ACCEPTAR com la creu cobren, i les monedes
    // volen del total cap al comptador. El botó de l'anunci cobra el doble.
    public class PanellDeProgresOffline : MonoBehaviour
    {
        [Header("Arrel del panell (el que s'amaga i es mostra)")]
        [SerializeField] private GameObject arrel;

        [Header("Textos")]
        [Tooltip("\"36000s x 18.5B\" i la moneda")]
        [SerializeField] private TMP_Text textCalcul;
        [Tooltip("\"78T!!\" i la moneda")]
        [SerializeField] private TMP_Text textTotal;

        [Header("Esclat de monedes en cobrar")]
        [Tooltip("D'on surten les monedes. Buit: del text del total")]
        [SerializeField] private RectTransform origenDeLEsclat;

        [Header("Botons — tots dos cobren")]
        [SerializeField] private Button botoAcceptar;
        [SerializeField] private Button botoTancar;
        [Tooltip("Opcional: \"x2\" veient un anunci. Amb \"Sense anuncis\" el x2 es dona sense anunci")]
        [SerializeField] private Button botoX2Anunci;
        [Tooltip("Opcional: el text del botó x2. Buit: el primer TMP de dins del botó")]
        [SerializeField] private TMP_Text textX2Anunci;

        private bool _esperantAnunci;

        private void Awake()
        {
            if (botoAcceptar != null) botoAcceptar.onClick.AddListener(Cobrar);
            if (botoTancar != null) botoTancar.onClick.AddListener(Cobrar);
            if (botoX2Anunci != null)
            {
                botoX2Anunci.onClick.AddListener(CobrarDoble);
                if (textX2Anunci == null) textX2Anunci = botoX2Anunci.GetComponentInChildren<TMP_Text>(true);
            }
            if (arrel != null) arrel.SetActive(false);
        }

        // El GameManager pot haver fet el càlcul abans o després d'aquest
        // Start: si ja hi ha una recompensa pendent, s'ensenya ara; si no,
        // arribarà per l'event.
        private void Start()
        {
            if (GameManager.Instancia == null) return;

            GameManager.Instancia.EnProgresOffline += Ensenyar;
            if (GameManager.Instancia.HiHaRecompensaOffline)
                Ensenyar(GameManager.Instancia.RecompensaOfflinePendent);
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnProgresOffline -= Ensenyar;
        }

        private void Ensenyar(RecompensaOffline r)
        {
            if (textCalcul != null)
            {
                string segons = System.Math.Round(r.segonsComptats).ToString("0", CultureInfo.InvariantCulture);
                textCalcul.text = $"{segons}s x {FormatadorDeNombres.Doblers(r.PerSegon)}{Icones.Moneda}";
            }
            if (textTotal != null)
                textTotal.text = $"{FormatadorDeNombres.Doblers(r.doblers)}!!{Icones.Moneda}";

            if (textX2Anunci != null) textX2Anunci.text = Textos.T("anunci.x2");
            if (arrel != null) arrel.SetActive(true);
        }

        public void Cobrar() => Cobrar(1);

        private void CobrarDoble()
        {
            if (_esperantAnunci || GestorDeBotiga.Instancia == null) return;
            _esperantAnunci = true;
            GestorDeBotiga.Instancia.VeureAnunci(vist =>
            {
                _esperantAnunci = false;
                if (vist) Cobrar(2);
                else GestorDAudio.Sonar(So.Error);
            });
        }

        private void Cobrar(double factor)
        {
            if (arrel != null && !arrel.activeSelf) return;
            if (_esperantAnunci) return;

            // On és l'origen, abans d'amagar el panell: un cop apagat ja no és
            // a la pantalla.
            RectTransform origen = origenDeLEsclat != null ? origenDeLEsclat
                                 : textTotal != null ? textTotal.rectTransform : null;
            Vector2 punt = origen != null ? EfecteDeRecompensa.PuntDePantalla(origen) : Vector2.zero;

            if (arrel != null) arrel.SetActive(false);
            if (GameManager.Instancia == null) return;

            double cobrat = GameManager.Instancia.CobrarRecompensaOffline(factor);
            if (cobrat > 0) GestorDAudio.Sonar(So.Recompensa);
            if (cobrat > 0 && origen != null && EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.Llancar(TipusDeMoneda.Doblers, punt, cobrat);
        }
    }
}
