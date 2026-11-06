using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // L'escena de l'arena: la del país on ets (o la primera oberta) i les
    // fletxes < > per passar a les altres arenes obertes. Dalt, els punts de
    // recerca del país, els tickets, el temps que queda del cicle i el
    // refresc; al mig els tres pantans; a baix, els lluitadors i el gacha.
    public class PantallaDeLArena : MonoBehaviour
    {
        [Header("Fons")]
        [SerializeField] private Image fons;
        [SerializeField] private GespaDelPrestigi gespa;

        [Header("Dalt")]
        [SerializeField] private TMP_Text textNomDelPais;
        [Tooltip("Els punts de recerca del país, del seu color")]
        [SerializeField] private TMP_Text textPunts;
        [SerializeField] private TMP_Text textTickets;
        [Tooltip("\"Bosses nous en 5h 20m\"")]
        [SerializeField] private TMP_Text textCicle;
        [SerializeField] private Button botoRefrescar;
        [SerializeField] private TMP_Text textRefrescar;

        [Header("Pantans")]
        [SerializeField] private PantaDeLArena[] pantans = new PantaDeLArena[0];
        [Tooltip("Es mostra quan no hi ha cap arena oberta (i s'amaguen els pantans)")]
        [SerializeField] private TMP_Text textBloquejada;
        [Tooltip("El que conté els pantans i els botons de baix")]
        [SerializeField] private GameObject contingut;

        [Header("A baix")]
        [SerializeField] private Button botoLluitadors;
        [SerializeField] private Button botoGacha;
        [SerializeField] private TMP_Text textMissatge;
        [SerializeField] private float segonsDelMissatge = 3f;

        [Header("Panells")]
        [SerializeField] private PanellDeLluitadors panellDeLluitadors;
        [SerializeField] private PanellDelGacha panellDelGacha;

        [Header("Botons")]
        [SerializeField] private Button botoTancar;
        [SerializeField] private Button botoAnterior;
        [SerializeField] private Button botoSeguent;

        private readonly List<PaisSO> _paisos = new List<PaisSO>();
        private int _index;
        private float _amagarMissatge;

        private void Awake()
        {
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoAnterior != null) botoAnterior.onClick.AddListener(() => Moure(-1));
            if (botoSeguent != null) botoSeguent.onClick.AddListener(() => Moure(1));
            if (botoRefrescar != null) botoRefrescar.onClick.AddListener(Refrescar_);
            if (botoLluitadors != null) botoLluitadors.onClick.AddListener(() => { if (panellDeLluitadors != null && Zona >= 0) panellDeLluitadors.Obrir(Zona); });
            if (botoGacha != null) botoGacha.onClick.AddListener(() => { if (panellDelGacha != null && Zona >= 0) panellDelGacha.Obrir(Zona); });
            foreach (PantaDeLArena p in pantans) if (p != null) p.Inicialitzar(this);
            if (textMissatge != null) textMissatge.text = string.Empty;
        }

        private void Start()
        {
            GestorDePaisos g = GestorDePaisos.Instancia;
            GestorDeLArena arena = GestorDeLArena.Instancia;
            if (g != null && arena != null)
            {
                foreach (PaisSO p in g.Paisos)
                    if (p != null && !p.esCiutatComercial && arena.Desbloquejada(g.ZonaDe(p))) _paisos.Add(p);
                _index = Mathf.Max(0, _paisos.IndexOf(g.PaisActual));
            }
            if (arena != null)
            {
                arena.EnCanvi += Refrescar;
                arena.EnBossMort += BossMort;
            }
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.EnCanvi += Refrescar;
            Textos.EnCanviDIdioma += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GestorDeLArena.Instancia != null)
            {
                GestorDeLArena.Instancia.EnCanvi -= Refrescar;
                GestorDeLArena.Instancia.EnBossMort -= BossMort;
            }
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.EnCanvi -= Refrescar;
            Textos.EnCanviDIdioma -= Refrescar;
        }

        private void Update()
        {
            if (_amagarMissatge > 0 && (_amagarMissatge -= Time.unscaledDeltaTime) <= 0 && textMissatge != null)
                textMissatge.text = string.Empty;
        }

        private PaisSO Pais => _index >= 0 && _index < _paisos.Count ? _paisos[_index] : null;
        private int Zona => Pais != null && GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.ZonaDe(Pais) : -1;

        private void Moure(int d)
        {
            int nou = Mathf.Clamp(_index + d, 0, _paisos.Count - 1);
            if (nou == _index) return;
            _index = nou;
            GestorDAudio.Sonar(So.Toc);
            if (panellDeLluitadors != null) panellDeLluitadors.Tancar();
            if (panellDelGacha != null) panellDelGacha.Tancar();
            Refrescar();
        }

        // Des d'un nenúfar: el panell de lluitadors, per triar qui hi va.
        public void TriarLluitador(int panta)
        {
            if (panellDeLluitadors != null && Zona >= 0) panellDeLluitadors.TriarPer(Zona, panta);
        }

        private void Refrescar()
        {
            GestorDeLArena arena = GestorDeLArena.Instancia;
            PaisSO pais = Pais;
            Mostrar(botoAnterior, _index > 0);
            Mostrar(botoSeguent, _index < _paisos.Count - 1);

            bool hiHa = pais != null && arena != null;
            if (contingut != null) contingut.SetActive(hiHa);
            if (textBloquejada != null)
            {
                textBloquejada.gameObject.SetActive(!hiHa);
                ArenaConfigSO config = ArenaConfigSO.Actual;
                textBloquejada.text = Textos.F("arena.bloquejada", config != null ? config.pantaQueDesbloqueja : 15);
            }
            if (!hiHa) return;

            int z = Zona;
            if (fons != null) fons.color = pais.colorDeFons;
            if (Camera.main != null) Camera.main.backgroundColor = pais.colorDeFons;
            if (gespa != null) gespa.Mostrar(pais);

            Posar(textNomDelPais, pais.NomVisible);
            if (textPunts != null)
            {
                ArbreDeRecercaSO arbre = ArbreDeRecercaSO.Actual;
                double punts = GestorDeRecerca.Instancia != null ? GestorDeRecerca.Instancia.Punts(z) : 0;
                textPunts.text = Textos.F("arena.punts", FormatadorDeNombres.Doblers(punts));
                textPunts.color = arbre != null ? arbre.ColorDe(z) : Color.white;
            }
            Posar(textTickets, Textos.F("arena.tickets", arena.TicketsComuns, arena.TicketsPremium));
            Posar(textCicle, Textos.F("arena.cicle", FormatadorDeNombres.Temps(arena.SegonsDelCicle(z))));
            int gratis = arena.RefrescosGratisQueQueden(z);
            Posar(textRefrescar, gratis > 0 ? Textos.F("arena.refresc_gratis", gratis)
                                            : Textos.F("arena.refresc_gemes", FormatadorDeNombres.Doblers(arena.GemesPerRefrescar) + Icones.Gema));

            foreach (PantaDeLArena p in pantans) if (p != null) p.Refrescar(z, pais);
        }

        private void Refrescar_()
        {
            GestorDeLArena arena = GestorDeLArena.Instancia;
            int z = Zona;
            if (arena == null || z < 0) return;
            if (arena.RefrescosGratisQueQueden(z) > 0)
            {
                Fer(arena, z);
                return;
            }
            FinestraDeConfirmacio.Preguntar(Textos.T("arena.refrescar"),
                Textos.F("arena.refrescar_text", FormatadorDeNombres.Doblers(arena.GemesPerRefrescar) + Icones.Gema),
                () => Fer(arena, z),
                textAcceptar: FormatadorDeNombres.Doblers(arena.GemesPerRefrescar) + Icones.Gema);
        }

        private void Fer(GestorDeLArena arena, int z)
        {
            bool fet = arena.Refrescar(z);
            GestorDAudio.Sonar(fet ? So.Acceptar : So.Error);
            if (!fet && PanellDeBotiga.Instancia != null) PanellDeBotiga.Instancia.ObrirA(SeccioDeBotiga.Gemes);
        }

        private void BossMort(int zona, BossALArena b, double punts)
        {
            if (zona != Zona || textMissatge == null) return;
            GestorDAudio.Sonar(So.Recompensa);
            textMissatge.text = Textos.F("arena.boss_mort", AparencaDeLArena.Nom(b), FormatadorDeNombres.Doblers(punts));
            _amagarMissatge = segonsDelMissatge;
        }

        private void Tancar()
        {
            GestorDAudio.Sonar(So.Tancar);
            if (GameManager.Instancia != null) GameManager.Instancia.TornarDeLEscena();
        }

        private static void Mostrar(Button b, bool visible)
        {
            if (b != null && b.gameObject.activeSelf != visible) b.gameObject.SetActive(visible);
        }

        private static void Posar(TMP_Text t, string text)
        {
            if (t != null) t.text = text ?? string.Empty;
        }
    }
}
