using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // "Offline Progress!!": el que has guanyat mentre no hi eres, idle × temps.
    //
    // Els doblers no es donen en carregar: es queden pendents (i guardats) fins
    // que el jugador tria. Dos botons cobren el doble, un pagant gemes i
    // l'altre veient un vídeo; la creu cobra el que hi ha, sense doblar. Les
    // monedes volen del total cap al comptador.
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

        [Header("x2 pagant gemes")]
        [FormerlySerializedAs("botoAcceptar")]
        [SerializeField] private Button botoX2Gemes;
        [Tooltip("Opcional: el text del botó (\"x2 200 gemes\"). Buit: el primer TMP de dins del botó")]
        [SerializeField] private TMP_Text textX2Gemes;
        [Min(0)] [SerializeField] private int gemesPerX2 = 200;

        [Header("x2 veient un vídeo (amb \"Sense anuncis\", sense vídeo)")]
        [SerializeField] private Button botoX2Anunci;
        [Tooltip("Opcional: el text del botó x2. Buit: el primer TMP de dins del botó")]
        [SerializeField] private TMP_Text textX2Anunci;

        [Header("La creu: cobra sense doblar")]
        [SerializeField] private Button botoTancar;

        private bool _esperantAnunci;

        private void Awake()
        {
            if (botoTancar != null) botoTancar.onClick.AddListener(Cobrar);
            if (botoX2Gemes != null)
            {
                botoX2Gemes.onClick.AddListener(CobrarDobleAmbGemes);
                if (textX2Gemes == null) textX2Gemes = botoX2Gemes.GetComponentInChildren<TMP_Text>(true);
            }
            if (botoX2Anunci != null)
            {
                botoX2Anunci.onClick.AddListener(CobrarDoble);
                if (textX2Anunci == null) textX2Anunci = botoX2Anunci.GetComponentInChildren<TMP_Text>(true);
            }
            // Aquests textos els posa el panell: un TextLocalitzat ("ACCEPT") els trepitjaria.
            NoLocalitzar(textX2Gemes);
            NoLocalitzar(textX2Anunci);
            if (arrel != null) arrel.SetActive(false);
        }

        // El GameManager pot haver fet el càlcul abans o després d'aquest
        // Start: si ja hi ha una recompensa pendent, s'ensenya ara; si no,
        // arribarà per l'event.
        private void Start()
        {
            if (GameManager.Instancia == null) return;

            GameManager.Instancia.EnProgresOffline += Ensenyar;
            // Si compra gemes a la botiga amb el panell obert, el botó s'encén.
            if (CurrencyManager.Instancia != null) CurrencyManager.Instancia.EnCanviDeGemes += RefrescarPerGemes;
            if (GameManager.Instancia.HiHaRecompensaOffline)
                Ensenyar(GameManager.Instancia.RecompensaOfflinePendent);
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnProgresOffline -= Ensenyar;
            if (CurrencyManager.Instancia != null) CurrencyManager.Instancia.EnCanviDeGemes -= RefrescarPerGemes;
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

            if (arrel != null) arrel.SetActive(true);
            RefrescarBotons();
        }

        private void RefrescarPerGemes(int _)
        {
            if (arrel == null || arrel.activeSelf) RefrescarBotons();
        }

        private static void NoLocalitzar(TMP_Text t)
        {
            TextLocalitzat loc = t != null ? t.GetComponent<TextLocalitzat>() : null;
            if (loc != null) loc.enabled = false;
        }

        private void RefrescarBotons()
        {
            if (textX2Gemes != null) textX2Gemes.text = Textos.F("offline.x2_gemes", FormatadorDeNombres.Doblers(gemesPerX2) + Icones.Gema);
            if (textX2Anunci != null) textX2Anunci.text = Textos.T("anunci.x2");
            CompraAmbGemes.Pintar(botoX2Gemes, CurrencyManager.Instancia != null && CurrencyManager.Instancia.gemesActuals >= gemesPerX2);
        }

        public void Cobrar() => Cobrar(1);

        // Sense prou gemes s'obre la botiga a Gemes i el panell es queda obert.
        private void CobrarDobleAmbGemes()
        {
            if (_esperantAnunci || CurrencyManager.Instancia == null) return;
            if (!CurrencyManager.Instancia.GastarGemes(gemesPerX2))
            {
                GestorDAudio.Sonar(So.Error);
                if (PanellDeBotiga.Instancia != null) PanellDeBotiga.Instancia.ObrirA(SeccioDeBotiga.Gemes);
                return;
            }
            Cobrar(2);
        }

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
