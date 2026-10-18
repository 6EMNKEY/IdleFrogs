using System;

namespace IdleFrogs.Game
{
    // El que has guanyat mentre no hi eres: la producció idle pel temps que
    // has estat fora, amb un tope. No es cobra en carregar sinó quan el
    // jugador accepta el panell; fins llavors queda guardada a la partida,
    // i si tornes a marxar abans d'acceptar-la, la nova s'hi suma.
    [Serializable]
    public struct RecompensaOffline
    {
        public double segonsFora;       // els reals
        public double segonsComptats;   // amb el tope aplicat
        public double doblers;

        public bool HiHaRecompensa => doblers > 0;
        public bool HiHaTope => segonsFora > segonsComptats + 0.5;

        // El ritme mitjà: si s'hi han sumat dues absències amb idles diferents,
        // "segons × ritme" continua donant el total.
        public double PerSegon => segonsComptats > 0 ? doblers / segonsComptats : 0;

        public static RecompensaOffline Calcular(double segonsFora, double segonsMaxims, double idlePerSegon)
        {
            if (segonsFora <= 0 || idlePerSegon <= 0) return default;
            double comptats = segonsMaxims > 0 ? Math.Min(segonsFora, segonsMaxims) : segonsFora;
            return new RecompensaOffline
            {
                segonsFora = segonsFora,
                segonsComptats = comptats,
                doblers = comptats * idlePerSegon
            };
        }

        public RecompensaOffline Sumar(RecompensaOffline altra) => new RecompensaOffline
        {
            segonsFora = segonsFora + altra.segonsFora,
            segonsComptats = segonsComptats + altra.segonsComptats,
            doblers = doblers + altra.doblers
        };
    }
}
