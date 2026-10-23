using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum EstatDePanta { Bloquejat, EnVenda, Comprat }

    public class Panta : UnitatMillorable
    {
        [Header("ConfiguracioPanta")]
        [SerializeField] private PantaConfigSO config;

        [Header("Aspecte — la versió gràfica d'aquest pantà")]
        [SerializeField] private AspectePantaSO aspecte;
        [SerializeField] private SpriteRenderer renderitzadorDeFons;
        [SerializeField] private SpriteRenderer renderitzadorDeCapsa;

        [Header("Arrels visuals")]
        [Tooltip("Tot el que només es veu quan el pantà està comprat")]
        [SerializeField] private GameObject contingut;
        [Tooltip("El cartell amb el botó de comprar")]
        [SerializeField] private GameObject cartellDeVenda;

        [Header("Posicions")]
        [SerializeField] private Transform pilaDeMosques;
        [SerializeField] private Transform capsaMosques;
        [SerializeField] private Transform[] nenufars = new Transform[6];

        // On seu la granota respecte al centre del nenúfar. Sense això la granota
        // queda centrada sobre el nenúfar i sembla enfonsada a l'aigua; amb un
        // petit desplaçament cap amunt sembla que hi seu a sobre. Es veu a
        // l'escena seleccionant el pantà (cercles grocs).
        [Tooltip("On es posa la granota respecte al centre del nenúfar, en unitats del món")]
        [SerializeField] private Vector2 desplacamentDeLaGranota = Vector2.zero;

        // El mateix per quan la granota arriba a la capsa a buidar. Només mou la
        // granota: el tren segueix parant on és la capsa (PosicioCapsa).
        [Tooltip("On es posa la granota respecte a la capsa quan hi va a buidar, en unitats del món")]
        [SerializeField] private Vector2 desplacamentALaCapsa = Vector2.zero;

        [Header("Referencies")]
        [SerializeField] private ControladorGranota[] slotsGranotes = new ControladorGranota[6];

        [Header("Doblers a la capsa")]
        [SerializeField] private double doblersALaCapsa;

        // Li'l dona LlistaDePantans: el prefab és idèntic per a tots,
        // la diferència entre el pantà 1 i el 7 és la seva posició a la llista.
        private int _index;
        private EstatDePanta _estat = EstatDePanta.Bloquejat;

        public double DoblersALaCapsa => doblersALaCapsa;
        public Vector3 PosicioCapsa => capsaMosques.position;
        public EstatDePanta Estat => _estat;

        public event System.Action<double> EnCanviDeCapsa;
        public event System.Action EnCanviDEstat;

        // ---------------------------------------------------------------
        // Profunditat
        // ---------------------------------------------------------------

        // L'índex és el lloc a la fila (nom, ranura, guardat). La posició és la
        // de l'economia: el mateix índex més una per cada barrera de davant,
        // perquè cada barrera ocupa el lloc d'un pantà.
        private int _posicio;
        // Les barreres de davant l'abarateixen (vegeu TramsDePantans.FactorDePreu).
        private double _factorDePreu = 1;

        public void AssignarIndex(int index) => AssignarIndex(index, index, 1);

        public void AssignarIndex(int index, int posicio, double factorDePreu)
        {
            _index = index;
            _posicio = posicio;
            _factorDePreu = factorDePreu;
        }

        public double PreuALaPosicio(int posicio) => config.PreuDeCompraAlIndex(posicio);

        // Un pantà neix per comprar, no comprat. El tren sí que neix a
        // nivell 1, per això el valor per defecte del camp heretat és 1
        // i aquí el corregim quan s'autora el prefab.
        private void Reset() => nivell = 0;

        private double MultiplicadorDeProfunditat => config.MultiplicadorDeProfunditat(_posicio);

        public double PreuDeCompra => config.PreuDeCompraAlIndex(_posicio) * _factorDePreu;

        // ---------------------------------------------------------------
        // UnitatMillorable
        // ---------------------------------------------------------------

        public override Ranura Ranura => new Ranura(TipusDeLloc.Panta, _index);
        public override string NomVisible => Textos.F("unitat.panta", _index + 1);
        public override string TitolDeLesEstadistiques => Textos.T("titol.panta");
        public override double CostBase => config.costBase * MultiplicadorDeProfunditat;
        public override double CreixementDeCost => config.creixementDeCost;
        public override int NivellMaxim => config.nivellMaxim;

        public override FitaDeMultiplicador[] FitesDeMillora => config.fites;

        // ProduccioReal i no ProduccioPerSegon: amb encarregat, el número de
        // dalt ha de dir el que de debò cau a la capsa.
        public override FilaEstadistica Resum(int desti) => new FilaEstadistica(TipusDEstadistica.Resum,
            Textos.T("stat.produccio_total"),
            FormatadorDeNombres.Ritme(ProduccioReal),
            FormatadorDeNombres.Ritme(ProduccioRealAlNivell(desti)));

        // Els valors de destí porten la mateixa bonificació que els d'ara;
        // si no, amb encarregat la fletxa diria que millorar et fa baixar.
        public override void OmplirFiles(List<FilaEstadistica> files, int desti)
        {
            // La velocitat de moviment no puja de nivell: surt sense fletxa.
            string velocitat = FormatadorDeNombres.Decimals(VelocitatDeMoviment, 1);
            files.Add(new FilaEstadistica(TipusDEstadistica.VelocitatDeGranota,
                Textos.T("stat.velocitat"), velocitat, velocitat));

            files.Add(new FilaEstadistica(TipusDEstadistica.Granotes,
                Textos.T("stat.granotes"),
                GranotesActives.ToString(),
                GranotesAlNivell(desti).ToString()));

            files.Add(new FilaEstadistica(TipusDEstadistica.Recollecio,
                Textos.T("stat.recollecio"),
                FormatadorDeNombres.Ritme(VelocitatDeRecoleccio),
                FormatadorDeNombres.Ritme(VelocitatDeRecoleccioAlNivell(desti) * Bonus.treball)));

            files.Add(new FilaEstadistica(TipusDEstadistica.CapacitatDeGranota,
                Textos.T("stat.capacitat"),
                FormatadorDeNombres.Doblers(CapacitatPerGranota),
                FormatadorDeNombres.Doblers(CapacitatAlNivell(desti) * Bonus.capacitat)));
        }

        // ---------------------------------------------------------------
        // Estadístiques: sempre calculades a partir del nivell.
        // El SO només guarda la corba, perquè és un asset compartit
        // entre tots els pantans i no pot guardar estat per instància.
        // ---------------------------------------------------------------

        // Profunditat i fites multipliquen els DOS stats, així que la producció
        // puja just aquell factor i el temps d'omplir es mou poc.
        //
        // El floor fa que el temps d'omplir oscil·li entre 4,0 i 4,4s en
        // comptes de quedar-se clavat, i que la producció pugi a salts
        // (3,3 → 3,4 → 4 → 4,1) en comptes de pujar llisa.
        //
        // Màxim amb 1 perquè una base petita no doni 0 i parteixi la divisió.
        public double CapacitatAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.capacitatBase * MultiplicadorDeProfunditat
                * System.Math.Pow(config.creixementDeCapacitat, n - 1)
                * Fites.Acumulat(config.fites, n)));

        public double VelocitatDeRecoleccioAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.velocitatDeRecoleccioBase * MultiplicadorDeProfunditat
                * System.Math.Pow(config.creixementDeVelocitatDeRecoleccio, n - 1)
                * Fites.Acumulat(config.fites, n)));

        // Derivat: la granota recull fins a omplir-se.
        public float TempsFinsPleAlNivell(int n)
        {
            double velocitat = VelocitatDeRecoleccioAlNivell(n);
            if (velocitat <= 0) return 0f;
            return (float)(CapacitatAlNivell(n) / velocitat);
        }

        public int GranotesAlNivell(int n)
        {
            if (n < 1) return 0;
            int total = config.granotesInicials;
            if (config.nivellsDeGranotaExtra != null)
            {
                foreach (int fita in config.nivellsDeGranotaExtra)
                    if (n >= fita) total++;
            }
            return Mathf.Clamp(total, 0, slotsGranotes.Length);
        }

        // El camí del nenúfar a la capsa, tret de la geometria de l'escena.
        // Només l'anada: la producció es mesura des que comença a recollir
        // fins que deixa la mercaderia a la capsa, no el cicle sencer.
        public float TempsDeCami
        {
            get
            {
                if (capsaMosques == null || nenufars == null) return 0f;

                float suma = 0f;
                int compte = 0;
                foreach (Transform n in nenufars)
                {
                    if (n == null) continue;
                    suma += Vector3.Distance(SeientDeLaGranota(n), LlocDeBuidar);
                    compte++;
                }
                if (compte == 0) return 0f;

                float velocitat = VelocitatDeMoviment;
                if (velocitat <= 0) return 0f;
                return (suma / compte) / velocitat;
            }
        }

        // Doblers per segon, comptant des que comença a recollir fins que
        // deixa la càrrega a la capsa.
        public double ProduccioAlNivell(int n) =>
            GranotesAlNivell(n) * CapacitatAlNivell(n)
            / (TempsFinsPleAlNivell(n) + TempsBuidar + TempsDeCami);

        // Les bonificacions s'apliquen aquí i no a *AlNivell(), que es fan
        // servir per a les projeccions del panell.
        public double CapacitatPerGranota => CapacitatAlNivell(nivell) * Bonus.capacitat;
        public double VelocitatDeRecoleccio => VelocitatDeRecoleccioAlNivell(nivell) * Bonus.treball;

        public float TempsFinsPle =>
            VelocitatDeRecoleccio <= 0 ? 0f : (float)(CapacitatPerGranota / VelocitatDeRecoleccio);
        public int GranotesActives => GranotesAlNivell(nivell);
        public double ProduccioPerSegon => ProduccioAlNivell(nivell);

        // La mateixa definició que el número del panell, però amb les
        // bonificacions de l'encarregat. És el que es fa servir per al
        // progrés offline: el jugador veu "X/s" i espera rebre X × temps.
        public double ProduccioReal => ProduccioRealAlNivell(nivell);

        // La mateixa fórmula a qualsevol nivell, amb les bonificacions d'ara:
        // és el número de destí de la targeta del panell.
        //
        // Granota per granota, amb el cicle sencer que fa de debò: recollir,
        // anar a la capsa, buidar i TORNAR, i cadascuna des del seu nenúfar
        // (no tots són a la mateixa distància).
        public double ProduccioRealAlNivell(int n)
        {
            double capacitat = CapacitatAlNivell(n) * Bonus.capacitat;
            double velocitat = VelocitatDeRecoleccioAlNivell(n) * Bonus.treball;
            double finsPle = velocitat <= 0 ? 0 : capacitat / velocitat;
            float caminar = VelocitatDeMoviment;
            if (capsaMosques == null || nenufars == null || slotsGranotes == null) return 0;

            double total = 0;
            int granotes = GranotesAlNivell(n);
            for (int i = 0; i < granotes && i < slotsGranotes.Length; i++)
            {
                if (slotsGranotes[i] == null || i >= nenufars.Length || nenufars[i] == null) continue;
                float distancia = Vector3.Distance(SeientDeLaGranota(nenufars[i]), LlocDeBuidar);
                total += CicleDeGranota.Produccio(capacitat, finsPle, TempsBuidar, distancia, caminar);
            }
            return total;
        }
        public float VelocitatDeMoviment => config.velocitatDeMoviment * (float)Bonus.velocitat;
        public float TempsBuidar => config.tempsBuidar / (float)Bonus.treball;

        // Les mosques que volen pel pantà: les llengües de les granotes en
        // cacen. Es busca la primera vegada; el pantà pot no tenir-ne.
        private EixamDeMosques _eixam;
        private bool _eixamBuscat;
        public EixamDeMosques Eixam
        {
            get
            {
                if (!_eixamBuscat) { _eixam = GetComponentInChildren<EixamDeMosques>(true); _eixamBuscat = true; }
                return _eixam;
            }
        }

        // ---------------------------------------------------------------
        // Compra
        // ---------------------------------------------------------------

        public bool PucPagarLaCompra =>
            CurrencyManager.Instancia != null &&
            CurrencyManager.Instancia.doblersActuals >= PreuDeCompra;

        public bool Comprar()
        {
            if (EstaComprat || _estat != EstatDePanta.EnVenda) return false;
            if (CurrencyManager.Instancia == null) return false;
            if (!CurrencyManager.Instancia.GastarDoblers(PreuDeCompra)) return false;

            AplicarNivell(1);
            return true;
        }

        // ---------------------------------------------------------------
        // Estat visual — el decideix LlistaDePantans, que és qui sap
        // si la barrera del davant està oberta i si el pantà anterior
        // ja s'ha comprat.
        // ---------------------------------------------------------------

        public void AplicarEstat(EstatDePanta estat)
        {
            _estat = estat;
            if (contingut != null) contingut.SetActive(estat == EstatDePanta.Comprat);
            if (cartellDeVenda != null) cartellDeVenda.SetActive(estat == EstatDePanta.EnVenda);
            EnCanviDEstat?.Invoke();
        }

        // ---------------------------------------------------------------
        // Arrencada — la dirigeix LlistaDePantans, no un Start() propi,
        // perquè ha de passar DESPRÉS de carregar els nivells guardats.
        // ---------------------------------------------------------------

        // On es posa la granota d'aquest nenúfar.
        private Vector3 SeientDeLaGranota(Transform nenufar) =>
            nenufar.position + (Vector3)desplacamentDeLaGranota;

        // On es posa la granota quan buida a la capsa.
        private Vector3 LlocDeBuidar => capsaMosques.position + (Vector3)desplacamentALaCapsa;

        public void InicialitzarPanta()
        {
            AplicarAspecte();

            for (int i = 0; i < slotsGranotes.Length; i++)
            {
                if (slotsGranotes[i] != null && nenufars.Length > i && nenufars[i] != null)
                {
                    slotsGranotes[i].Inicialitzar(
                        this, SeientDeLaGranota(nenufars[i]), pilaDeMosques.position, LlocDeBuidar);
                }
            }
            ActualitzarGranotes();
        }

        protected override void DespresDeCanviDeNivell() => ActualitzarGranotes();

        // Amb encarregat assignat les granotes no esperen cap dit: se'ls
        // demana d'arrencar cada frame i les que ja treballen l'ignoren.
        private void Update()
        {
            if (!EstaAutomatitzada || !EstaComprat) return;

            for (int i = 0; i < slotsGranotes.Length; i++)
            {
                if (slotsGranotes[i] == null || !slotsGranotes[i].gameObject.activeSelf) continue;
                slotsGranotes[i].IntentarEngegar();
            }
        }

        // ---------------------------------------------------------------
        // Aspecte
        // ---------------------------------------------------------------

        private void AplicarAspecte()
        {
            // El del país, si en té; si no, el del pantà.
            AspectePantaSO delPais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.AspecteDelsPantans : null;
            AspectePantaSO a = delPais != null ? delPais : aspecte;
            if (a == null) return;

            PosarSprite(renderitzadorDeFons, a.fons, a.tintDelFons);
            PosarSprite(renderitzadorDeCapsa, a.capsa, null);

            if (nenufars != null)
                foreach (Transform t in nenufars)
                    if (t != null) PosarSprite(t.GetComponent<SpriteRenderer>(), a.nenufar, null);

            if (slotsGranotes != null)
                foreach (ControladorGranota g in slotsGranotes)
                    if (g != null) PosarSprite(g.GetComponent<SpriteRenderer>(), a.granota, null);
        }

        private static void PosarSprite(SpriteRenderer renderitzador, Sprite sprite, Color? tint)
        {
            if (renderitzador == null) return;
            // Damunt de l'aspecte, els canvis de sprites del país.
            if (sprite != null) renderitzador.sprite = SpritesDelPais.Substituir(sprite);
            if (tint.HasValue) renderitzador.color = tint.Value;
        }

