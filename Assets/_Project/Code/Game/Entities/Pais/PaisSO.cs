using UnityEngine;

namespace IdleFrogs.Game
{
    // Un país del mapa. Els de pantans fan servir tots la mateixa escena: el
    // que els distingeix és aquest asset (aspecte, moneda i preu). La ciutat
    // comercial no té pantans: obre la botiga.
    [CreateAssetMenu(fileName = "Pais", menuName = "IdleFrogs/Pais")]
    public class PaisSO : ScriptableObject
    {
        [Header("Identitat")]
        [Tooltip("El nom en els tres idiomes")]
        public TextTraduit nom;
        [Tooltip("Si falta la traducció de l'idioma i de l'anglès")]
        public string nomVisible = "País";

        public string NomVisible
        {
            get
            {
                string t = nom.Text;
                return string.IsNullOrEmpty(t) ? nomVisible : t;
            }
        }
        [Tooltip("No té pantans: tocar-lo obre la botiga. Sempre oberta i fora de la cadena de desbloqueig")]
        public bool esCiutatComercial;

        [Header("Desbloqueig")]
        [Tooltip("Es paga amb la moneda del país on ets en comprar-lo. El primer país no en té")]
        [Min(0)] public double preuDeDesbloqueig = 1e6;

        [Header("Aspecte")]
        [Tooltip("El color de fons de la càmera (el verd de l'herba al primer país)")]
        public Color colorDeFons = new Color(0.549f, 0.773f, 0.165f);
        [Tooltip("Buit: els pantans fan servir el seu propi aspecte")]
        public AspectePantaSO aspecteDelsPantans;

        [Header("Moneda")]
        [Tooltip("El nom del sprite de la moneda dins del Sprite Asset de TextMeshPro")]
        public string spriteDeMoneda = "moneda";
        [Tooltip("La icona que vola cap al comptador i la del HUD. Buit: la de sempre")]
        public Sprite iconaDeMoneda;
    }
}
