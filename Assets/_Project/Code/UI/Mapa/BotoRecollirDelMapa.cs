using UnityEngine;

namespace IdleFrogs.UI
{
    // El botó del cartellet de guanys d'un país: recull el que ha guanyat i
    // ho posa a la cartera d'aquell país. Va dins del PaisDelMapa, amb un
    // collider i un SpriteRenderer amb Order in Layer més alt que el país
    // (així el toc és seu i no del país).
    public class BotoRecollirDelMapa : MonoBehaviour, Game.IEsPotTocar
    {
        private PaisDelMapa _pais;

        private void Awake() => _pais = GetComponentInParent<PaisDelMapa>();

        public void EnTocar()
        {
            if (_pais != null && GestorDelMapa.Instancia != null) GestorDelMapa.Instancia.Recollir(_pais);
        }
    }
}
