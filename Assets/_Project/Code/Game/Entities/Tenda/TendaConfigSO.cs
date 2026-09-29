using UnityEngine;

namespace IdleFrogs.Game
{
    [CreateAssetMenu(fileName = "TendaConfig", menuName = "IdleFrogs/TendaConfig")]
    public class TendaConfigSO : ScriptableObject
    {
        // Les granotes treballen a temps fix, no a cabal: és el tren qui
        // transfereix per cabal. Aquí seguim el mateix criteri que als pantans.
        [Header("Temps de la granota (no pugen amb el nivell)")]
        public float tempsDeCarrega = 1.0f;
        public float tempsDeDescarrega = 1.0f;

        [Header("Capacitat per viatge (puja amb el nivell)")]
        public double capacitatBase = 100;
        public double creixementDeCapacitat = 1.3;

        [Header("Velocitat de la granota (puja amb el nivell)")]
        public float velocitatBase = 2.0f;
        public float creixementDeVelocitat = 1.01f;
        public float velocitatMaxima = 20f;

        // IMT: al 20 un transportista més i ×1.3; al 50 un ×1.2.
        // Al nivell 10 IMT hi posa un transportista, però aquí no n'hi ha.
        [Header("Fites — multipliquen càrrega i velocitat alhora")]
        public FitaDeMultiplicador[] fites =
        {
            new FitaDeMultiplicador { nivell = 20, multiplicador = 1.3 },
            new FitaDeMultiplicador { nivell = 50, multiplicador = 1.2 },
        };

        [Header("Transportistes")]
        public int transportistesInicials = 1;
        public int[] nivellsDeTransportistaExtra = { 20 };

        [Header("Cost de millora")]
        public double costBase = 500;
        public double creixementDeCost = 1.2;

        // Amb creixement 1.3 la càrrega es passa de rang cap al 2680.
        [Header("Límit")]
        public int nivellMaxim = 2000;

#if UNITY_EDITOR
        private void OnValidate()
        {
            int segur = Fites.NivellMaximSegur(
                capacitatBase, creixementDeCapacitat, Fites.Acumulat(fites, nivellMaxim));

            if (nivellMaxim > segur)
                Debug.LogWarning($"{name}: nivellMaxim {nivellMaxim} passa de rang. " +
                                 $"Amb aquests creixements el màxim segur és {segur}; " +
                                 "per sobre les estadístiques donen NaN.", this);
        }
#endif
    }
}
