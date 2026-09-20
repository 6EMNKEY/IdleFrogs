using UnityEngine;

namespace IdleFrogs.Game
{
    public class LlistaDePantans : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public const int PantansPerNivell = 5;

        [Header("Tots els pantans en ordre (0 = el més a prop de la descàrrega)")]
        [SerializeField] private Panta[] pantans;

        [Header("Una barrera per nivell, en el mateix ordre")]
        [SerializeField] private Barrera[] barreres;

        public Panta Obtenir(int index) => pantans[index];

        // La barrera del nivell N guarda el pantà d'índex (N+1)*5 - 1.
        // Si està tancada, el tren no passa del d'abans.
        public int IndexMaximAccessible()
        {
            for (int n = 0; n < barreres.Length; n++)
            {
                if (barreres[n] != null && !barreres[n].EstaOberta)
                    return (n + 1) * PantansPerNivell - 2;
            }
            return pantans.Length - 1;
        }

        public int IndexMesLlunyaAmbCarrega()
        {
            for (int i = IndexMaximAccessible(); i >= 0; i--)
            {
                if (pantans[i] != null && pantans[i].doblersALaCapsa > 0) return i;
            }
            return -1;
        }
    }
}