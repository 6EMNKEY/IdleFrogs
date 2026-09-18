using UnityEngine;
using System;

namespace IdleFrogs.Game
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instancia { get; private set; }

        public double doblersActuals { get; private set; }
        public event Action<double> EnCanviDeDoblers;

        //Asegurar que sigui Singleton 
        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;

        }

        public void AfegirDoblers(double quantitat)
        {
            if (quantitat <= 0) return;
            doblersActuals += quantitat;
            EnCanviDeDoblers?.Invoke(doblersActuals);
        }

        public bool GastarDoblers(double quantitat)
        {
            if (doblersActuals < quantitat) return false;
            doblersActuals -= quantitat;
            EnCanviDeDoblers?.Invoke(doblersActuals);
            return true;
        }
    }
}
