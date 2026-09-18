using UnityEngine;

namespace IdleFrogs.Game
{
    public class Panta : MonoBehaviour
    {

        [Header("ConfiguracioPanta")]
        [SerializeField] private PantaConfigSO config;

        [Header("Posicions")]
        [SerializeField] private Transform pilaDeMosques;
        [SerializeField] private Transform capsaMosques;
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

        public void RecollirMosquesDeLaCapsa()
        {
            if (doblersALaCapsa <= 0) return;
            if (CurrencyManager.Instancia != null)
            {
                CurrencyManager.Instancia.AfegirDoblers(doblersALaCapsa);
                Debug.Log($"S'han mogut {doblersALaCapsa} doblers al compte");
                doblersALaCapsa = 0;
            }
        }

    }
}
