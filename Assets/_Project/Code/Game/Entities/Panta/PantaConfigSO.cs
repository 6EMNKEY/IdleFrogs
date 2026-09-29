using UnityEngine;
using UnityEngine.Serialization;

namespace IdleFrogs.Game
{
    [CreateAssetMenu(fileName = "PantaConfig", menuName = "IdleFrogs/PantaConfig")]
    public class PantaConfigSO : ScriptableObject
    {
        // A IMT la Walking Speed del primer pou és 2 i no puja mai amb el
        // nivell. Allà el temps que no és minar surt constant a 2,06s, i és
        // camí d'anada + buidar + camí de tornada.
        //
        // Al prefab Panta_Base els sis nenúfars queden de mitjana a 1,751
        // unitats de la capsa, o sigui 1,751s d'anada i tornada a velocitat 2.
        // Deixem el fix total en ~1,95s en comptes de 2,06 — una mica més
        // ràpid que IMT a propòsit, per no calcar-los els números:
        //     1,751 (camí) + 0,20 (buidar) = 1,951s
        [Header("Moviment (no puja amb el nivell)")]
        public float velocitatDeMoviment = 2.0f;
        public float tempsBuidar = 0.2f;

        // El temps de captura no és una corba pròpia: la granota recull fins a
        // omplir-se, o sigui temps = capacitat / velocitat. Quan els dos stats
        // pugen pel mateix factor el temps no es mou i la producció puja just
        // aquell factor — que és el que fa que el ×50 de profunditat doni ×50
        // de producció i no ×2500.
        // Valors del primer pou d'IMT: MiningSpeed 5 i Worker Capacity 20,
        // totes dues ×1.1 per nivell. Amb aquests números la fórmula reprodueix
        // la seva taula de nivells 1-10 amb menys d'un 1,6% d'error.
        [Header("Recol·lecció — doblers per segon (puja amb el nivell)")]
        public double velocitatDeRecoleccioBase = 5;
        public double creixementDeVelocitatDeRecoleccio = 1.1;

        [Header("Capacitat — doblers per ple (puja amb el nivell)")]
        [FormerlySerializedAs("doblersPerPle")]
        public double capacitatBase = 20;
        public double creixementDeCapacitat = 1.1;

        // IMT: ×2.1 a la velocitat i la capacitat al nivell 25, i un altre al 50.
        [Header("Fites — multipliquen velocitat i capacitat alhora")]
        public FitaDeMultiplicador[] fites =
        {
            new FitaDeMultiplicador { nivell = 25, multiplicador = 2.1 },
            new FitaDeMultiplicador { nivell = 50, multiplicador = 2.1 },
        };

        [Header("Cost de millora")]
        public double costBase = 100;
        public double creixementDeCost = 1.2;

        // IMT: 10 → 1.000 (×100) → 30.000 (×30) → ×20 constant a partir d'aquí.
        // No és una corba: els tres primers són a mà i la resta té factor de cua.
        [Header("Compra del pantà")]
        public double[] preusDeCompra = { 10, 1000, 30000 };
        public double factorDePreuDeCompra = 20;

        // Cada pantà produeix més que l'anterior. El primer salt és el gros.
        [Header("Profunditat — multiplica velocitat i capacitat")]
        public double multiplicadorDelPrimerSalt = 50;
        [FormerlySerializedAs("multiplicadorPerProfunditat")]
        public double multiplicadorDeCadaSalt = 10;

        // IMT: +1 miner al 10 i un altre al 50, sobre una base d'1.
        [Header("Granotes")]
        public int granotesInicials = 1;
        public int[] nivellsDeGranotaExtra = { 10, 50 };

        [Header("Límit")]
        public int nivellMaxim = 800;

        public double PreuDeCompraAlIndex(int index)
        {
            if (preusDeCompra == null || preusDeCompra.Length == 0) return 0;
            if (index < 0) index = 0;
            if (index < preusDeCompra.Length) return preusDeCompra[index];

            int ultim = preusDeCompra.Length - 1;
            return preusDeCompra[ultim] * System.Math.Pow(factorDePreuDeCompra, index - ultim);
        }

        public double MultiplicadorDeProfunditat(int index) =>
            index <= 0
                ? 1
                : multiplicadorDelPrimerSalt * System.Math.Pow(multiplicadorDeCadaSalt, index - 1);
    }
}
