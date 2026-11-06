using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // L'arena: una per país, s'obre en comprar el pantà 15. Tres pantans
    // amb un boss cadascun, que canvien cada 24 h; hi poses lluitadors del
    // país i maten (o no) segons el dany i la regeneració. Va a Resources/Arena.
    [CreateAssetMenu(fileName = "Arena", menuName = "IdleFrogs/Arena/Config")]
    public class ArenaConfigSO : ScriptableObject
    {
        [Tooltip("Una per raresa, de D a SSS")]
        public DadesDeRaresa[] rareses = new DadesDeRaresa[0];

        [Header("Desbloqueig")]
        [Tooltip("Comprar aquest pantà del país (1 = el primer) obre la seva arena")]
        [Min(1)] public int pantaQueDesbloqueja = 15;
        [Tooltip("Tickets comuns de regal en obrir la primera arena de cada país")]
        [Min(0)] public int ticketsDeRegal = 10;

        [Header("Cicle")]
        [Min(0.01f)] public double horesDelCicle = 24;
        [Min(0)] public int refrescosGratisAlDia = 1;
        [Min(0)] public int gemesPerRefresc = 50;

        [Header("Catàleg")]
        public List<LluitadorSO> lluitadors = new List<LluitadorSO>();
        public List<BossSO> bosses = new List<BossSO>();

        public TaulaDeLArena Taula => new TaulaDeLArena(rareses, horesDelCicle);

        public LluitadorSO Lluitador(string id)
        {
            foreach (LluitadorSO l in lluitadors) if (l != null && l.id == id) return l;
            return null;
        }

        public BossSO Boss(string id)
        {
            foreach (BossSO b in bosses) if (b != null && b.id == id) return b;
            return null;
        }

        // Els d'una raresa que poden sortir en un país (els seus i els de tots).
        public void LluitadorsDe(RaresaDeLluita r, PaisSO pais, List<LluitadorSO> sortida)
        {
            sortida.Clear();
            foreach (LluitadorSO l in lluitadors)
                if (l != null && l.raresa == r && (l.pais == null || l.pais == pais)) sortida.Add(l);
        }

        public void BossesDe(RaresaDeLluita r, PaisSO pais, List<BossSO> sortida)
        {
            sortida.Clear();
            foreach (BossSO b in bosses)
                if (b != null && b.raresa == r && (b.pais == null || b.pais == pais)) sortida.Add(b);
        }

        private static ArenaConfigSO _carregat;
        private static bool _buscat;

        public static ArenaConfigSO Actual
        {
            get
            {
                if (!_buscat)
                {
                    _buscat = true;
                    _carregat = Resources.Load<ArenaConfigSO>("Arena");
                }
                return _carregat;
            }
        }

        // Sense Domain Reload, l'estàtic sobreviu entre Plays: es torna a buscar a cada Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() { _buscat = false; _carregat = null; }
    }
}
