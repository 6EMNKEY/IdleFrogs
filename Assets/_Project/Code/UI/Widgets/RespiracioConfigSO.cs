using System;
using UnityEngine;

namespace IdleFrogs.UI
{
    public enum TipusDeRespiracio { Encarregat, Granota }

    // Com respira un tipus de cosa.
    [Serializable]
    public class PerfilDeRespiracio
    {
        [Tooltip("Desmarcat: tots els d'aquest tipus quiets (el toc encara els fa créixer)")]
        public bool activa = true;

        [Header("Respirar (estirar i encongir)")]
        [Tooltip("Quant s'estira: 0,04 = un 4 % de l'alçada")]
        [Range(0f, 0.3f)] public float estirament = 0.04f;
        [Tooltip("Respiracions per segon")]
        [Range(0.05f, 3f)] public float respiracionsPerSegon = 0.5f;
        [Tooltip("Quan s'estira s'aprima (i a l'inrevés), com si tingués volum")]
        public bool conservarElVolum = true;

        [Header("Balanceig (esquerra i dreta)")]
        [Tooltip("Graus cap a cada costat. 0 = sense balanceig")]
        [Range(0f, 20f)] public float graus = 3f;
        [Tooltip("Balanceigs per segon")]
        [Range(0.05f, 3f)] public float balanceigsPerSegon = 0.25f;

        [Header("Varietat")]
        [Tooltip("Cadascun va una mica més ràpid o més lent: 0,15 = ±15 %")]
        [Range(0f, 0.5f)] public float variacioDelRitme = 0.15f;
        [Tooltip("Cadascun comença en un punt diferent de la respiració")]
        public bool comencarALAtzar = true;

        [Header("On s'aguanta")]
        [Tooltip("S'estira i es balanceja des dels peus. Només per a coses que no es mouen " +
                 "(els encarregats): a les granotes, que caminen, deixa-ho desmarcat")]
        public bool ancorarAlsPeus = true;
    }

    // Tots els ajustos de la respiració en un sol lloc (Resources/Respiracio):
    // un perfil per als encarregats i un per a les granotes. Es poden canviar
    // en Play i es veu a l'instant.
    [CreateAssetMenu(fileName = "Respiracio", menuName = "IdleFrogs/Respiració")]
    public class RespiracioConfigSO : ScriptableObject
    {
        public PerfilDeRespiracio encarregats = new PerfilDeRespiracio();

        [Tooltip("Respiren quan són al nenúfar (quietes o pescant), no quan caminen ni buiden")]
        public PerfilDeRespiracio granotes = new PerfilDeRespiracio
        {
            estirament = 0.03f, respiracionsPerSegon = 0.6f, graus = 0f, ancorarAlsPeus = false,
        };

        public PerfilDeRespiracio Perfil(TipusDeRespiracio t) => t == TipusDeRespiracio.Granota ? granotes : encarregats;

        private static RespiracioConfigSO _carregat;
        private static bool _buscat;

        public static RespiracioConfigSO Actual
        {
            get
            {
                if (!_buscat)
                {
                    _buscat = true;
                    _carregat = Resources.Load<RespiracioConfigSO>("Respiracio");
                }
                return _carregat;
            }
        }

        // Sense Domain Reload, l'estàtic sobreviu entre Plays: es torna a buscar a cada Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() { _buscat = false; _carregat = null; }
    }
}
