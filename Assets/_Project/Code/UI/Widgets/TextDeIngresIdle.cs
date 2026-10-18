using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // "12.4K/s" al HUD: el que entra sol, sense tocar res. És zero fins que
    // els tres trams (pantans, tren i tenda) tenen encarregat.
    // Es refresca cada segon: les habilitats dels encarregats el fan pujar i
    // baixar sense que canviï cap nivell.
    public class TextDeIngresIdle : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void Start()
        {
            if (_text == null)
            {
                Debug.LogError("TextDeIngresIdle: ha d'anar en un TMP_Text", this);
                enabled = false;
                return;
            }

            if (GameManager.Instancia == null)
            {
                Debug.LogError("TextDeIngresIdle: no hi ha cap GameManager a l'escena", this);
                enabled = false;
                return;
            }

            GameManager.Instancia.EnTickDeSegon += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Refrescar;
        }

        private void Refrescar() => _text.text = FormatadorDeNombres.Ritme(GameManager.Instancia.IngresIdlePerSegon());
    }
}
