using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El comptador de doblers del HUD. Va en un TMP_Text del Canvas.
    // CurrencyManager és un singleton, així que tampoc té res per arrossegar.
    public class TextDeDoblers : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        // A Start i no a Awake: CurrencyManager.Instancia s'assigna al seu
        // Awake i l'ordre entre objectes de l'escena no està garantit.
        private void Start()
        {
            if (_text == null)
            {
                Debug.LogError("TextDeDoblers: ha d'anar en un TMP_Text", this);
                enabled = false;
                return;
            }

            if (CurrencyManager.Instancia == null)
            {
                Debug.LogError("TextDeDoblers: no hi ha cap CurrencyManager a l'escena", this);
                enabled = false;
                return;
            }

            CurrencyManager.Instancia.EnCanviDeDoblers += Refrescar;
            // Mentre volen monedes cap aquí, el número puja a mesura que arriben.
            if (EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.EnCanviDelQueEsMostra += RefrescarPerEfecte;
            Refrescar(CurrencyManager.Instancia.doblersActuals);
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= Refrescar;
            if (EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.EnCanviDelQueEsMostra -= RefrescarPerEfecte;
        }

        private void RefrescarPerEfecte(TipusDeMoneda tipus)
        {
            if (tipus == TipusDeMoneda.Doblers && CurrencyManager.Instancia != null)
                Refrescar(CurrencyManager.Instancia.doblersActuals);
        }

        // El saldo menys el que encara és de camí en forma de monedes.
        private void Refrescar(double quantitat)
        {
            double retingut = EfecteDeRecompensa.Instancia != null ? EfecteDeRecompensa.Instancia.Retingut(TipusDeMoneda.Doblers) : 0;
            _text.text = FormatadorDeNombres.Doblers(System.Math.Max(0, quantitat - retingut));
        }
    }
}
