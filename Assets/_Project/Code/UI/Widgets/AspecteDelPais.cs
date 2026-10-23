using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Aplica el país on ets: el color de fons de la càmera, la moneda dels
    // textos, la icona que vola cap al comptador i els sprites que el país
    // canvia (tren, restaurant, herbes, mosques, granotes, botons...).
    // Corre abans que la resta de l'escena faci el seu Awake.
    [DefaultExecutionOrder(-150)]
    public class AspecteDelPais : MonoBehaviour
    {
        [Tooltip("Buit: Camera.main")]
        [SerializeField] private Camera cameraDelMon;
        [Tooltip("Opcional: la icona de la moneda del HUD")]
        [SerializeField] private Image iconaDeMonedaDelHUD;

        private void Awake()
        {
            SpritesDelPais.Refrescar();

            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            if (pais == null) return;

            Camera cam = cameraDelMon != null ? cameraDelMon : Camera.main;
            if (cam != null) cam.backgroundColor = pais.colorDeFons;

            if (!string.IsNullOrEmpty(pais.spriteDeMoneda)) Icones.SpriteDeLaMoneda = pais.spriteDeMoneda;
            if (iconaDeMonedaDelHUD != null && pais.iconaDeMoneda != null) iconaDeMonedaDelHUD.sprite = pais.iconaDeMoneda;

            CanviarSpritesDeLEscena();
        }

        // A Start: EfecteDeRecompensa s'assigna l'Instancia al seu Awake.
        private void Start()
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            if (pais != null && EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.EstablirIconaDeDoblers(pais.iconaDeMoneda);
        }

        // Tot el que hi ha a l'escena, també el que està apagat (pantans sense
        // comprar, panells tancats). Com que passa abans dels Awake dels altres,
        // el que es copia d'un model (les mosques de l'eixam) ja surt canviat.
        private static void CanviarSpritesDeLEscena()
        {
            if (SpritesDelPais.QuantsCanvis == 0) return;

            foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (sr.sprite != null) sr.sprite = SpritesDelPais.Substituir(sr.sprite);

            foreach (Image img in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (img.sprite != null) img.sprite = SpritesDelPais.Substituir(img.sprite);
        }

        // Per al que es crea durant la partida a partir d'un prefab (les fitxes
        // dels panells): el mateix canvi, només a dins d'aquest objecte.
        public static void Aplicar(GameObject arrel)
        {
            if (arrel == null || SpritesDelPais.QuantsCanvis == 0) return;

            foreach (SpriteRenderer sr in arrel.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sprite != null) sr.sprite = SpritesDelPais.Substituir(sr.sprite);

            foreach (Image img in arrel.GetComponentsInChildren<Image>(true))
                if (img.sprite != null) img.sprite = SpritesDelPais.Substituir(img.sprite);
        }
    }
}
