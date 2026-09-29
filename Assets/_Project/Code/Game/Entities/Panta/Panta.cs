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

        public void AssignarIndex(int index) => _index = index;

        // Un pantà neix per comprar, no comprat. El tren sí que neix a
        // nivell 1, per això el valor per defecte del camp heretat és 1
        // i aquí el corregim quan s'autora el prefab.
        private void Reset() => nivell = 0;

        private double MultiplicadorDeProfunditat => config.MultiplicadorDeProfunditat(_index);

        public double PreuDeCompra => config.PreuDeCompraAlIndex(_index);

        // ---------------------------------------------------------------
        // UnitatMillorable
        // ---------------------------------------------------------------

        public override Ranura Ranura => new Ranura(TipusDeLloc.Panta, _index);
        public override string NomVisible => $"Pantà {_index + 1}";
        public override double CostBase => config.costBase * MultiplicadorDeProfunditat;
        public override double CreixementDeCost => config.creixementDeCost;
        public override int NivellMaxim => config.nivellMaxim;

        public override void OmplirFiles(List<FilaEstadistica> files, int desti)
        {
            files.Add(new FilaEstadistica("Producció",
                FormatadorDeNombres.Ritme(ProduccioPerSegon),
                FormatadorDeNombres.Delta(ProduccioAlNivell(desti) - ProduccioPerSegon)));

            files.Add(new FilaEstadistica("Granotes",
                GranotesActives.ToString(),
                FormatadorDeNombres.Delta(GranotesAlNivell(desti) - GranotesActives)));

            // La velocitat de moviment no puja de nivell, igual que
            // Walking Speed a IMT, que sempre ensenya +0.
            files.Add(new FilaEstadistica("Velocitat",
                FormatadorDeNombres.Decimals(VelocitatDeMoviment, 1), "+0"));

            files.Add(new FilaEstadistica("Recol·lecció",
                FormatadorDeNombres.Ritme(VelocitatDeRecoleccio),
                FormatadorDeNombres.Delta(VelocitatDeRecoleccioAlNivell(desti) - VelocitatDeRecoleccio)));

            files.Add(new FilaEstadistica("Capacitat",
                FormatadorDeNombres.Doblers(CapacitatPerGranota),
                FormatadorDeNombres.Delta(CapacitatAlNivell(desti) - CapacitatPerGranota)));
        }

        // ---------------------------------------------------------------
        // Estadístiques: sempre calculades a partir del nivell.
        // El SO només guarda la corba, perquè és un asset compartit
        // entre tots els pantans i no pot guardar estat per instància.
        // ---------------------------------------------------------------

        // Profunditat i fites multipliquen els DOS stats, així que la producció
        // puja just aquell factor i el temps d'omplir es mou poc.
        //
        // El floor és el d'IMT: amb base 20 i 5 reprodueix les seves columnes
        // de capacitat i mineria exactes als deu primers nivells. I és el que
        // fa que el temps d'omplir oscil·li entre 4,0 i 4,4s en comptes de
        // quedar-se clavat, que és d'on surt la seva extracció a salts
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
        // Només l'anada: el "Total Extraction" d'IMT mesura des que comença
        // a recollir fins que deixa la mercaderia, no el cicle sencer. Als
        // seus nivells 1-10 aquest tram surt constant a 2,06s (camí + buidar).
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
                    suma += Vector3.Distance(n.position, capsaMosques.position);
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
        public float VelocitatDeMoviment => config.velocitatDeMoviment * (float)Bonus.velocitat;
        public float TempsBuidar => config.tempsBuidar / (float)Bonus.treball;

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

        public void InicialitzarPanta()
        {
            AplicarAspecte();

            for (int i = 0; i < slotsGranotes.Length; i++)
            {
                if (slotsGranotes[i] != null && nenufars.Length > i && nenufars[i] != null)
                {
                    slotsGranotes[i].Inicialitzar(
                        this, nenufars[i].position, pilaDeMosques.position, capsaMosques.position);
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
            if (aspecte == null) return;

            PosarSprite(renderitzadorDeFons, aspecte.fons, aspecte.tintDelFons);
            PosarSprite(renderitzadorDeCapsa, aspecte.capsa, null);

            if (nenufars != null)
                foreach (Transform t in nenufars)
                    if (t != null) PosarSprite(t.GetComponent<SpriteRenderer>(), aspecte.nenufar, null);

            if (slotsGranotes != null)
                foreach (ControladorGranota g in slotsGranotes)
                    if (g != null) PosarSprite(g.GetComponent<SpriteRenderer>(), aspecte.granota, null);
        }

        private static void PosarSprite(SpriteRenderer renderitzador, Sprite sprite, Color? tint)
        {
            if (renderitzador == null) return;
            if (sprite != null) renderitzador.sprite = sprite;
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
    }
}
