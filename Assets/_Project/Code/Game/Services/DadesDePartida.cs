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
    }

    // Camps públics i no propietats: JsonUtility només serialitza camps.
    // Els camps que falten en una partida vella conserven el valor del seu
    // inicialitzador, així que afegir-ne de nous no trenca els guardats.
    [Serializable]
    public class DadesDePartida
    {
        public int versio = 2;
        public long marcaDeTempsUtc;
        public double doblers;

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
        public int encarregatsComprats;   // mou el preu de la següent compra
    }
}
