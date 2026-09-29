using UnityEngine;

namespace IdleFrogs.Game
{
    // El magatzem on descarrega el tren. No té nivell ni tope: el que la
    // granota transportista no se'n dugui cap a la tenda, s'hi va acumulant.
    // El que limita cada tram és el tren per una banda i la tenda per l'altra,
    // no aquest dipòsit.
    //
    // No es buida tocant-la: d'aquí ho treu la granota. Si es pogués recollir
    // a mà, la tenda no serviria de res.
    public class Estacio : MonoBehaviour
    {
        [SerializeField] private double doblersEmmagatzemats;

        public double DoblersEmmagatzemats => doblersEmmagatzemats;
        public event System.Action<double> EnCanviDeDoblers;

        // Hi arriba el tren
        public void Descarregar(double quantitat)
        {
            if (quantitat <= 0) return;
            doblersEmmagatzemats += quantitat;
            Avisar();
        }

        // Se'n du la granota transportista
        public double Retirar(double maxim)
        {
            if (maxim <= 0 || doblersEmmagatzemats <= 0) return 0;
            double retirat = System.Math.Min(maxim, doblersEmmagatzemats);
            doblersEmmagatzemats -= retirat;
            Avisar();
            return retirat;
        }

        // Per al carregador de partida
        public void AplicarDoblers(double quantitat)
        {
            doblersEmmagatzemats = quantitat < 0 ? 0 : quantitat;
            Avisar();
        }

        private void Avisar() => EnCanviDeDoblers?.Invoke(doblersEmmagatzemats);
    }
}
