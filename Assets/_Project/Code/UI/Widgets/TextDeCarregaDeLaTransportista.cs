using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El que porta la granota de la tenda en aquell moment, de l'estació cap a
    // la tenda. Va en un TMP_Text penjant de la granota transportista, així
    // que viatja amb ella. Quan va buida, no ensenya res.
    public class TextDeCarregaDeLaTransportista : MonoBehaviour
    {
        private GranotaTransportista _granota;
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _granota = GetComponentInParent<GranotaTransportista>();
        }

        private void Start()
        {
            if (_granota == null || _text == null)
            {
                Debug.LogError("TextDeCarregaDeLaTransportista: ha d'anar en un TMP_Text penjant de la granota transportista", this);
                enabled = false;
                return;
            }

            _granota.EnCanviDeCarrega += Refrescar;
            Refrescar(_granota.Carrega);
        }

        private void OnDestroy()
        {
            if (_granota != null) _granota.EnCanviDeCarrega -= Refrescar;
        }

        private void Refrescar(double quantitat) =>
            _text.text = quantitat > 0 ? FormatadorDeNombres.Doblers(quantitat) + Icones.Moneda : string.Empty;
    }
}
