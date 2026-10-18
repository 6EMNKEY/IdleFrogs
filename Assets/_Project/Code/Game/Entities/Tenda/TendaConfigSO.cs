using UnityEngine;
using UnityEngine.Serialization;

namespace IdleFrogs.Game
{
    // La configuració del restaurant (el tercer tram). El nom de la classe és
    // el de l'antiga tenda perquè els assets, el guardat i els encarregats de
    // tenda continuïn valent.
    [CreateAssetMenu(fileName = "TendaConfig", menuName = "IdleFrogs/TendaConfig")]
    public class TendaConfigSO : ScriptableObject
    {
        // Com el transportista: el temps no puja amb el nivell. Un client
        // carrega una part del que portava el transportista i menja a la
        // mateixa part del ritme, així que tarda el mateix.
        [Header("Temps de menjar (no puja amb el nivell)")]
        [FormerlySerializedAs("tempsDeCarrega")]
        public float tempsDeMenjar = 4f;

        [Header("Doblers per client (puja amb el nivell)")]
        public double capacitatBase = 250;
        public double creixementDeCapacitat = 1.3;

        [Header("Velocitat dels clients")]
        public float velocitatBase = 1f;
        public float creixementDeVelocitat = 1f;
        public float velocitatMaxima = 20f;

        [Header("Fites — multipliquen doblers per client i velocitat alhora")]
        public FitaDeMultiplicador[] fites =
        {
            new FitaDeMultiplicador { nivell = 20, multiplicador = 1.3 },
            new FitaDeMultiplicador { nivell = 50, multiplicador = 1.2 },
        };

        // On abans hi havia un transportista ara n'hi ha uns quants de clients.
        [Header("Clients")]
        [FormerlySerializedAs("transportistesInicials")]
        public int clientsInicials = 4;
        [Tooltip("Quants se n'hi afegeixen a cada nivell de la llista")]
        public int clientsPerFita = 4;
        [FormerlySerializedAs("nivellsDeTransportistaExtra")]
        public int[] nivellsDeClientsExtra = { 10, 25, 50, 100, 200, 400 };

        [Header("Cost de millora")]
        public double costBase = 1250;
        public double creixementDeCost = 1.42;

        [Header("Límit")]
        public int nivellMaxim = 1500;

#if UNITY_EDITOR
        private void OnValidate()
        {
            int segur = Fites.NivellMaximSegur(
                capacitatBase, creixementDeCapacitat, Fites.Acumulat(fites, nivellMaxim));
            // El cost també: amb un creixement alt es passa de rang abans que la capacitat.
            segur = Mathf.Min(segur, Fites.NivellMaximSegur(costBase, creixementDeCost));

            if (nivellMaxim > segur)
                Debug.LogWarning($"{name}: nivellMaxim {nivellMaxim} passa de rang. " +
                                 $"Amb aquests creixements el màxim segur és {segur}; " +
                                 "per sobre les estadístiques donen NaN.", this);
        }
#endif
    }
}
