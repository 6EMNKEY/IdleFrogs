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

        // Corba de potència i no exponencial: puja de pressa als primers
        // nivells, quan s'obren pantans, i cada cop més a poc a poc, fins a
        // tocar la màxima al nivell que diguis. La suavitat frena només el
        // principi. Amb 3 → 40 al 800 i suavitat 2,2: 5,2 al nivell 8,
        // 15 al 100, 29 al 400.
        [Header("Velocitat (puja amb el nivell, cada cop més a poc a poc)")]
        [FormerlySerializedAs("velocitatDeMoviment")]
        public float velocitatBase = 3.0f;
        public float velocitatMaxima = 40f;
        [Tooltip("El nivell on arriba a la velocitat màxima")]
        [Min(2)] public int nivellDeVelocitatMaxima = 800;
        [Tooltip("Com més alta, més a poc a poc puja als primers nivells (0 = sense frenar el principi). " +
                 "No canvia ni la base ni on s'arriba a la màxima")]
        [Min(0)] public float suavitatInicial = 2.2f;

        [Header("Capacitat del tren (puja amb el nivell)")]
        [FormerlySerializedAs("capacitat")]
        public double capacitatBase = 50;
        public double creixementDeCapacitat = 1.33;

        // ×2.66 al nivell 10 i ×1.21 al 40, a capacitat i velocitat de càrrega.
        [Header("Fites — multipliquen capacitat i ritme alhora")]
        public FitaDeMultiplicador[] fites =
        {
            new FitaDeMultiplicador { nivell = 10, multiplicador = 2.66 },
            new FitaDeMultiplicador { nivell = 40, multiplicador = 1.21 },
        };

        [Header("Cost de millora")]
        public double costBase = 200;
        public double creixementDeCost = 1.2;

        // Molt per sobre del tope 800 dels pantans. Amb creixement 1.33 la
        // capacitat es passa de rang cap al 2470, així que 2000 és el màxim
        // rodó que hi cap amb marge.
        [Header("Límit")]
        public int nivellMaxim = 2000;

        // velocitatBase × ((n + s) / (1 + s))^p: al nivell 1 dona la base, i p
        // és tal que al nivell de la màxima hi arribi just.
        public float VelocitatAlNivell(int n)
        {
            if (n < 1) n = 1;
            if (velocitatBase <= 0 || velocitatMaxima <= velocitatBase || nivellDeVelocitatMaxima < 2)
                return Mathf.Max(0f, velocitatBase);
            float s = Mathf.Max(0f, suavitatInicial);
            float p = Mathf.Log(velocitatMaxima / velocitatBase) / Mathf.Log((nivellDeVelocitatMaxima + s) / (1f + s));
            return Mathf.Min(velocitatMaxima, velocitatBase * Mathf.Pow((n + s) / (1f + s), p));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            int segur = Mathf.Min(
                Fites.NivellMaximSegur(capacitatBase, creixementDeCapacitat, Fites.Acumulat(fites, nivellMaxim)),
                Fites.NivellMaximSegur(ritmeDeCarregaBase, creixementDeRitme, Fites.Acumulat(fites, nivellMaxim)));
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
