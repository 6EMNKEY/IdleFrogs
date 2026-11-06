using UnityEngine;

namespace IdleFrogs.Game
{
    // Un personatge de l'arena. Les estadístiques són les de la seva raresa
    // (ArenaConfigSO); el personatge només hi posa el nom i el dibuix. Els
    // que tens són exemplars d'aquest (en pots tenir de repetits).
    [CreateAssetMenu(fileName = "Lluitador", menuName = "IdleFrogs/Arena/Lluitador")]
    public class LluitadorSO : ScriptableObject
    {
        [Tooltip("Estable: el guardat hi apunta. No el canviïs un cop publicat")]
        public string id;
        public TextTraduit nom;
        public Sprite sprite;
        public RaresaDeLluita raresa;
        [Tooltip("Només surt a l'arena d'aquest país. Buit: a totes")]
        public PaisSO pais;

        public string NomVisible
        {
            get
            {
                string t = nom.Text;
                return string.IsNullOrEmpty(t) ? name : t;
            }
        }
    }
}
