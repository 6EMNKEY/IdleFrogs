using UnityEngine;
using System;

namespace IdleFrogs.Game
{
    public class Panta : MonoBehaviour
    {

        [Header("ConfiguracioPanta")]
        [SerializeField] private PantaConfigSO config;

        [Header("Posicions")]
        [SerializeField] private Transform pilaDeMosques;
        [SerializeField] public Transform capsaMosques;
        [SerializeField] private Transform[] nenufars = new Transform[6];

        [Header("Referencies")]
        [SerializeField] private ControladorGranota[] slotsGranotes = new ControladorGranota[6];

        [Header("Doblers a la capsa")]
        public double doblersALaCapsa;

        void Start()
        {
            InicialitzarPanta();
        }

        private void InicialitzarPanta()
        {
            for (int i = 0; i < slotsGranotes.Length; i++)
            {
                if (slotsGranotes[i] != null && nenufars.Length > i)
                {
                    slotsGranotes[i].Inicialitzar(this, config, nenufars[i].position, pilaDeMosques.position, capsaMosques.position
                    );
                }

            }
        }

        public void AfegirMosquesALaCapsa(double quantitat)
        {
            doblersALaCapsa += quantitat;
            Debug.Log($"S'han posat {quantitat} doblers a la capsa");
        }

        public double RecollirMosquesDeLaCapsa(double quantitat)
        {
            if (doblersALaCapsa <= 0) return 0;
            double doblersRecollits = Math.Min(doblersALaCapsa, quantitat);
            Debug.Log($"S'han mogut {doblersRecollits} doblers al tren");
            doblersALaCapsa = doblersALaCapsa - doblersRecollits;
            return doblersRecollits;

        }

    }
}
