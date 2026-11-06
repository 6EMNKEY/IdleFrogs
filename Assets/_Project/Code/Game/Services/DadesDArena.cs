using System;
using System.Collections.Generic;

namespace IdleFrogs.Game
{
    // L'arena i la recerca es guarden fora de les zones: el prestigi
    // reemplaça la zona sencera i no ha de tocar ni lluitadors ni punts.

    [Serializable]
    public class LluitadorGuardat
    {
        public int id;               // l'exemplar (en pots tenir de repetits)
        public string idDefinicio;   // LluitadorSO.id
        public int raresa;           // RaresaDeLluita: les estadístiques no depenen del SO
        public int nivell = 1;
    }

    [Serializable]
    public class BossALArena
    {
        public string idBoss;        // BossSO.id (buit: no n'hi ha)
        public int raresa;
        public double vida;
        public double vidaMaxima;
        public int lluitador;        // l'exemplar del nenúfar; 0 = cap
        public bool mort;
    }

    [Serializable]
    public class ArenaGuardada
    {
        public bool desbloquejada;
        public bool regalDonat;
        public List<LluitadorGuardat> lluitadors = new List<LluitadorGuardat>();
        public List<BossALArena> bosses = new List<BossALArena>();
        public long iniciDelCicleUtc;
        public long actualitzadaUtc;
        public int diaDelsRefrescos;
        public int refrescosGratisFets;
    }

    [Serializable]
    public class DadesDArena
    {
        public List<ArenaGuardada> arenes = new List<ArenaGuardada>();
        public int ticketsComuns;
        public int ticketsPremium;
        public int seguentId = 1;

        public ArenaGuardada Arena(int zona)
        {
            if (zona < 0) zona = 0;
            while (arenes.Count <= zona) arenes.Add(new ArenaGuardada());
            if (arenes[zona] == null) arenes[zona] = new ArenaGuardada();
            return arenes[zona];
        }
    }

    [Serializable]
    public class NivellDeNode
    {
        public string id;
        public int nivell;
    }

    [Serializable]
    public class DadesDeRecerca
    {
        public List<NivellDeNode> nivells = new List<NivellDeNode>();
        // Els punts de cada color: un per país (zona).
        public List<double> punts = new List<double>();

        public double Punts(int zona) => zona >= 0 && zona < punts.Count ? punts[zona] : 0;

        public void AfegirPunts(int zona, double quantitat)
        {
            if (zona < 0) return;
            while (punts.Count <= zona) punts.Add(0);
            punts[zona] = Math.Max(0, punts[zona] + quantitat);
        }

        public int Nivell(string id)
        {
            foreach (NivellDeNode n in nivells) if (n != null && n.id == id) return n.nivell;
            return 0;
        }

        public void PosarNivell(string id, int nivell)
        {
            foreach (NivellDeNode n in nivells)
                if (n != null && n.id == id) { n.nivell = nivell; return; }
            nivells.Add(new NivellDeNode { id = id, nivell = nivell });
        }
    }
}
