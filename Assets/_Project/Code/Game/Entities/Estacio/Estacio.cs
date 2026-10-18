using UnityEngine;

namespace IdleFrogs.Game
{
    // El magatzem on descarrega el tren, unit al restaurant per un tub. No té
    // nivell ni tope: el que els clients no paguin s'hi va acumulant. El que
    // limita cada tram és el tren per una banda i el restaurant per l'altra,
    // no aquest dipòsit.
    //
    // No es buida tocant-la: d'aquí ho treuen els clients en pagar. Si es
    // pogués recollir a mà, el restaurant no serviria de res.
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

        // Ho paga un client en acabar de menjar
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
