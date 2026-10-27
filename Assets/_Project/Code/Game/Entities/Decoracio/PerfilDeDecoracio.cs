using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Una capa de decoració: un tipus de coses escampades (gespa, arbres...)
    // amb les seves herbes, la seva llavor i el seu ordre de dibuix. Cada capa
    // és independent: els arbres poden anar per sobre de l'aigua i la gespa
    // per sota, i cadascuna evita el que li toca.
    [System.Serializable]
    public class CapaDeDecoracio
    {
        public string nom = "Gespa";
        public VariantDeDecoracio[] variants;

        [Header("Llavor")]
        public int llavor;
        [Tooltip("Les que t'han agradat. A l'inspector de la decoració, 'Fer servir' la torna a generar")]
        public List<LlavorDesada> llavorsDesades = new List<LlavorDesada>();

        [Header("Quantitat")]
        [Tooltip("Elements per unitat quadrada de zona")]
        [Min(0)] public float densitat = 0.1f;
        [Tooltip("Distància mínima entre dos elements, perquè no s'amunteguin. 0 = sense límit")]
        [Min(0)] public float distanciaMinima = 0.2f;

        [Header("Què evita (dels objectes A Evitar de la decoració)")]
        [Tooltip("Buit: tots els dibuixos. Si no, només els que es diguin així (p. ex. Fons: d'un pantà només l'aigua)")]
        public string[] nomsAEvitar = { "Fons" };
        [Tooltip("Espai de més al voltant del que s'evita, en unitats del món")]
        [Min(0)] public float marge;

        [Header("Ordre de dibuix — el fons dels pantans és -50")]
        public string capaDeDibuix = "Default";
        [Tooltip("-100: per sota de tot (gespa). Per sobre de -50: per sobre de l'aigua (arbres, canyes)")]
        public int ordreDeDibuix = -100;
    }

    // La decoració d'un país: les seves capes. El PaisSO l'assenyala i el joc
    // la genera en carregar el país. Si té un camp "fornejat" (generat i
    // retocat a mà, desat com a prefab), el joc posa aquell tal qual.
    [CreateAssetMenu(fileName = "Decoracio", menuName = "IdleFrogs/PerfilDeDecoracio")]
    public class PerfilDeDecoracio : ScriptableObject
    {
        public List<CapaDeDecoracio> capes = new List<CapaDeDecoracio>();

        [Tooltip("Opcional: el camp ja generat i retocat. Si hi és, el joc el posa tal qual i no genera res")]
        public GameObject campFornejat;
    }
}
