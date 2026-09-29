using UnityEngine;
using UnityEngine.Serialization;

namespace IdleFrogs.Game
{
    [CreateAssetMenu(fileName = "TrenConfig", menuName = "IdleFrogs/TrenConfig")]
    public class TrenConfigSO : ScriptableObject
    {
        // Carregar no és un temps fix sinó un cabal: omplir el doble triga
        // el doble. És el que fa que pujar la capacitat sense pujar el ritme
        // no serveixi de res, i al revés.
        // Capacitat i ritme pugen pel mateix factor: així el temps de càrrega
        // no es mou i el que puja és el cabal, com al pantà.
        [Header("Ritme de transferència — doblers/segon (puja amb el nivell)")]
        public double ritmeDeCarregaBase = 20;
        public double ritmeDeDescarregaBase = 40;
        public double creixementDeRitme = 1.33;

        [Header("Velocitat (puja amb el nivell)")]
        [FormerlySerializedAs("velocitatDeMoviment")]
        public float velocitatBase = 3.0f;
        public float creixementDeVelocitat = 1.01f;
        public float velocitatMaxima = 30f;

        [Header("Capacitat del tren (puja amb el nivell)")]
        [FormerlySerializedAs("capacitat")]
        public double capacitatBase = 50;
        public double creixementDeCapacitat = 1.33;

        // IMT: ×2.66 al nivell 10 i ×1.21 al 40, a capacitat i velocitat de càrrega.
        [Header("Fites — multipliquen capacitat i ritme alhora")]
        public FitaDeMultiplicador[] fites =
        {
            new FitaDeMultiplicador { nivell = 10, multiplicador = 2.66 },
            new FitaDeMultiplicador { nivell = 40, multiplicador = 1.21 },
        };

        [Header("Cost de millora")]
        public double costBase = 200;
        public double creixementDeCost = 1.2;

        // Molt per sobre del tope 800 dels pantans: el teardown parla de
        // millores d'ascensor "massa cares" pel nivell 1400. Però amb
        // creixement 1.33 la capacitat es passa de rang cap al 2470, així
        // que 2000 és el màxim rodó que hi cap amb marge.
        [Header("Límit")]
        public int nivellMaxim = 2000;

#if UNITY_EDITOR
        private void OnValidate()
        {
            int segur = Mathf.Min(
                Fites.NivellMaximSegur(capacitatBase, creixementDeCapacitat, Fites.Acumulat(fites, nivellMaxim)),
                Fites.NivellMaximSegur(ritmeDeCarregaBase, creixementDeRitme, Fites.Acumulat(fites, nivellMaxim)));

            if (nivellMaxim > segur)
                Debug.LogWarning($"{name}: nivellMaxim {nivellMaxim} passa de rang. " +
                                 $"Amb aquests creixements el màxim segur és {segur}; " +
                                 "per sobre les estadístiques donen NaN.", this);
        }
#endif
    }
}
