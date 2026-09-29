using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // El tercer coll d'ampolla. La granota transportista treu doblers de
    // l'estació i els porta fins aquí; en arribar es venen i entren a la
    // cartera. El nivell de la tenda és el que mana sobre la granota:
    // quant carrega per viatge i com de ràpid camina.
    public class Tenda : UnitatMillorable
    {
        [Header("Configuració")]
        [SerializeField] private TendaConfigSO config;

        [Header("Referències")]
        [SerializeField] private Estacio estacio;
        [Tooltip("Una per ranura. Se n'activen segons el nivell, com les granotes del pantà.")]
        [SerializeField] private GranotaTransportista[] transportistes;

        [Header("Posicions")]
        [Tooltip("On espera la granota, al costat de l'estació")]
        [SerializeField] private Transform puntDeRecollida;
        [Tooltip("On deixa la càrrega, dins de la tenda")]
        [SerializeField] private Transform puntDeVenda;

        // ---------------------------------------------------------------
        // UnitatMillorable
        // ---------------------------------------------------------------

        public override Ranura Ranura => new Ranura(TipusDeLloc.Tenda);
        public override string NomVisible => "Tenda";
        public override double CostBase => config.costBase;
        public override double CreixementDeCost => config.creixementDeCost;
        public override int NivellMaxim => config.nivellMaxim;

        // Floor com al pantà i al tren: la càrrega per viatge és un número
        // enter. La velocitat de moviment es queda amb decimals, que creix
        // a l'1% i arrodonida es quedaria congelada trenta nivells.
        public double CapacitatAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.capacitatBase * System.Math.Pow(config.creixementDeCapacitat, n - 1)
                * Fites.Acumulat(config.fites, n)));

        public float VelocitatAlNivell(int n) =>
            Mathf.Min(config.velocitatMaxima,
                      config.velocitatBase * Mathf.Pow(config.creixementDeVelocitat, n - 1)
                      * (float)Fites.Acumulat(config.fites, n));

        public int TransportistesAlNivell(int n)
        {
            if (n < 1) return 0;
            int total = config.transportistesInicials;
            if (config.nivellsDeTransportistaExtra != null)
                foreach (int fita in config.nivellsDeTransportistaExtra)
                    if (n >= fita) total++;
            return Mathf.Clamp(total, 0, transportistes != null ? transportistes.Length : 0);
        }

        // Nominal: ignora el temps de camí, igual que el número de IMT
        // i que el que ensenya el pantà.
        public double ProduccioAlNivell(int n) =>
            TransportistesAlNivell(n) * CapacitatAlNivell(n)
            / (config.tempsDeCarrega + config.tempsDeDescarrega);

        public int TransportistesActius => TransportistesAlNivell(nivell);

        // Multiplicadors fora dels clamps, com al tren.
        public double CapacitatPerViatge => CapacitatAlNivell(nivell) * Bonus.capacitat;
        public float Velocitat => VelocitatAlNivell(nivell) * (float)Bonus.velocitat;
        public double ProduccioPerSegon => ProduccioAlNivell(nivell);
        public float TempsDeCarrega => Mathf.Max(0.02f, config.tempsDeCarrega / (float)Bonus.treball);
        public float TempsDeDescarrega => Mathf.Max(0.02f, config.tempsDeDescarrega / (float)Bonus.treball);

        public override void OmplirFiles(List<FilaEstadistica> files, int desti)
        {
            files.Add(new FilaEstadistica("Venda",
                FormatadorDeNombres.Ritme(ProduccioPerSegon),
                FormatadorDeNombres.Delta(ProduccioAlNivell(desti) - ProduccioPerSegon)));

            files.Add(new FilaEstadistica("Per viatge",
                FormatadorDeNombres.Doblers(CapacitatPerViatge),
                FormatadorDeNombres.Delta(CapacitatAlNivell(desti) - CapacitatPerViatge)));

            files.Add(new FilaEstadistica("Transportistes",
                TransportistesActius.ToString(),
                FormatadorDeNombres.Delta(TransportistesAlNivell(desti) - TransportistesActius)));

            files.Add(new FilaEstadistica("Velocitat",
                FormatadorDeNombres.Decimals(Velocitat, 2),
                FormatadorDeNombres.Delta(VelocitatAlNivell(desti) - Velocitat)));
        }

        // ---------------------------------------------------------------
        // Arrencada — la dirigeix GameManager, com els pantans, perquè ha
        // de passar després de carregar el nivell guardat.
        // ---------------------------------------------------------------

        public void InicialitzarTenda()
        {
            if (estacio == null || puntDeRecollida == null || puntDeVenda == null ||
                transportistes == null || transportistes.Length == 0)
            {
                Debug.LogError("Tenda: falten referències (estació, transportistes o punts)", this);
                return;
            }

            foreach (GranotaTransportista g in transportistes)
                if (g != null)
                    g.Inicialitzar(this, estacio, puntDeRecollida.position, puntDeVenda.position);

            ActualitzarTransportistes();
        }

        protected override void DespresDeCanviDeNivell() => ActualitzarTransportistes();

        private void ActualitzarTransportistes()
        {
            if (transportistes == null) return;
            int actius = TransportistesActius;
            for (int i = 0; i < transportistes.Length; i++)
            {
                if (transportistes[i] == null) continue;
                transportistes[i].gameObject.SetActive(i < actius);
            }
        }

        private void Update()
        {
            if (!EstaAutomatitzada || transportistes == null) return;

            for (int i = 0; i < transportistes.Length; i++)
            {
                if (transportistes[i] == null || !transportistes[i].gameObject.activeSelf) continue;
                transportistes[i].IntentarEngegar();
            }
        }

        // La crida la granota en arribar
        public void Vendre(double quantitat)
        {
            if (quantitat <= 0) return;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(quantitat);
        }

        // La suma de tots: el guardat la retorna sencera a la cartera.
        public double CarregaDelTransportista
        {
            get
            {
                if (transportistes == null) return 0;
                double total = 0;
                foreach (GranotaTransportista g in transportistes)
                    if (g != null) total += g.Carrega;
                return total;
            }
        }
    }
}
