using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El punt vermell del botó de la botiga del HUD: hi ha alguna cosa gratis
    // per recollir (regal, calendari, pase). Va en un objecte sempre encès;
    // el que s'encén i s'apaga és el punt.
    public class PuntDAvisDeBotiga : MonoBehaviour
    {
        [SerializeField] private GameObject punt;

        private void Start()
        {
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi -= Refrescar;
        }

        private void Refrescar()
        {
            if (punt == null) return;
            bool si = GestorDeBotiga.Instancia != null && GestorDeBotiga.Instancia.HiHaGratisPerRecollir;
            if (punt.activeSelf != si) punt.SetActive(si);
        }
    }
}
