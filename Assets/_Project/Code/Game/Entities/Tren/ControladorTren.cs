using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum EstatTren { Esperant, Anant, Carregant, Tornant, Descarregant }

    public class ControladorTren : UnitatMillorable
    {
        [Header("Configuració")]
        [SerializeField] private TrenConfigSO config;

        [Header("Referències")]
        [SerializeField] private LlistaDePantans llistaDePantans;
        [Tooltip("On para el tren (normalment el transform de l'estació)")]
        [SerializeField] private Transform puntDeDescarrega;
        [Tooltip("Qui rep la càrrega. Sense estació, va directe a la cartera.")]
        [SerializeField] private Estacio estacio;

        [Header("Estat")]
        public EstatTren estatActual = EstatTren.Esperant;
        [SerializeField] private double carregaActual;

        public double CarregaActual => carregaActual;
        public event System.Action<double> EnCanviDeCarrega;

        // ---------------------------------------------------------------
        // UnitatMillorable
        // ---------------------------------------------------------------

        public override Ranura Ranura => new Ranura(TipusDeLloc.Tren);
        public override string NomVisible => "Tren i estació";
        public override double CostBase => config.costBase;
        public override double CreixementDeCost => config.creixementDeCost;
        public override int NivellMaxim => config.nivellMaxim;

        // Floor als stats de cabal, com al pantà: números enters i el temps
        // de càrrega (capacitat/ritme) oscil·la una mica en comptes de
        // quedar-se clavat. La velocitat de moviment no es toca: creix
        // a l'1% i arrodonida es quedaria congelada trenta nivells.
        public double CapacitatAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.capacitatBase * System.Math.Pow(config.creixementDeCapacitat, n - 1)
                * Fites.Acumulat(config.fites, n)));

        public float VelocitatAlNivell(int n) =>
            Mathf.Min(config.velocitatMaxima,
                      config.velocitatBase * Mathf.Pow(config.creixementDeVelocitat, n - 1));

        public double RitmeDeCarregaAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.ritmeDeCarregaBase * System.Math.Pow(config.creixementDeRitme, n - 1)
                * Fites.Acumulat(config.fites, n)));

        public double RitmeDeDescarregaAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.ritmeDeDescarregaBase * System.Math.Pow(config.creixementDeRitme, n - 1)
                * Fites.Acumulat(config.fites, n)));

        // Els multiplicadors van FORA de VelocitatAlNivell perquè aquella té
        // el clamp de velocitatMaxima a dins.
        public double Capacitat => CapacitatAlNivell(nivell) * Bonus.capacitat;
        public float VelocitatDeMoviment => VelocitatAlNivell(nivell) * (float)Bonus.velocitat;
        public double RitmeDeCarrega => RitmeDeCarregaAlNivell(nivell) * Bonus.treball;
        public double RitmeDeDescarrega => RitmeDeDescarregaAlNivell(nivell) * Bonus.treball;

        public override void OmplirFiles(List<FilaEstadistica> files, int desti)
        {
            files.Add(new FilaEstadistica("Capacitat",
                FormatadorDeNombres.Doblers(Capacitat),
                FormatadorDeNombres.Delta(CapacitatAlNivell(desti) - Capacitat)));

            files.Add(new FilaEstadistica("Velocitat",
                FormatadorDeNombres.Decimals(VelocitatDeMoviment, 2),
                FormatadorDeNombres.Delta(VelocitatAlNivell(desti) - VelocitatDeMoviment)));

            files.Add(new FilaEstadistica("Càrrega",
                FormatadorDeNombres.Ritme(RitmeDeCarrega),
                FormatadorDeNombres.Delta(RitmeDeCarregaAlNivell(desti) - RitmeDeCarrega)));

            files.Add(new FilaEstadistica("Descàrrega",
                FormatadorDeNombres.Ritme(RitmeDeDescarrega),
                FormatadorDeNombres.Delta(RitmeDeDescarregaAlNivell(desti) - RitmeDeDescarrega)));
        }

        // Un ritme de 0 donaria una espera infinita i penjaria la corrutina.
        private static float SegonsPerTransferir(double quantitat, double ritme)
        {
            if (quantitat <= 0) return 0f;
            if (ritme <= 0) return 0f;
            return (float)(quantitat / ritme);
        }

        // ---------------------------------------------------------------
        // Bucle
        // ---------------------------------------------------------------

        private void Start()
        {
            transform.position = PosicioSobreLaVia(puntDeDescarrega.position);
            estatActual = EstatTren.Esperant;
            AvisarCarrega();
        }

        // El tren espera a l'estació fins que el toques, igual que les
        // granotes. Quan hi hagi encarregats, qui cridarà IntentarEngegar()
        // serà l'automatisme i no el dit.
        private void OnMouseDown() => IntentarEngegar();

        private void Update()
        {
            if (EstaAutomatitzada) IntentarEngegar();
        }

        public bool IntentarEngegar()
        {
            if (estatActual != EstatTren.Esperant) return false;
            if (llistaDePantans == null || puntDeDescarrega == null) return false;

            // Amb càrrega pendent sí que val la pena sortir encara que no
            // hi hagi res per recollir: cal anar a deixar-la.
            if (carregaActual <= 0 && llistaDePantans.IndexMesLlunyaAmbCarrega() < 0)
                return false;

            StartCoroutine(ViatgeDelTren());
            return true;
        }

        // El tren només es mou en X: conserva sempre la seva Y i Z.
        // Per això li és igual que la capsa quedi a dalt o a baix de la via.
        private Vector3 PosicioSobreLaVia(Vector3 objectiu)
        {
            return new Vector3(objectiu.x, transform.position.y, transform.position.z);
        }

        private IEnumerator MouresA(Vector3 objectiu)
        {
            Vector3 desti = PosicioSobreLaVia(objectiu);
            while (Vector3.Distance(transform.position, desti) >= 0.05f)
            {
                // Es llegeix cada frame, no una vegada: el tren pot pujar
                // de nivell mentre està en marxa.
                transform.position = Vector3.MoveTowards(
                    transform.position, desti, VelocitatDeMoviment * Time.deltaTime);
                yield return null;
            }
            transform.position = desti;
        }

        // Un sol viatge: anada, tornada i para. No es reenganxa sol.
        private IEnumerator ViatgeDelTren()
        {
            int mesLlunya = llistaDePantans.IndexMesLlunyaAmbCarrega();

            // 1. Anada: para i carrega a cada pantà que tingui doblers
            for (int i = 0; i <= mesLlunya; i++)
            {
                if (carregaActual >= Capacitat) break;

                Panta panta = llistaDePantans.Obtenir(i);
                if (panta == null || panta.DoblersALaCapsa <= 0) continue;

                estatActual = EstatTren.Anant;
                yield return MouresA(panta.PosicioCapsa);

                // Esperem primer i retirem després: si retiréssim abans,
                // tancar el joc enmig de la càrrega perdria els doblers,
                // que ja no serien ni de la capsa ni del tren.
                estatActual = EstatTren.Carregant;
                double espai = Capacitat - carregaActual;
                double previst = System.Math.Min(espai, panta.DoblersALaCapsa);
                yield return new WaitForSeconds(SegonsPerTransferir(previst, RitmeDeCarrega));

                carregaActual += panta.RetirarDeLaCapsa(espai);
                AvisarCarrega();
            }

            // 2. Tornada directa, sense parar enlloc
            estatActual = EstatTren.Tornant;
            yield return MouresA(puntDeDescarrega.position);

            // 3. Descarregar — a cabal, no a temps fix. L'estació no té
            //    tope: el que no se'n dugui la granota s'hi va acumulant.
            estatActual = EstatTren.Descarregant;
            yield return new WaitForSeconds(SegonsPerTransferir(carregaActual, RitmeDeDescarrega));

            if (estacio != null) estacio.Descarregar(carregaActual);
            else if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(carregaActual);

            carregaActual = 0;
            AvisarCarrega();

            // Torna a quedar-se a l'espera: sense això no es podria tornar a tocar.
            estatActual = EstatTren.Esperant;
        }

        private void AvisarCarrega() => EnCanviDeCarrega?.Invoke(carregaActual);

        // Per al carregador de partida
        public void AplicarCarrega(double quantitat)
        {
            carregaActual = quantitat < 0 ? 0 : quantitat;
            AvisarCarrega();
        }
    }
}
