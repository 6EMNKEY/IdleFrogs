using TMPro;
using UnityEngine;

namespace IdleFrogs.UI
{
    // El text del temps de l'habilitat es veu més fosc mentre recarrega.
    // Recorda el color que li vas posar a l'editor la primera vegada que el
    // pinta, i l'enfosqueix a partir d'aquest: així no s'ha de triar un
    // segon color per a cada text.
    public class ColorDelTemps
    {
        private TMP_Text _text;
        private Color _base;

        public void Pintar(TMP_Text text, bool recarregant, float claror)
        {
            if (text == null) return;
            if (text != _text)
            {
                _text = text;
                _base = text.color;
            }

            float f = recarregant ? Mathf.Clamp01(claror) : 1f;
            text.color = new Color(_base.r * f, _base.g * f, _base.b * f, _base.a);
        }
    }
}
