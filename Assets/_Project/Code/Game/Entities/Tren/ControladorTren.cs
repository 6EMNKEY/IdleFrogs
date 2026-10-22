using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum EstatTren { Esperant, Anant, Carregant, Tornant, Descarregant }

    public class ControladorTren : UnitatMillorable, IEsPotTocar
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
        // Just abans de descarregar a l'estació, amb els segons que hi estarà.
        public event System.Action<float> EnComencaADescarregar;

        // ---------------------------------------------------------------
        // UnitatMillorable
        // ---------------------------------------------------------------

        public override Ranura Ranura => new Ranura(TipusDeLloc.Tren);
        public override string NomVisible => Textos.T("unitat.tren");
        public override string TitolDeLesEstadistiques => Textos.T("titol.tren");
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

        public override FitaDeMultiplicador[] FitesDeMillora => config.fites;

        // El número gran del tren és el que pot portar a l'estació per segon;
        // la capacitat va a les caixes amb la resta.
        public override FilaEstadistica Resum(int desti) => new FilaEstadistica(TipusDEstadistica.Resum,
            Textos.T("stat.transport_total"),
            FormatadorDeNombres.Ritme(CabalReal),
            FormatadorDeNombres.Ritme(CabalRealAlNivell(desti)));

        public override void OmplirFiles(List<FilaEstadistica> files, int desti)
        {
            files.Add(new FilaEstadistica(TipusDEstadistica.CapacitatDelTren,
                Textos.T("stat.capacitat"),
                FormatadorDeNombres.Doblers(Capacitat),
                FormatadorDeNombres.Doblers(CapacitatAlNivell(desti) * Bonus.capacitat)));

            files.Add(new FilaEstadistica(TipusDEstadistica.VelocitatDelTren,
                Textos.T("stat.velocitat"),
                FormatadorDeNombres.Decimals(VelocitatDeMoviment, 2),
                FormatadorDeNombres.Decimals(VelocitatAlNivell(desti) * Bonus.velocitat, 2)));

            // Una sola caixa: pugen juntes, i normalment van al mateix ritme.
            files.Add(new FilaEstadistica(TipusDEstadistica.Carrega,
                Textos.T("stat.carrega"),
                Ritmes(nivell),
                Ritmes(desti)));
        }

        // "150/s", o "150/s · 300/s" si la descàrrega va a un altre ritme.
        private string Ritmes(int n)
        {
            string carrega = FormatadorDeNombres.Ritme(RitmeDeCarregaAlNivell(n) * Bonus.treball);
            string descarrega = FormatadorDeNombres.Ritme(RitmeDeDescarregaAlNivell(n) * Bonus.treball);
            return carrega == descarrega ? carrega : $"{carrega} · {descarrega}";
        }

        // Doblers per segon que el tren pot moure fent viatges sencers fins al
        // pantà comprat més llunyà: capacitat / (anada i tornada + carregar +
        // descarregar). És la mesura de la cadena i el que fa servir l'offline.
        public double CabalReal => CabalRealAlNivell(nivell);

        // La mateixa fórmula a qualsevol nivell, amb les bonificacions d'ara:
        // és el número de destí de la targeta del panell.
        public double CabalRealAlNivell(int n)
        {
            if (llistaDePantans == null || puntDeDescarrega == null) return 0;

            int maxim = Mathf.Min(llistaDePantans.IndexMaximAccessible(), llistaDePantans.Quantitat - 1);
            Panta mesLlunya = null;
            for (int i = maxim; i >= 0; i--)
            {
                Panta p = llistaDePantans.Obtenir(i);
                if (p != null && p.EstaComprat) { mesLlunya = p; break; }
            }
            if (mesLlunya == null) return 0;

            float velocitat = VelocitatAlNivell(n) * (float)Bonus.velocitat;
            if (velocitat <= 0) return 0;

            double distancia = Mathf.Abs(mesLlunya.PosicioCapsa.x - puntDeDescarrega.position.x);
            double c = CapacitatAlNivell(n) * Bonus.capacitat;
            double temps = 2 * distancia / velocitat
                           + SegonsPerTransferir(c, RitmeDeCarregaAlNivell(n) * Bonus.treball)
                           + SegonsPerTransferir(c, RitmeDeDescarregaAlNivell(n) * Bonus.treball);
            return temps <= 0 ? 0 : c / temps;
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
        // granotes. Amb encarregat, qui crida IntentarEngegar() és Update().
        public void EnTocar() => IntentarEngegar();

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
            float segonsDeDescarrega = SegonsPerTransferir(carregaActual, RitmeDeDescarrega);
            EnComencaADescarregar?.Invoke(segonsDeDescarrega);
            yield return new WaitForSeconds(segonsDeDescarrega);

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
