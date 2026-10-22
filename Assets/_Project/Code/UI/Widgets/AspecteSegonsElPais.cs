using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Canvia el dibuix d'aquest objecte segons el país: el tren, l'estació, el
    // restaurant, una decoració... Va en un objecte amb SpriteRenderer o amb
    // Image. Un sprite per país de pantans, en l'ordre del mapa (sense la
    // ciutat comercial); si en falta un, es queda el que tingui.
    public class AspecteSegonsElPais : MonoBehaviour
    {
        [Tooltip("Un per país de pantans, en ordre: el 0 és el primer")]
        [SerializeField] private Sprite[] perPais;
        [Tooltip("Opcional: el color del dibuix per país (buit o curt: blanc)")]
        [SerializeField] private Color[] colorPerPais;

        private void Awake()
        {
            int zona = GameManager.Instancia != null ? GameManager.Instancia.Zona : 0;
            Sprite s = perPais != null && zona < perPais.Length ? perPais[zona] : null;
            bool teColor = colorPerPais != null && zona < colorPerPais.Length;

            if (TryGetComponent(out SpriteRenderer sr))
            {
                if (s != null) sr.sprite = s;
                if (teColor) sr.color = colorPerPais[zona];
            }
            else if (TryGetComponent(out Image img))
            {
                if (s != null) img.sprite = s;
                if (teColor) img.color = colorPerPais[zona];
            }
        }
    }
}
