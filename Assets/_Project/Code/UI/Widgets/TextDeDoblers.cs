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
            Refrescar(CurrencyManager.Instancia.doblersActuals);
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= Refrescar;
        }

        private void Refrescar(double quantitat) => _text.text = FormatadorDeNombres.Doblers(quantitat);
    }
}
