using System;

namespace IdleFrogs.Game
{
    [Serializable]
    public class BarreraGuardada
    {
        public bool iniciada;
        public bool oberta;
        public long vencimentUtc;
    }

    [Serializable]
    public class EncarregatGuardat
    {
        public string idDefinicio;   // EncarregatSO.id, no l'índex de l'array
        public int idInstancia;      // en pots tenir dos d'iguals a ranures diferents
        public bool assignat;
        public int llocTipus;        // TipusDeLloc
        public int llocIndex;        // índex del pantà; 0 per a tren i tenda
        public long fiDeLHabilitatUtc;
        public long fiDeRecarregaUtc;
        // El que va costar; vendre'l en torna la meitat. 0 a les partides
        // d'abans, i llavors el gestor el reconstrueix.
        public double preuPagat;
    }

    // Tot el que pertany a una zona (un mapa: els seus pantans, el seu tren,
    // la seva tenda). Els encarregats també hi van: són de cada zona i no
    // es passen d'una a l'altra.
    [Serializable]
    public class ZonaGuardada
    {
        // L'índex de l'array és l'identificador de cada pantà. 0 = no comprat.
        public int[] nivellsDePanta;
        public double[] capsesDePanta;
        public BarreraGuardada[] barreres;

        public int nivellDeTren = 1;
        public double carregaDeTren;

        public double doblersALEstacio;

        public int nivellDeTenda = 1;
        public double carregaDelTransportista;

        public EncarregatGuardat[] encarregats;
        public int[] encarregatsCompratsPerTipus;   // un per TipusDeLloc

        // Cada país té la seva moneda i el seu rellotge: mentre ets en un altre
        // (o amb el joc tancat) aquest continua produint com a offline.
        public double doblers;
        public long marcaDeTempsUtc;
        public RecompensaOffline offlinePendent;
    }

    // Camps públics i no propietats: JsonUtility només serialitza camps.
    //
    // 4: una entrada per zona. Les versions 3 i anteriors tenien una sola
    //    zona en camps plans; MigracioDePartida les converteix en la zona 0.
    [Serializable]
    public class DadesDePartida
    {
        public const int VersioActual = 4;

        public int versio = VersioActual;
        public long marcaDeTempsUtc;

        // Globals: valen per a totes les zones.
        // Ja no s'hi guarden els doblers (són de cada país, a ZonaGuardada):
        // es manté per llegir les partides d'abans, que en carregar-se els
        // passen a la zona 0.
        public double doblers;
        // Les partides d'abans no el tenen i JsonUtility el deixa a 0, que és
        // el que toca: no cal pujar de versió.
        public int gemes;

        // Quants països de pantans hi ha oberts, en ordre (el primer sempre).
        public int paisosDesbloquejats = 1;

        // El que es va guanyar fora i encara no s'ha acceptat al panell.
        public RecompensaOffline offlinePendent;
        public int pasDeLaGuia;

        // La zona on era el jugador l'últim cop, per tornar-hi en obrir.
        public int zonaActual;
        public ZonaGuardada[] zones;

        // La zona n, creant-la (i les anteriors) si encara no existeix.
        public ZonaGuardada Zona(int n)
        {
            if (n < 0) n = 0;
            if (zones == null) zones = new ZonaGuardada[0];
            if (n >= zones.Length)
            {
                var noves = new ZonaGuardada[n + 1];
                Array.Copy(zones, noves, zones.Length);
                zones = noves;
            }
            if (zones[n] == null) zones[n] = new ZonaGuardada();
            return zones[n];
        }

        // S'hi ha jugat mai? Una zona que només existeix perquè n'hi ha una
        // de posterior a l'array no compta.
        //
        // Es mira la llargada i no només si és null: JsonUtility guarda els
        // arrays null com a [], així que una zona de farciment, després de
        // guardar i carregar, tindria un array buit i semblaria jugada. Una
        // zona de debò sempre té un nivell per cada pantà de l'escena.
        public bool TeZona(int n) =>
            zones != null && n >= 0 && n < zones.Length && zones[n] != null &&
            zones[n].nivellsDePanta != null && zones[n].nivellsDePanta.Length > 0;
    }

    // La forma del guardat fins a la versió 3, quan només hi havia una zona.
    // Només es fa servir per llegir partides velles.
    [Serializable]
    public class DadesDePartidaV3
    {
        public int versio;
        public long marcaDeTempsUtc;
        public double doblers;
        public int[] nivellsDePanta;
        public double[] capsesDePanta;
        public BarreraGuardada[] barreres;
        public int nivellDeTren = 1;
        public double carregaDeTren;
        public double doblersALEstacio;
        public int nivellDeTenda = 1;
        public double carregaDelTransportista;
        public EncarregatGuardat[] encarregats;
        public int encarregatsComprats;              // v2: comptador global, ja no es fa servir
        public int[] encarregatsCompratsPerTipus;    // v3
        public int pasDeLaGuia;
    }
}
