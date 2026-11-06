using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // L'arbre de recerca: un de sol per a tots els països. Va a Resources/Recerca.
    [CreateAssetMenu(fileName = "Recerca", menuName = "IdleFrogs/Recerca/Arbre")]
    public class ArbreDeRecercaSO : ScriptableObject
    {
        public List<NodeDeRecerca> nodes = new List<NodeDeRecerca>();

        [Tooltip("El color dels punts de cada país, per zona")]
        public Color[] colorsDelsPunts =
        {
            new Color(0.35f, 0.75f, 0.25f), new Color(0.2f, 0.5f, 0.9f), new Color(0.55f, 0.3f, 0.85f),
            new Color(0.95f, 0.8f, 0.2f), new Color(0.95f, 0.45f, 0.7f), new Color(0.9f, 0.3f, 0.15f),
        };
        [Tooltip("La lupa dels punts")]
        public Sprite iconaDeLaLupa;

        public Color ColorDe(int zona) =>
            colorsDelsPunts != null && zona >= 0 && zona < colorsDelsPunts.Length ? colorsDelsPunts[zona] : Color.white;

        public NodeDeRecerca Node(string id)
        {
            foreach (NodeDeRecerca n in nodes) if (n != null && n.id == id) return n;
            return null;
        }

        private static ArbreDeRecercaSO _carregat;
        private static bool _buscat;

        public static ArbreDeRecercaSO Actual
        {
            get
            {
                if (!_buscat)
                {
                    _buscat = true;
                    _carregat = Resources.Load<ArbreDeRecercaSO>("Recerca");
                }
                return _carregat;
            }
        }
    }
}
