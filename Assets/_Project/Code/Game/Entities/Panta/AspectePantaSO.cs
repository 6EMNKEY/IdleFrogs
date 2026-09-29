using UnityEngine;

namespace IdleFrogs.Game
{
    // L'aspecte d'un pantà és una dada, no un prefab.
    //
    // Si cada versió gràfica fos una variant, els dos eixos es multiplicarien:
    // 2 disposicions (amunt/avall) × N aspectes = 2N variants per mantenir.
    // Així són 2 variants + N assets, i es combinen lliurement: el pantà
    // "de nit" de dalt i el de baix comparteixen aquest mateix asset.
    [CreateAssetMenu(fileName = "AspectePanta", menuName = "IdleFrogs/AspectePanta")]
    public class AspectePantaSO : ScriptableObject
    {
        [Header("Sprites")]
        public Sprite fons;
        public Sprite capsa;
        public Sprite nenufar;
        public Sprite granota;

        [Header("Color")]
        public Color tintDelFons = Color.white;
    }
}
