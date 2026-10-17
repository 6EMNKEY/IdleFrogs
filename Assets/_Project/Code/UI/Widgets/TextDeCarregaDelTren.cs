using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Mateix patró que TextDeLaCapsa, però per al tren: "12.4K" i la moneda.
    // Va en un TMP_Text penjant del tren.
    public class TextDeCarregaDelTren : MonoBehaviour
    {
        private ControladorTren _tren;
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _tren = GetComponentInParent<ControladorTren>();
        }

        private void Start()
        {
            if (_tren == null || _text == null)
            {
                Debug.LogError("TextDeCarregaDelTren: ha d'anar en un TMP_Text penjant del tren", this);
                enabled = false;
                return;
            }

            _tren.EnCanviDeCarrega += Refrescar;
            Refrescar(_tren.CarregaActual);
        }

        private void OnDestroy()
        {
            if (_tren == null) return;
            _tren.EnCanviDeCarrega -= Refrescar;
        }

        private void Refrescar(double quantitat)
        {
            _text.text = FormatadorDeNombres.Doblers(quantitat) + Icones.Moneda;
        }
    }
}
