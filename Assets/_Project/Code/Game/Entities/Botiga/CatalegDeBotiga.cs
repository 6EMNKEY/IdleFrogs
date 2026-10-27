using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Tots els productes, en l'ordre en què surten dins de cada secció, i el
    // calendari de recompenses diàries.
    [CreateAssetMenu(fileName = "CatalegDeBotiga", menuName = "IdleFrogs/CatalegDeBotiga")]
    public class CatalegDeBotiga : ScriptableObject
    {
        public List<ProducteDeBotiga> productes = new List<ProducteDeBotiga>();

        [Header("Calendari")]
        [Tooltip("Els primers dies d'un jugador nou (un mes). Un dia per cada entrada al joc")]
        public List<DiaDelCalendari> mesInicial = new List<DiaDelCalendari>();
        [Tooltip("Després del mes inicial, aquest cicle es repeteix")]
        public List<DiaDelCalendari> cicle = new List<DiaDelCalendari>();

        public ProducteDeBotiga Producte(string id)
        {
            foreach (ProducteDeBotiga p in productes)
                if (p != null && p.id == id) return p;
            return null;
        }
    }
}
