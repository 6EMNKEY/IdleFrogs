using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Mateix patró que TextDeLaCapsa: ensenya el que el tren ha descarregat a
    // l'estació i els clients encara no han pagat. Pot anar penjant de
    // l'estació o a qualsevol lloc (damunt del restaurant) amb el camp omplert.
    public class TextDeLEstacio : MonoBehaviour
    {
        [Tooltip("Buit: es busca pujant per la jerarquia. Omple'l si el text és al restaurant")]
        [SerializeField] private Estacio _estacio;
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            if (_estacio == null) _estacio = GetComponentInParent<Estacio>();
        }

        private void Start()
        {
            if (_estacio == null || _text == null)
            {
                Debug.LogError("TextDeLEstacio: necessita un TMP_Text i l'Estacio (al camp o com a pare)", this);
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
            _text.text = FormatadorDeNombres.Doblers(quantitat) + Icones.Moneda;
    }
}
