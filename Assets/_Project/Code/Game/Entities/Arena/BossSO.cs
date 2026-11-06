using UnityEngine;

namespace IdleFrogs.Game
{
    // Un boss de l'arena. La vida, la regeneració i els punts són els de la
    // seva raresa (ArenaConfigSO); aquí només el nom i el dibuix.
    [CreateAssetMenu(fileName = "Boss", menuName = "IdleFrogs/Arena/Boss")]
    public class BossSO : ScriptableObject
    {
        [Tooltip("Estable: el guardat hi apunta")]
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
