using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    [System.Serializable]
    public class IconaDEstadistica
    {
        public TipusDEstadistica tipus;
        public Sprite icona;
    }

    // Un sol panell per a totes les unitats millorables: pantà, tren i
    // tenda. No sap què està ensenyant — li demana a la unitat el resum,
    // les fites i les estadístiques i les pinta.
    //
    // Els botons dels prefabs hi arriben per la instància: no poden guardar
    // una referència perquè som un objecte d'escena i ells són prefabs.
    public class PanellDeMillora : MonoBehaviour
    {
        public static PanellDeMillora Instancia { get; private set; }

        // La guia inicial ho fa servir per saber que ja has obert el panell.
        public bool EstaObert => arrel != null && arrel.activeSelf;

        [Header("Arrel del panell (el que s'amaga i es mostra)")]
        [SerializeField] private GameObject arrel;

        [Header("Capçalera")]
        [SerializeField] private Button botoTancar;

        [Header("Targeta")]
        [SerializeField] private Image retrat;
        [Tooltip("\"Pantà 1\"")]
        [SerializeField] private TMP_Text nom;
        [Tooltip("\"Level 500/800\"")]
        [SerializeField] private TMP_Text textNivell;
        [Tooltip("\"Boost at lvl 600\"")]
        [SerializeField] private TMP_Text textSeguentBoost;
        [Tooltip("\"Producció total: 100k\"")]
        [SerializeField] private TMP_Text textResum;

        [Header("Barra fins al següent boost")]
        [Tooltip("Una Image amb Image Type = Filled, Fill Method = Horizontal")]
        [SerializeField] private Image barraDeBoost;
        [Tooltip("\"+20\" i la gema")]
        [SerializeField] private TMP_Text textRecompensa;

        [Header("Estadístiques")]
        [Tooltip("El text del separador: el posa cada unitat (Titol De Les Estadistiques)")]
        [SerializeField] private TMP_Text textDelSeparador;
        [SerializeField] private RectTransform contenidorDeFiles;
        [SerializeField] private FilaDEstadistica prefabDeFila;
        [Tooltip("La icona de cada caixa. Les que no hi siguin surten sense icona")]
        [SerializeField] private IconaDEstadistica[] icones;

        [Header("Peu")]
        [Tooltip("Cada botó porta el seu quadrat com a Image pròpia: només es veu la del triat")]
        [SerializeField] private Button botoX1, botoX10, botoX50, botoMax;
        [Tooltip("\"Cash : 65B\" i la moneda")]
        [SerializeField] private TMP_Text textDoblers;
        [SerializeField] private Button botoMillorar;
        [Tooltip("El cost i la moneda. \"UPGRADE\" és un text fix a part")]
        [SerializeField] private TMP_Text textCostDeMillora;

        [Header("Botó d'upgrade sense diners")]
        [Tooltip("Es multiplica pel sprite del botó, com el Disabled Color del Button " +
                 "(que el panell sobreescriu amb aquest). Deixa A a 1: amb menys es torna transparent")]
        [SerializeField] private Color colorSenseDiners = new Color(0.44f, 0.44f, 0.44f, 1f);
        [Tooltip("Només per provar en Play: apaga el botó encara que tinguis diners, " +
                 "per veure el color mentre el canvies")]
        [SerializeField] private bool provarSenseDiners;

        private UnitatMillorable _unitat;
        // Quin dels quatre està triat: 0 = x1, 1 = x10, 2 = x50, 3 = max.
        // Es recorda entre sessions: és una preferència de qui juga, no
        // part de la partida, i per això va a PlayerPrefs i no al guardat.
        private const string ClauDeLaQuantitat = "IdleFrogs.QuantitatDeMillora";
        private static readonly int[] Quantitats = { 1, 10, 50, 1 };
        private const int IndexMaxim = 3;
        private int _triat;

        private readonly List<FilaEstadistica> _dades = new List<FilaEstadistica>();
        private readonly List<FilaDEstadistica> _files = new List<FilaDEstadistica>();

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoMillorar != null) botoMillorar.onClick.AddListener(Millorar);
            AplicarColorSenseDiners();
            if (botoX1 != null) botoX1.onClick.AddListener(() => Triar(0));
            if (botoX10 != null) botoX10.onClick.AddListener(() => Triar(1));
            if (botoX50 != null) botoX50.onClick.AddListener(() => Triar(2));
            if (botoMax != null) botoMax.onClick.AddListener(() => Triar(IndexMaxim));

            // La primera vegada no hi ha res guardat i comença a x1.
            _triat = Mathf.Clamp(PlayerPrefs.GetInt(ClauDeLaQuantitat, 0), 0, IndexMaxim);
            PintarSeleccio();
            if (arrel != null) arrel.SetActive(false);
        }

        public void Obrir(UnitatMillorable unitat)
        {
            if (unitat == null || !unitat.EstaComprat) return;

            // Dos panells oberts alhora es trepitgen; el d'encarregats surt.
            if (PanellDEncarregats.Instancia != null) PanellDEncarregats.Instancia.Tancar();

            Desubscriure();
            _unitat = unitat;
            _unitat.EnCanviDeNivell += Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers += RefrescarPerDoblers;

            if (arrel != null) arrel.SetActive(true);
            Refrescar();
        }

        public void Tancar()
        {
            Desubscriure();
            _unitat = null;
            if (arrel != null) arrel.SetActive(false);
        }

        private void OnEnable() => Textos.EnCanviDIdioma += Refrescar;

        private void OnDisable() => Textos.EnCanviDIdioma -= Refrescar;

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            Desubscriure();
        }

        private void Desubscriure()
        {
            if (_unitat != null) _unitat.EnCanviDeNivell -= Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= RefrescarPerDoblers;
        }

        private void Triar(int index)
        {
            _triat = index;
            PlayerPrefs.SetInt(ClauDeLaQuantitat, index);
            PintarSeleccio();
            Refrescar();
        }

        // Només s'amaga el quadrat (la Image del botó), no el text "x10".
        // Amb alfa 0 i no amb enabled = false: si s'apagués la Image, el
        // botó deixaria de rebre tocs i no el podries tornar a triar.
        private void PintarSeleccio()
        {
            Mostrar(botoX1, _triat == 0);
            Mostrar(botoX10, _triat == 1);
            Mostrar(botoX50, _triat == 2);
            Mostrar(botoMax, _triat == IndexMaxim);
        }

        private static void Mostrar(Button boto, bool visible)
        {
            if (boto == null || boto.image == null) return;
            Color c = boto.image.color;
            c.a = visible ? 1f : 0f;
            boto.image.color = c;
        }

        private int QuantitatActual()
        {
            if (_unitat == null) return 0;
            return _triat == IndexMaxim ? Mathf.Max(1, _unitat.MillorsQuePucPagar()) : Quantitats[_triat];
        }

        private void RefrescarPerDoblers(double _) => Refrescar();

        private void Refrescar()
        {
            if (_unitat == null) return;

            int ara = _unitat.Nivell;
            int k = QuantitatActual();
            int desti = Mathf.Min(ara + k, _unitat.NivellMaxim);

            PintarTargeta(ara, desti);

            if (textDelSeparador != null) textDelSeparador.text = _unitat.TitolDeLesEstadistiques;
            _dades.Clear();
            _unitat.OmplirFiles(_dades, desti);
            PintarFiles();

            double cost = _unitat.CostDeMillora(k);
            double doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;
            bool alMaxim = ara >= _unitat.NivellMaxim;

            if (textDoblers != null)
                textDoblers.text = Textos.F("millora.diners", FormatadorDeNombres.Doblers(doblers) + Icones.Moneda);
            if (textCostDeMillora != null)
                textCostDeMillora.text = alMaxim ? Textos.T("millora.max") : FormatadorDeNombres.Doblers(cost) + Icones.Moneda;
            // No interactuable = el Disabled Color del botó: és el que el fa més fosc.
            if (botoMillorar != null)
                botoMillorar.interactable = !provarSenseDiners && !alMaxim && k > 0 && doblers >= cost;
        }

        private void AplicarColorSenseDiners()
        {
            if (botoMillorar == null) return;
            ColorBlock colors = botoMillorar.colors;
            colors.disabledColor = colorSenseDiners;
            botoMillorar.colors = colors;
        }

        // Canviar el color o la casella a l'inspector es veu a l'acte, també
        // amb el joc en marxa: així es pot triar mirant el botó.
        private void OnValidate()
        {
            AplicarColorSenseDiners();
            if (Application.isPlaying) Refrescar();
        }

        private void PintarTargeta(int ara, int desti)
        {
            if (retrat != null)
            {
                retrat.sprite = _unitat.RetratDelPanell;
                retrat.enabled = retrat.sprite != null;
            }
            if (nom != null) nom.text = _unitat.NomVisible;
            if (textNivell != null) textNivell.text = Textos.F("millora.nivell", ara, _unitat.NivellMaxim);

            FilaEstadistica resum = _unitat.Resum(desti);
            if (textResum != null)
                textResum.text = prefabDeFila != null
                    ? $"{resum.nom}: {prefabDeFila.Valor(resum)}"
                    : $"{resum.nom}: {resum.valor}";

            FitaDeMultiplicador[] fites = _unitat.FitesDeMillora;
            FitaDeMultiplicador seguent = Fites.Seguent(fites, ara);

            if (textSeguentBoost != null)
                textSeguentBoost.text = seguent != null ? Textos.F("millora.boost", seguent.nivell) : Textos.T("millora.tots_boosts");
            if (barraDeBoost != null) barraDeBoost.fillAmount = Fites.Progres(fites, ara);
            if (textRecompensa != null)
                textRecompensa.text = seguent != null && seguent.gemes > 0 ? $"+{seguent.gemes}{Icones.Gema}" : string.Empty;
        }

        // Reaprofitem les caixes que ja hi ha i només en creem quan en falten:
        // el panell s'obre i es refresca constantment.
        private void PintarFiles()
        {
            if (contenidorDeFiles == null || prefabDeFila == null) return;

            while (_files.Count < _dades.Count)
                _files.Add(Instantiate(prefabDeFila, contenidorDeFiles));

            for (int i = 0; i < _files.Count; i++)
            {
                bool cal = i < _dades.Count;
                _files[i].gameObject.SetActive(cal);
                if (cal) _files[i].Omplir(_dades[i], IconaPer(_dades[i].tipus));
            }
        }

        private Sprite IconaPer(TipusDEstadistica tipus)
        {
            if (icones == null) return null;
            foreach (IconaDEstadistica i in icones)
                if (i != null && i.tipus == tipus) return i.icona;
            return null;
        }

        private void Millorar()
        {
            if (_unitat == null) return;

            int gemesAbans = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.gemesActuals : 0;
            if (!_unitat.Millorar(QuantitatActual())) return;

            // Si ha passat una fita, les gemes surten del "+20" de la barra.
            int guanyades = (CurrencyManager.Instancia != null ? CurrencyManager.Instancia.gemesActuals : 0) - gemesAbans;
            if (guanyades > 0 && EfecteDeRecompensa.Instancia != null)
            {
                RectTransform origen = textRecompensa != null ? textRecompensa.rectTransform
                                     : barraDeBoost != null ? barraDeBoost.rectTransform : null;
                EfecteDeRecompensa.Instancia.LlancarDesDe(TipusDeMoneda.Gemes, origen, guanyades);
            }
        }
    }
}
