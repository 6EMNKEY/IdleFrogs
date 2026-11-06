using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    [Serializable]
    public class EstrellaDePrestigi
    {
        [Tooltip("Multiplica el que guanya el país, des que tens aquesta estrella (no s'acumula amb les anteriors)")]
        [Min(1)] public double multiplicadorDIngressos = 3;
        [Tooltip("Multiplica el preu dels pantans, les millores i les barreres del país (la dificultat)")]
        [Min(1)] public double multiplicadorDeCost = 2.5;
        [Tooltip("Gemes que dona en comprar-la")]
        [Min(0)] public int gemes = 50;
        [Tooltip("El preu: tantes vegades el que costa l'últim pantà del país (amb la dificultat que tingui)")]
        [Min(0)] public double preuEnDarrersPantans = 2;
        [Tooltip("Opcional: el dibuix que la granota porta a sobre amb aquesta estrella. " +
                 "Un país el pot canviar a la seva llista de Granotes")]
        public Sprite dibuixDeLaGranota;
    }

    // El prestigi de cada país: fins a cinc estrelles. Cada una reinicia el
    // país (pantans, tren, restaurant, barreres i encarregats), et deixa els
    // doblers que et sobren, multiplica per sempre el que guanya i fa més
    // cars els preus. Va a Resources/Prestigi.
    [CreateAssetMenu(fileName = "Prestigi", menuName = "IdleFrogs/Prestigi")]
    public class PrestigiConfigSO : ScriptableObject
    {
        public EstrellaDePrestigi[] estrelles =
        {
            new EstrellaDePrestigi { multiplicadorDIngressos = 3, multiplicadorDeCost = 2.5, gemes = 50 },
            new EstrellaDePrestigi { multiplicadorDIngressos = 8, multiplicadorDeCost = 6, gemes = 100 },
            new EstrellaDePrestigi { multiplicadorDIngressos = 20, multiplicadorDeCost = 15, gemes = 150 },
            new EstrellaDePrestigi { multiplicadorDIngressos = 50, multiplicadorDeCost = 35, gemes = 200 },
            new EstrellaDePrestigi { multiplicadorDIngressos = 120, multiplicadorDeCost = 80, gemes = 300 },
        };

        [Header("El pantà i la granota del panell")]
        [Tooltip("L'aspecte dels pantans dels països que no en tenen (el del prefab del pantà). " +
                 "El panell fa com el joc: l'aspecte del país o aquest, amb els canvis de sprites del país")]
        public AspectePantaSO aspectePerDefecte;

        // L'aspecte dels pantans d'un país, com el tria el pantà al joc.
        public AspectePantaSO AspecteDe(PaisSO pais) =>
            pais != null && pais.aspecteDelsPantans != null ? pais.aspecteDelsPantans : aspectePerDefecte;

        public int Maxim => estrelles != null ? estrelles.Length : 0;

        // null amb 0 estrelles (o més de les que hi ha).
        public EstrellaDePrestigi Estrella(int n) =>
            estrelles != null && n >= 1 && n <= estrelles.Length ? estrelles[n - 1] : null;

        public double FactorDIngressos(int estrelles) => Estrella(Mathf.Min(estrelles, Maxim))?.multiplicadorDIngressos ?? 1;
        public double FactorDeCost(int estrelles) => Estrella(Mathf.Min(estrelles, Maxim))?.multiplicadorDeCost ?? 1;

        // ---------------------------------------------------------------

        private static PrestigiConfigSO _carregat;
        private static bool _buscat;

        // El de Resources/Prestigi. Sense: res no canvia (x1 i sense prestigi).
        // Es busca un cop: els preus el pregunten a cada repintada.
        public static PrestigiConfigSO Actual
        {
            get
            {
                if (!_buscat)
                {
                    _buscat = true;
                    _carregat = Resources.Load<PrestigiConfigSO>("Prestigi");
                }
                return _carregat;
            }
        }

        // Sense Domain Reload, l'estàtic sobreviu entre Plays: es torna a buscar a cada Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() { _buscat = false; _carregat = null; }
    }
}
