using System.Collections.Generic;
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

        [Tooltip("Les herbes, flors i arbres d'aquest país, per capes. Es prova i es desa des de " +
                 "l'objecte de decoració de l'escena. Buit: el perfil que tingui aquell objecte")]
        public PerfilDeDecoracio decoracio;

        [Header("Moneda")]
        [Tooltip("El nom del sprite de la moneda dins del Sprite Asset de TextMeshPro")]
        public string spriteDeMoneda = "moneda";
        [Tooltip("La icona que vola cap al comptador i la del HUD. Buit: la de sempre")]
        public Sprite iconaDeMoneda;

        // Cada fila: el sprite del primer país i el que el substitueix aquí.
        // El que no hi sigui es queda com al primer país. Les llistes només
        // serveixen per endreçar: totes funcionen igual i un sprite que surt
        // a diversos llocs (la mosca) es canvia a tot arreu amb una fila.
        [Header("Sprites del país — buit: els del primer país")]
        [Tooltip("El tren, el recipient i el que porti a sobre")]
        public CanviDeSprite[] tren;
        [Tooltip("El restaurant, l'estació, la botiga i els clients")]
        public CanviDeSprite[] restaurantIEstacio;
        [Tooltip("La de l'eixam, la del tren, la de la llengua...")]
        public CanviDeSprite[] mosques;
        [Tooltip("Els retrats del panell i els sprites al món (també la silueta buida)")]
        public CanviDeSprite[] encarregats;
        [Tooltip("Fons, capsa, nenúfars... S'aplica també damunt d'Aspecte Dels Pantans")]
        public CanviDeSprite[] pantans;
        [Tooltip("El cos normal i el de la boca oberta")]
        public CanviDeSprite[] granotes;
        [Tooltip("Els botons de millorar i el cartell de comprar, al món i als panells")]
        public CanviDeSprite[] botons;
        public CanviDeSprite[] barreres;

        public IEnumerable<CanviDeSprite> CanvisDeSprites()
        {
            CanviDeSprite[][] llistes = { tren, restaurantIEstacio, mosques, encarregats, pantans, granotes, botons, barreres };
            foreach (CanviDeSprite[] llista in llistes)
            {
                if (llista == null) continue;
                foreach (CanviDeSprite c in llista) yield return c;
            }
        }
    }
}
