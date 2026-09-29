using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Mateix patró que TextDeLaCapsa: va en un TMP_Text penjant de l'estació
    // i ensenya el que el tren hi ha descarregat i encara no s'ha recollit.
    public class TextDeLEstacio : MonoBehaviour
    {
        private Estacio _estacio;
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _estacio = GetComponentInParent<Estacio>();
        }

        private void Start()
        {
            if (_estacio == null || _text == null)
            {
                Debug.LogError("TextDeLEstacio: ha d'anar en un TMP_Text penjant de l'Estacio", this);
                enabled = false;
                return;
            }

            _estacio.EnCanviDeDoblers += Refrescar;
            Refrescar(_estacio.DoblersEmmagatzemats);
        }

        private void OnDestroy()
        {
            if (_estacio != null) _estacio.EnCanviDeDoblers -= Refrescar;
        }

        // Sense denominador: el magatzem no té tope.
        private void Refrescar(double quantitat) =>
            _text.text = FormatadorDeNombres.Doblers(quantitat);
    }
}
