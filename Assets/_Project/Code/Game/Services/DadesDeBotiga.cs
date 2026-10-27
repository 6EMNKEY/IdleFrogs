using System;

namespace IdleFrogs.Game
{
    [Serializable]
    public class CompraGuardada
    {
        public string id;
        public int vegades;
        public long ultimaUtc;        // la darrera vegada (regal cada X hores)
        public int vegadesAvui;
        public int diaDeVegadesAvui;  // Calendari.Dia: si no és avui, vegadesAvui no compta
        public long primeraVistaUtc;  // les ofertes amb temps compten des d'aquí
    }

    // El que la botiga ha de recordar. És global, de tots els països.
    [Serializable]
    public class DadesDeBotiga
    {
        public CompraGuardada[] compres;

        public int diesDelCalendariRecollits;
        public int ultimDiaDelCalendari;

        public long paseFiTicks;          // DateTime.Date (local) en què s'acaba, exclòs
        public double paseGemesPerDia;
        public int ultimDiaDelPase;

        public bool x2Permanent;
        public bool senseAnuncis;
        public bool offlineX2;

        public MultiplicadorActiu[] multiplicadors;
    }
}
