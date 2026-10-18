using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El comptador de gemes del HUD, germà de TextDeDoblers. Va en un
    // TMP_Text del Canvas i no té res per arrossegar.
    public class TextDeGemes : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        // A Start i no a Awake: CurrencyManager.Instancia s'assigna al seu
        // Awake i l'ordre entre objectes de l'escena no està garantit.
        private void Start()
        {
            if (_text == null)
            {
                Debug.LogError("TextDeGemes: ha d'anar en un TMP_Text", this);
                enabled = false;
                return;
            }

            if (CurrencyManager.Instancia == null)
            {
                Debug.LogError("TextDeGemes: no hi ha cap CurrencyManager a l'escena", this);
                enabled = false;
                return;
            }

            CurrencyManager.Instancia.EnCanviDeGemes += Refrescar;
            // Mentre volen monedes cap aquí, el número puja a mesura que arriben.
            if (EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.EnCanviDelQueEsMostra += RefrescarPerEfecte;
            Refrescar(CurrencyManager.Instancia.gemesActuals);
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeGemes -= Refrescar;
            if (EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.EnCanviDelQueEsMostra -= RefrescarPerEfecte;
        }

        private void RefrescarPerEfecte(TipusDeMoneda tipus)
        {
            if (tipus == TipusDeMoneda.Gemes && CurrencyManager.Instancia != null)
                Refrescar(CurrencyManager.Instancia.gemesActuals);
        }

        // Mateix format que els doblers: 1.2K quan en tinguis moltes.
        // El saldo menys el que encara és de camí en forma de monedes.
        private void Refrescar(int quantitat)
        {
            double retingut = EfecteDeRecompensa.Instancia != null ? EfecteDeRecompensa.Instancia.Retingut(TipusDeMoneda.Gemes) : 0;
            _text.text = FormatadorDeNombres.Doblers(System.Math.Max(0, quantitat - retingut));
        }
    }
}
