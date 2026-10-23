using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    [System.Serializable]
    public class IconaDHabilitat
    {
        public TipusDeLloc lloc;
        public TipusDEfecte efecte;
        public Sprite icona;
    }

    // El panell d'encarregats d'una ranura. A dalt, la carta de l'encarregat
    // que hi treballa (o buida); a sota, els que tens d'aquest tipus sense
    // feina, amb la compra a l'última casella; al peu, assignar el triat.
    //
    // Les siluetes dels prefabs hi arriben per la instància, perquè un prefab
    // no pot referenciar un objecte d'escena.
    public class PanellDEncarregats : MonoBehaviour
    {
        public static PanellDEncarregats Instancia { get; private set; }

        // La guia inicial ho fa servir per saber que ja has obert el panell.
        public bool EstaObert => arrel != null && arrel.activeSelf;

        [Header("Arrel del panell (el que s'amaga i es mostra)")]
        [SerializeField] private GameObject arrel;

        [Header("Capçalera")]
        [SerializeField] private Button botoTancar;

        [Header("Encarregat actual")]
        [Tooltip("El quadrat de darrere del retrat: es pinta del color de la raresa")]
        [SerializeField] private Image fonsDelRetratActual;
        [SerializeField] private Image retratActual;
        [SerializeField] private TMP_Text nomActual;
        [Tooltip("\"Epic\", del color de la raresa")]
        [SerializeField] private TMP_Text raresaActual;
        [Tooltip("\"Efecte : 3m\"")]
        [SerializeField] private TMP_Text duracioActual;
        [Tooltip("L'habilitat activa: \"5x Velocitat de recol·lecció\"")]
        [SerializeField] private TMP_Text habilitatActual;
        [Tooltip("Opcional: l'efecte passiu, el que fa sempre mentre treballa")]
        [SerializeField] private TMP_Text passiuActual;
        [Tooltip("El botó taronja: engega l'habilitat")]
        [SerializeField] private Button botoHabilitat;
        [Tooltip("Opcional: el temps que queda mentre està encesa o recarregant")]
        [SerializeField] private TMP_Text textBotoHabilitat;
        [Tooltip("La icona de sobre el botó taronja; canvia segons l'habilitat")]
        [SerializeField] private Image iconaDHabilitat;
        [SerializeField] private IconaDHabilitat[] iconesDHabilitat;
        [Tooltip("El botó vermell UNASSIGN")]
        [SerializeField] private Button botoTreure;

        [Header("Carta buida")]
        [SerializeField] private Color colorDelRetratBuit = new Color(0.35f, 0.25f, 0.18f);

        [Header("Llista")]
        [Tooltip("El Content del Scroll View")]
        [SerializeField] private RectTransform contenidorDeFitxes;
        [SerializeField] private FitxaDEncarregat prefabDeFitxa;
        [Tooltip("La casella de comprar. Ha de ser filla del contenidor: es manté sempre l'última")]
        [SerializeField] private RectTransform casellaDeContractar;
        [Tooltip("\"Cash : 65B\" i la moneda")]
        [SerializeField] private TMP_Text textDoblers;
        [SerializeField] private Button botoContractar;
        [Tooltip("El preu i la moneda. \"Hire Boss\" és un text fix a part")]
        [SerializeField] private TMP_Text textPreuDeContractar;
        [Tooltip("Opcional: hi surt què t'ha tocat en comprar")]
        [SerializeField] private TMP_Text textDeResultat;

        [Header("Peu")]
        [Tooltip("ASSIGN SELECTED BOSS. Només es pot tocar amb una carta seleccionada")]
        [SerializeField] private Button botoAssignar;

        private UnitatMillorable _unitat;
        private EncarregatPosseit _seleccionat;
        private readonly List<EncarregatPosseit> _dades = new List<EncarregatPosseit>();
        private readonly List<FitxaDEncarregat> _fitxes = new List<FitxaDEncarregat>();

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoContractar != null) botoContractar.onClick.AddListener(Contractar);
            if (botoAssignar != null) botoAssignar.onClick.AddListener(AssignarElSeleccionat);
            if (botoTreure != null) botoTreure.onClick.AddListener(TreureLActual);
            if (botoHabilitat != null) botoHabilitat.onClick.AddListener(ActivarHabilitat);

            if (arrel != null) arrel.SetActive(false);
        }

        private void OnEnable() => Textos.EnCanviDIdioma += Refrescar;

        private void OnDisable() => Textos.EnCanviDIdioma -= Refrescar;

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            Desubscriure();
        }

        public void Obrir(UnitatMillorable unitat)
        {
            if (unitat == null) return;
            if (GestorDEncarregats.Instancia == null || !GestorDEncarregats.Instancia.EstanDesbloquejats) return;

            // Dos panells oberts alhora es trepitgen; el de millores surt.
            if (PanellDeMillora.Instancia != null) PanellDeMillora.Instancia.Tancar();

            Desubscriure();
            _unitat = unitat;
            _seleccionat = null;

            GestorDEncarregats.Instancia.EnCanviDEncarregats += Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers += RefrescarPerDoblers;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += Refrescar;   // els cooldowns

            if (textDeResultat != null) textDeResultat.text = string.Empty;
            if (arrel != null) arrel.SetActive(true);
            Refrescar();
        }

        public void Tancar()
        {
            Desubscriure();
            _unitat = null;
            _seleccionat = null;
            if (arrel != null) arrel.SetActive(false);
        }

        private void Desubscriure()
        {
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.EnCanviDEncarregats -= Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= RefrescarPerDoblers;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Refrescar;
        }

        private void RefrescarPerDoblers(double _) => Refrescar();

        private void Refrescar()
        {
            if (_unitat == null || GestorDEncarregats.Instancia == null) return;

            Ranura ranura = _unitat.Ranura;
            PintarActual(GestorDEncarregats.Instancia.ALaRanura(ranura));

            GestorDEncarregats.Instancia.NoAssignats(ranura.tipus, _dades);
            // El seleccionat pot haver desaparegut (venut, o assignat des d'un altre lloc).
            if (_seleccionat != null && !_dades.Contains(_seleccionat)) _seleccionat = null;
            PintarFitxes();

            double preu = GestorDEncarregats.Instancia.PreuDeLaSeguentCompra(ranura.tipus);
            double doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;

            if (textDoblers != null)
                textDoblers.text = Textos.F("millora.diners", FormatadorDeNombres.Doblers(doblers) + Icones.Moneda);
            if (textPreuDeContractar != null)
                textPreuDeContractar.text = FormatadorDeNombres.Doblers(preu) + Icones.Moneda;

            // No interactuable = el Disabled Color de cada botó.
            if (botoContractar != null) botoContractar.interactable = doblers >= preu;
            if (botoAssignar != null) botoAssignar.interactable = _seleccionat != null;
        }

        // La carta no desapareix mai: sense encarregat es queda buida.
        private void PintarActual(EncarregatPosseit actual)
        {
            bool hiEs = actual != null && actual.definicio != null;
            EncarregatSO d = hiEs ? actual.definicio : null;

            if (fonsDelRetratActual != null)
                fonsDelRetratActual.color = hiEs ? ColorsDeRaresa.Per(d.raresa) : colorDelRetratBuit;
            if (retratActual != null)
            {
                Sprite s = !hiEs ? null
                         : GestorDEncarregats.Instancia != null ? GestorDEncarregats.Instancia.RetratDe(d) : d.retrat;
                retratActual.sprite = s;
                retratActual.enabled = s != null;
            }
            if (nomActual != null) nomActual.text = hiEs ? d.nomVisible : Textos.T("enc.cap");
            if (raresaActual != null)
            {
                raresaActual.text = hiEs ? ColorsDeRaresa.Nom(d.raresa) : string.Empty;
                if (hiEs) raresaActual.color = ColorsDeRaresa.Per(d.raresa);
            }
            if (duracioActual != null) duracioActual.text = hiEs ? Textos.F("enc.durada", d.DuracioCurta) : string.Empty;
            if (habilitatActual != null) habilitatActual.text = hiEs ? Textos.F("enc.actiu", d.DescripcioDeLHabilitat) : string.Empty;
            if (passiuActual != null) passiuActual.text = hiEs ? Textos.F("enc.passiu", d.DescripcioDeLEfecte) : string.Empty;

            if (botoTreure != null) botoTreure.gameObject.SetActive(hiEs);

            if (iconaDHabilitat != null)
            {
                Sprite icona = hiEs ? IconaPer(d.lloc, d.efecte) : null;
                iconaDHabilitat.sprite = icona;
                iconaDHabilitat.enabled = icona != null;
            }

            if (botoHabilitat == null) return;
            botoHabilitat.gameObject.SetActive(hiEs);
            if (!hiEs) return;

            // Tres estats: llesta (es pot tocar), encesa i recarregant.
            bool activa = actual.HabilitatActiva;
            bool llesta = actual.HabilitatLlesta;
            botoHabilitat.interactable = llesta && !activa;

            if (textBotoHabilitat != null)
                textBotoHabilitat.text = activa ? FormatadorDeNombres.Temps(actual.SegonsDHabilitatRestants)
                                       : llesta ? string.Empty
                                       : FormatadorDeNombres.Temps(actual.SegonsFinsLlesta);
        }

        private Sprite IconaPer(TipusDeLloc lloc, TipusDEfecte efecte)
        {
            if (iconesDHabilitat == null) return null;
            foreach (IconaDHabilitat i in iconesDHabilitat)
                if (i != null && i.lloc == lloc && i.efecte == efecte) return i.icona;
            return null;
        }

        // Es reaprofiten les fitxes i només se'n creen quan en falten, com fa
        // PanellDeMillora amb les caixes.
        private void PintarFitxes()
        {
            if (contenidorDeFitxes == null || prefabDeFitxa == null) return;

            while (_fitxes.Count < _dades.Count)
            {
                FitxaDEncarregat fitxa = Instantiate(prefabDeFitxa, contenidorDeFitxes, false);
                AspecteDelPais.Aplicar(fitxa.gameObject);
                _fitxes.Add(fitxa);
            }

            for (int i = 0; i < _fitxes.Count; i++)
            {
                bool cal = i < _dades.Count;
                _fitxes[i].gameObject.SetActive(cal);
                if (cal) _fitxes[i].Omplir(_dades[i], _dades[i] == _seleccionat, Seleccionar, Vendre);
            }

            // Les fitxes noves es creen al final: la casella de comprar hi torna.
            if (casellaDeContractar != null) casellaDeContractar.SetAsLastSibling();
        }

        // Tocar la que ja està triada la desmarca.
        private void Seleccionar(EncarregatPosseit e)
        {
            _seleccionat = _seleccionat == e ? null : e;
            Refrescar();
        }

        private void Vendre(EncarregatPosseit e)
        {
            if (GestorDEncarregats.Instancia == null) return;
            if (_seleccionat == e) _seleccionat = null;

            // La carta desapareix en vendre: on era, abans que es refresqui.
            RectTransform carta = null;
            foreach (FitxaDEncarregat f in _fitxes)
                if (f != null && f.gameObject.activeSelf && f.Encarregat == e) { carta = (RectTransform)f.transform; break; }
            Vector2 origen = carta != null ? EfecteDeRecompensa.PuntDePantalla(carta) : Vector2.zero;
            double preu = GestorDEncarregats.PreuDeVenda(e);

            if (GestorDEncarregats.Instancia.Vendre(e) && carta != null && EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.Llancar(TipusDeMoneda.Doblers, origen, preu);
        }

        private void AssignarElSeleccionat()
        {
            if (_unitat == null || _seleccionat == null || GestorDEncarregats.Instancia == null) return;
            EncarregatPosseit e = _seleccionat;
            _seleccionat = null;
            // El que hi hagués torna sol a la llista de no assignats.
            GestorDEncarregats.Instancia.Assignar(e, _unitat.Ranura);
        }

        private void TreureLActual()
        {
            if (_unitat == null || GestorDEncarregats.Instancia == null) return;
            GestorDEncarregats.Instancia.Desassignar(GestorDEncarregats.Instancia.ALaRanura(_unitat.Ranura));
        }

        private void ActivarHabilitat()
        {
            if (_unitat == null || GestorDEncarregats.Instancia == null) return;
            GestorDEncarregats.Instancia.ActivarHabilitat(GestorDEncarregats.Instancia.ALaRanura(_unitat.Ranura));
        }

        private void Contractar()
        {
            if (_unitat == null || GestorDEncarregats.Instancia == null) return;

            EncarregatSO rebut = GestorDEncarregats.Instancia.Comprar(_unitat.Ranura.tipus);
            if (rebut == null) return;

            if (textDeResultat != null)
                textDeResultat.text = Textos.F("enc.contractat", rebut.nomVisible, rebut.DescripcioDeLHabilitat);
        }
    }
}
