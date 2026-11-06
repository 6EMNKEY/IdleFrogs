using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // L'escena del prestigi: el d'un país (s'obre al país on ets) i les
    // fletxes < > per passar als altres països oberts. A l'esquerra el pantà
    // i la granota d'aquell país, amb les estrelles (grises fins que es
    // compren); a la dreta què guanyes, la dificultat, què conserves i el
    // botó amb el preu.
    //
    // Totes les peces són opcionals. Les imatges sense sprite es queden amb
    // el seu color (els quadrats de mostra) fins que hi posis el dibuix.
    public class GestorDelPrestigi : MonoBehaviour
    {
        [Header("Fons")]
        [Tooltip("Es pinta del Color De Fons del país que mires (com la càmera). Buit: només la càmera")]
        [SerializeField] private Image fons;
        [Tooltip("Opcional: la gespa del país escampada pel fons (menys pel pantà)")]
        [SerializeField] private GespaDelPrestigi gespa;

        [Header("Esquerra")]
        [SerializeField] private Image dibuixDelPanta;
        [SerializeField] private Image dibuixDeLaGranota;
        [Tooltip("Damunt de la granota: el dibuix de les estrelles que té. S'amaga sense estrelles")]
        [SerializeField] private Image estrellesDeLaGranota;
        [Tooltip("D'esquerra a dreta, una per estrella")]
        [SerializeField] private Image[] estrelles = new Image[0];
        [Tooltip("L'estrella que encara no tens (fosca)")]
        [SerializeField] private Sprite estrellaApagada;
        [Tooltip("L'estrella comprada (groga)")]
        [SerializeField] private Sprite estrellaEncesa;
        [Tooltip("Només sense el sprite: el color del quadrat de mostra")]
        [SerializeField] private Color colorApagada = new Color(0.55f, 0.55f, 0.55f, 1f);
        [Tooltip("Només sense el sprite: el color del quadrat de mostra")]
        [SerializeField] private Color colorEncesa = Color.white;

        [Header("Dreta")]
        [SerializeField] private TMP_Text textNomDelPais;
        [Tooltip("\"El país es reiniciarà per...\"")]
        [SerializeField] private TMP_Text textExplicacio;
        [Tooltip("\"x3 → x8\"")]
        [SerializeField] private TMP_Text textIngressos;
        [Tooltip("\"+50 💎\"")]
        [SerializeField] private TMP_Text textGemes;
        [Tooltip("\"x2.5 → x6\"")]
        [SerializeField] private TMP_Text textDificultat;
        [Tooltip("El que tens per pagar-lo")]
        [SerializeField] private TMP_Text textTens;

        [Header("Botons")]
        [SerializeField] private Button botoPrestigi;
        [Tooltip("El preu, \"Prestigi màxim\" o \"Juga primer aquí\". Buit: el primer text del botó")]
        [SerializeField] private TMP_Text textDelBoto;
        [SerializeField] private Button botoTancar;
        [SerializeField] private Button botoAnterior;
        [SerializeField] private Button botoSeguent;

        private readonly List<PaisSO> _paisos = new List<PaisSO>();
        private int _index;
        private readonly Dictionary<Image, Color> _colorsDeMostra = new Dictionary<Image, Color>();

        private void Awake()
        {
            if (textDelBoto == null && botoPrestigi != null) textDelBoto = botoPrestigi.GetComponentInChildren<TMP_Text>(true);
            if (botoPrestigi != null) botoPrestigi.onClick.AddListener(Preguntar);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoAnterior != null) botoAnterior.onClick.AddListener(() => Moure(-1));
            if (botoSeguent != null) botoSeguent.onClick.AddListener(() => Moure(1));
            foreach (Image i in new[] { dibuixDelPanta, dibuixDeLaGranota })
                if (i != null) _colorsDeMostra[i] = i.color;
        }

        private void Start()
        {
            // Els països de pantans oberts, en ordre; comença pel que ets.
            GestorDePaisos g = GestorDePaisos.Instancia;
            GameManager gm = GameManager.Instancia;
            if (g != null && gm != null)
            {
                foreach (PaisSO p in g.Paisos)
                    if (p != null && !p.esCiutatComercial && g.ZonaDe(p) < gm.PaisosDesbloquejats) _paisos.Add(p);
                _index = Mathf.Max(0, _paisos.IndexOf(g.PaisActual));
            }

            if (gm != null) gm.EnTickDeSegon += Refrescar;
            if (CurrencyManager.Instancia != null) CurrencyManager.Instancia.EnCanviDeGemes += RefrescarPerGemes;
            Textos.EnCanviDIdioma += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.EnTickDeSegon -= Refrescar;
            if (CurrencyManager.Instancia != null) CurrencyManager.Instancia.EnCanviDeGemes -= RefrescarPerGemes;
            Textos.EnCanviDIdioma -= Refrescar;
        }

        private PaisSO Pais => _index >= 0 && _index < _paisos.Count ? _paisos[_index] : null;
        private int Zona => Pais != null && GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.ZonaDe(Pais) : -1;

        private void Moure(int d)
        {
            int nou = Mathf.Clamp(_index + d, 0, _paisos.Count - 1);
            if (nou == _index) return;
            _index = nou;
            GestorDAudio.Sonar(So.Toc);
            Refrescar();
        }

        private void RefrescarPerGemes(int _) => Refrescar();

        private void Refrescar()
        {
            GameManager gm = GameManager.Instancia;
            PrestigiConfigSO config = PrestigiConfigSO.Actual;
            PaisSO pais = Pais;
            if (gm == null || pais == null) return;
            int z = Zona;
            int te = gm.Estrelles(z);
            int maxim = gm.MaximDEstrelles;
            bool alMaxim = te >= maxim;

            // Les fletxes només si hi ha on anar.
            Mostrar(botoAnterior, _index > 0);
            Mostrar(botoSeguent, _index < _paisos.Count - 1);

            // El fons, del color del país (el de la seva càmera al joc).
            if (fons != null) fons.color = pais.colorDeFons;
            if (Camera.main != null) Camera.main.backgroundColor = pais.colorDeFons;
            if (gespa != null) gespa.Mostrar(pais);

            // Esquerra: el pantà i la granota d'aquest país, els mateixos que al joc.
            AspectePantaSO aspecte = config != null ? config.AspecteDe(pais) : null;
            Dibuix(dibuixDelPanta, aspecte != null ? SpritesDelPais.DelPais(aspecte.fons, pais) : null,
                   aspecte != null ? aspecte.tintDelFons : Color.white);
            Dibuix(dibuixDeLaGranota, aspecte != null ? SpritesDelPais.DelPais(aspecte.granota, pais) : null, Color.white);
            if (estrellesDeLaGranota != null)
            {
                EstrellaDePrestigi e = config != null ? config.Estrella(te) : null;
                Sprite s = e != null ? SpritesDelPais.DelPais(e.dibuixDeLaGranota, pais) : null;
                estrellesDeLaGranota.sprite = s;
                estrellesDeLaGranota.enabled = s != null;
            }
            for (int i = 0; i < estrelles.Length; i++)
            {
                if (estrelles[i] == null) continue;
                estrelles[i].gameObject.SetActive(i < maxim);
                bool encesa = i < te;
                Sprite s = encesa ? estrellaEncesa : estrellaApagada;
                // Amb el dibuix, tal com és; sense, el quadrat del color.
                if (s != null) estrelles[i].sprite = s;
                estrelles[i].color = s != null ? Color.white : encesa ? colorEncesa : colorApagada;
            }

            // Dreta.
            string moneda = Icones.MonedaDe(pais);
            if (textNomDelPais != null) textNomDelPais.text = pais.NomVisible;
            if (textExplicacio != null) textExplicacio.text = Textos.F("prestigi.explicacio", pais.NomVisible);

            EstrellaDePrestigi seguent = config != null ? config.Estrella(te + 1) : null;
            double ingressosAra = config != null ? config.FactorDIngressos(te) : 1;
            double costAra = config != null ? config.FactorDeCost(te) : 1;
            if (textIngressos != null)
                textIngressos.text = seguent != null ? $"x{FormatadorDeNombres.Factor(ingressosAra)} → x{FormatadorDeNombres.Factor(seguent.multiplicadorDIngressos)}"
                                                     : $"x{FormatadorDeNombres.Factor(ingressosAra)}";
            if (textDificultat != null)
                textDificultat.text = seguent != null ? $"x{FormatadorDeNombres.Factor(costAra)} → x{FormatadorDeNombres.Factor(seguent.multiplicadorDeCost)}"
                                                      : $"x{FormatadorDeNombres.Factor(costAra)}";
            if (textGemes != null)
                textGemes.text = seguent != null ? $"+{FormatadorDeNombres.Doblers(seguent.gemes)}{Icones.Gema}" : "-";

            double preu = gm.PreuDelPrestigi(z);
            double tens = gm.DoblersPerAlPrestigi(z);
            if (textTens != null) textTens.text = Textos.F("prestigi.tens", FormatadorDeNombres.Doblers(tens) + moneda);

            if (textDelBoto != null)
                textDelBoto.text = alMaxim ? Textos.T("prestigi.maxim")
                                 : preu <= 0 ? Textos.T("prestigi.sense_preu")
                                 : $"{Textos.T("prestigi.boto")} {FormatadorDeNombres.Doblers(preu)}{moneda}";
            if (botoPrestigi != null)
            {
                botoPrestigi.interactable = !alMaxim && preu > 0;
                CompraAmbGemes.Pintar(botoPrestigi, alMaxim || tens >= preu);
            }
        }

        private void Dibuix(Image imatge, Sprite sprite, Color tint)
        {
            if (imatge == null) return;
            imatge.sprite = sprite;
            imatge.color = sprite != null ? tint
                         : _colorsDeMostra.TryGetValue(imatge, out Color c) ? c : Color.white;
        }

        private static void Mostrar(Button b, bool visible)
        {
            if (b != null && b.gameObject.activeSelf != visible) b.gameObject.SetActive(visible);
        }

        private void Preguntar()
        {
            GameManager gm = GameManager.Instancia;
            PrestigiConfigSO config = PrestigiConfigSO.Actual;
            PaisSO pais = Pais;
            if (gm == null || pais == null || config == null) return;
            int z = Zona;
            double preu = gm.PreuDelPrestigi(z);
            if (preu <= 0) return;
            string moneda = Icones.MonedaDe(pais);

            if (!gm.PucFerPrestigi(z))
            {
                GestorDAudio.Sonar(So.Error);
                FinestraDeConfirmacio.Preguntar(Textos.T("conf.prestigi.titol"),
                    Textos.F("conf.falten", FormatadorDeNombres.Doblers(preu - gm.DoblersPerAlPrestigi(z)) + moneda), null);
                return;
            }

            EstrellaDePrestigi seguent = config.Estrella(gm.Estrelles(z) + 1);
            FinestraDeConfirmacio.Preguntar(
                Textos.T("conf.prestigi.titol"),
                Textos.F("conf.prestigi.text", pais.NomVisible, FormatadorDeNombres.Doblers(preu) + moneda,
                         "x" + FormatadorDeNombres.Factor(seguent != null ? seguent.multiplicadorDIngressos : 1)),
                () => Fer(z),
                textAcceptar: Textos.T("prestigi.boto"),
                perillosa: true);
        }

        private void Fer(int z)
        {
            if (GameManager.Instancia == null) return;
            bool fet = GameManager.Instancia.FerPrestigi(z);
            GestorDAudio.Sonar(fet ? So.Desbloquejar : So.Error);
            Refrescar();
        }

        private void Tancar()
        {
            GestorDAudio.Sonar(So.Tancar);
            if (GameManager.Instancia != null) GameManager.Instancia.SortirDelPrestigi();
        }
    }
}
