using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Aplica el país on ets a tot el que no és dels pantans: el color de fons
    // de la càmera, la moneda dels textos i la icona que vola cap al
    // comptador. Corre abans que els textos facin el primer refresc.
    [DefaultExecutionOrder(-150)]
    public class AspecteDelPais : MonoBehaviour
    {
        [Tooltip("Buit: Camera.main")]
        [SerializeField] private Camera cameraDelMon;
        [Tooltip("Opcional: la icona de la moneda del HUD")]
        [SerializeField] private UnityEngine.UI.Image iconaDeMonedaDelHUD;

        private void Awake()
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            if (pais == null) return;

            Camera cam = cameraDelMon != null ? cameraDelMon : Camera.main;
            if (cam != null) cam.backgroundColor = pais.colorDeFons;

            if (!string.IsNullOrEmpty(pais.spriteDeMoneda)) Icones.SpriteDeLaMoneda = pais.spriteDeMoneda;
            if (iconaDeMonedaDelHUD != null && pais.iconaDeMoneda != null) iconaDeMonedaDelHUD.sprite = pais.iconaDeMoneda;
        }

        // A Start: EfecteDeRecompensa s'assigna l'Instancia al seu Awake.
        private void Start()
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            if (pais != null && EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.EstablirIconaDeDoblers(pais.iconaDeMoneda);
        }
    }
}
