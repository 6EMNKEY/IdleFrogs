using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Tot el que depèn de la raresa a l'arena: el boss, el lluitador, la
    // millora i les probabilitats.
    [Serializable]
    public class DadesDeRaresa
    {
        public RaresaDeLluita raresa;
        [Tooltip("El color de la insígnia")]
        public Color color = Color.white;

        [Header("Boss")]
        [Min(1)] public double vidaDelBoss = 10000;
        [Tooltip("Vida que recupera per segon. Si el lluitador fa menys dany, no el pot matar mai")]
        [Min(0)] public double regeneracio = 0.2;
        [Tooltip("Punts de recerca (del color del país) en matar-lo")]
        [Min(0)] public int punts = 1;
        [Tooltip("Pes perquè surti un boss d'aquesta raresa")]
        [Min(0)] public float probabilitatDelBoss = 35;

        [Header("Lluitador")]
        [Tooltip("Dany per segon a nivell 1")]
        [Min(0)] public double danyBase = 1;
        [Tooltip("Cada nivell multiplica el dany per això")]
        [Min(1)] public double creixementDelDany = 1.06;
        [Min(1)] public int nivellMaxim = 100;

        // L'economia d'un país va de 1e21/s (pantà 15, quan s'obre l'arena) a
        // 1e33/s (al final): fins al nivell de canvi el cost puja molt de pressa
        // per seguir-la; després, a poc a poc, perquè el que el fa pujar és
        // el multiplicador del prestigi (x120 a 5 estrelles).
        [Header("Millorar el lluitador (moneda del seu país)")]
        [Tooltip("El que costa passar del nivell 1 al 2")]
        [Min(0)] public double costBase = 1000;
        [Tooltip("Cada nivell costa això vegades l'anterior, fins al Nivell De Canvi")]
        [Min(1)] public double creixementDelCost = 3;
        [Min(1)] public int nivellDeCanvi = 30;
        [Tooltip("I a partir d'aquí, això")]
        [Min(1)] public double creixementDespres = 1.1;

        [Header("Gacha (pesos)")]
        [Min(0)] public float probabilitatComu;
        [Min(0)] public float probabilitatPremium;

        public double Dany(int nivell) => danyBase * Math.Pow(creixementDelDany, Math.Max(0, nivell - 1));

        // El que costa passar de 'nivell' a 'nivell + 1'.
        public double CostDeMillora(int nivell)
        {
            int n = Math.Max(1, nivell);
            int primers = Math.Min(n, nivellDeCanvi) - 1;
            int despres = Math.Max(0, n - nivellDeCanvi);
            return costBase * Math.Pow(creixementDelCost, primers) * Math.Pow(creixementDespres, despres);
        }
    }
}
