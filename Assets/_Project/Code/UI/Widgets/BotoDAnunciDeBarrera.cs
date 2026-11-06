using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // "Anunci −30 min" d'una barrera: només surt mentre compta i queden
    // anuncis (Anuncis Maxims de la Barrera). Tocar-lo mostra l'anunci i, si
    // s'ha vist, treu el temps.
    //
    // Va en un fill de la Barrera, amb un dibuix i un collider; no dins del
    // BotoDeBarrera (aquest s'amaga mentre compta).
    public class BotoDAnunciDeBarrera : MonoBehaviour, IEsPotTocar
    {
        [Tooltip("El que s'amaga i es mostra. Buit: el dibuix i els textos d'aquest objecte")]
        [SerializeField] private GameObject arrel;
        [Tooltip("\"−30m\" i els que queden. Buit: el primer text dels fills")]
        [SerializeField] private TMP_Text text;

        private Barrera _barrera;
        private Collider2D _collider;
        private bool _esperant;

        private void Awake()
        {
            _barrera = GetComponentInParent<Barrera>();
            _collider = GetComponent<Collider2D>();
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);
        }

        private void Start()
        {
            if (_barrera == null)
            {
                Debug.LogError("BotoDAnunciDeBarrera: ha de penjar d'una Barrera", this);
                enabled = false;
                return;
            }
            _barrera.EnCanviDeTemps += Refrescar;
            if (GameManager.Instancia != null) GameManager.Instancia.EnTickDeSegon += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (_barrera != null) _barrera.EnCanviDeTemps -= Refrescar;
            if (GameManager.Instancia != null) GameManager.Instancia.EnTickDeSegon -= Refrescar;
        }

        public void EnTocar()
        {
            if (!enabled || _esperant || _barrera == null || !_barrera.EsPotReduirAmbAnunci) return;
            if (GestorDeBotiga.Instancia == null) return;
            _esperant = true;
            GestorDeBotiga.Instancia.VeureAnunci(vist =>
            {
                _esperant = false;
                bool fet = vist && _barrera != null && _barrera.ReduirAmbAnunci();
                GestorDAudio.Sonar(fet ? So.Recompensa : So.Error);
                Refrescar();
            });
        }

        private void Refrescar()
        {
            if (_barrera == null) return;
            bool visible = _barrera.EsPotReduirAmbAnunci;
            Mostrar(visible);
            if (visible && text != null)
                text.text = Textos.F("anunci.barrera", FormatadorDeNombres.TempsCurt(_barrera.SegonsPerAnunci), _barrera.AnuncisRestants);
        }

        private void Mostrar(bool visible)
        {
            if (arrel != null && arrel != gameObject)
            {
                if (arrel.activeSelf != visible) arrel.SetActive(visible);
            }
            else
            {
                // Aquest objecte ha de seguir encès: s'apaguen el dibuix i els textos.
                foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            }
            if (_collider != null) _collider.enabled = visible;
        }
    }
}