#if UNITY_EDITOR
        // Perquè es vegi a l'editor sense haver de donar-li al Play: si no,
        // no pots compondre l'escena mirant-la.
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            AplicarAspecte();
        }
#endif

        private void ActualitzarGranotes()
        {
            int actives = GranotesActives;
            for (int i = 0; i < slotsGranotes.Length; i++)
            {
                if (slotsGranotes[i] == null) continue;
                slotsGranotes[i].gameObject.SetActive(i < actives);
            }
        }

        // ---------------------------------------------------------------
        // La capsa
        // ---------------------------------------------------------------

        public void AfegirMosquesALaCapsa(double quantitat)
        {
            if (quantitat <= 0) return;
            doblersALaCapsa += quantitat;
            EnCanviDeCapsa?.Invoke(doblersALaCapsa);
        }

        public double RetirarDeLaCapsa(double maxim)
        {
            if (maxim <= 0 || doblersALaCapsa <= 0) return 0;
            double retirat = System.Math.Min(maxim, doblersALaCapsa);
            doblersALaCapsa -= retirat;
            EnCanviDeCapsa?.Invoke(doblersALaCapsa);
            return retirat;
        }

        // Per al carregador de partida
        public void AplicarCapsa(double quantitat)
        {
            doblersALaCapsa = quantitat < 0 ? 0 : quantitat;
            EnCanviDeCapsa?.Invoke(doblersALaCapsa);
        }

#if UNITY_EDITOR
        // Seleccionant el pantà es veu on seurà cada granota, sense haver de
        // donar al Play: així el desplaçament s'ajusta mirant l'escena.
        private void OnDrawGizmosSelected()
        {
            if (nenufars == null) return;
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            foreach (Transform n in nenufars)
            {
                if (n == null) continue;
                Vector3 seient = SeientDeLaGranota(n);
                Gizmos.DrawWireSphere(seient, 0.12f);
                Gizmos.DrawLine(n.position, seient);
            }

            if (capsaMosques == null) return;
            Gizmos.color = new Color(0.3f, 0.9f, 1f);
            Gizmos.DrawWireSphere(LlocDeBuidar, 0.12f);
            Gizmos.DrawLine(capsaMosques.position, LlocDeBuidar);
        }
#endif
    }
}
