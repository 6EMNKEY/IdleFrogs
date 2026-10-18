using System;

namespace IdleFrogs.Game
{
    public enum TramDeLaCadena { Pantans, Tren, Tenda }

    // Les tres capacitats de la cadena, en doblers per segon:
    //   pantans — el que produeixen tots els pantans comprats a l'abast del tren
    //   tren    — el que el tren pot portar a l'estació
    //   tenda   — el que els transportistes poden portar a vendre
    // El més petit és el coll d'ampolla, i la producció idle n'és una desena.
    public readonly struct MesuresDeLaCadena
    {
        public const double DivisorIdle = 10;

        public readonly double pantans;
        public readonly double tren;
        public readonly double tenda;

        public MesuresDeLaCadena(double pantans, double tren, double tenda)
        {
            this.pantans = Math.Max(0, pantans);
            this.tren = Math.Max(0, tren);
            this.tenda = Math.Max(0, tenda);
        }

        public double CollDAmpolla => Math.Min(pantans, Math.Min(tren, tenda));

        // A igualtat, el primer de la cadena: és on el jugador ho notarà abans.
        public TramDeLaCadena QuinTram =>
            pantans <= tren && pantans <= tenda ? TramDeLaCadena.Pantans
            : tren <= tenda ? TramDeLaCadena.Tren
            : TramDeLaCadena.Tenda;

        public double Idle => CollDAmpolla / DivisorIdle;
    }

    // El que produeix una granota sense parar: recull fins a omplir-se, va a
    // la capsa, buida i torna al nenúfar. Les esperes tenen el mateix mínim
    // de 0,02 s que la corrutina de ControladorGranota.
    public static class CicleDeGranota
    {
        public static double Produccio(double capacitat, double segonsFinsPle, double segonsBuidar,
                                       double distanciaALaCapsa, double velocitat)
        {
            if (capacitat <= 0 || velocitat <= 0) return 0;
            double cicle = Math.Max(0.02, segonsFinsPle) + Math.Max(0.02, segonsBuidar)
                           + 2 * distanciaALaCapsa / velocitat;
            return capacitat / cicle;
        }
    }
}
