using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La icona de la moneda del país on ets (la Icona De Moneda del seu
    // PaisSO). Si el país no en té, es queda la que hi hagi posada.
    [RequireComponent(typeof(Image))]
    public class IconaDeLaMonedaDelPais : MonoBehaviour
    {
        private Image _imatge;
        private Sprite _perDefecte;

        private void Awake()
        {
            _imatge = GetComponent<Image>();
            _perDefecte = _imatge.sprite;
        }

        private void Start()
        {
            if (GestorDePaisos.Instancia != null) GestorDePaisos.Instancia.EnCanviDePaisos += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GestorDePaisos.Instancia != null) GestorDePaisos.Instancia.EnCanviDePaisos -= Refrescar;
        }

        private void Refrescar()
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            Sprite s = pais != null && pais.iconaDeMoneda != null ? pais.iconaDeMoneda : _perDefecte;
            _imatge.sprite = s;
            // El quadrat de mostra es veu fins que hi ha una icona.
            if (s != null) _imatge.color = Color.white;
        }
    }
}
