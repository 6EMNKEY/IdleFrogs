using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IdleFrogs.UI
{
    // Zoom amb dos dits o amb la roda del ratolí sobre el contingut d'un
    // ScrollRect (l'arbre de recerca). L'arrossegament ja el fa el ScrollRect.
    [RequireComponent(typeof(ScrollRect))]
    public class ZoomDeScrollRect : MonoBehaviour
    {
        [SerializeField] private float escalaMinima = 0.4f;
        [SerializeField] private float escalaMaxima = 1.5f;
        [SerializeField] private float velocitatDeLaRoda = 0.001f;

        private ScrollRect _scroll;
        private float _distanciaAnterior;

        private void Awake() => _scroll = GetComponent<ScrollRect>();

        private void Update()
        {
            RectTransform c = _scroll != null ? _scroll.content : null;
            if (c == null) return;

            float factor = 1f;
            Touchscreen pantalla = Touchscreen.current;
            int premuts = 0;
            Vector2 a = default, b = default;
            if (pantalla != null)
                foreach (var toc in pantalla.touches)
                {
                    if (!toc.press.isPressed) continue;
                    if (premuts == 0) a = toc.position.ReadValue();
                    else if (premuts == 1) b = toc.position.ReadValue();
                    premuts++;
                }

            if (premuts >= 2)
            {
                float d = Vector2.Distance(a, b);
                if (_distanciaAnterior > 1f && d > 1f) factor = d / _distanciaAnterior;
                _distanciaAnterior = d;
                // Mentre es pessiga, el ScrollRect no s'ha d'endur el contingut.
                _scroll.velocity = Vector2.zero;
            }
            else
            {
                _distanciaAnterior = 0f;
                Mouse ratoli = Mouse.current;
                float roda = ratoli != null ? ratoli.scroll.ReadValue().y : 0f;
                if (Mathf.Abs(roda) > 0.01f) factor = 1f + roda * velocitatDeLaRoda;
            }

            if (Mathf.Approximately(factor, 1f)) return;
            float escala = Mathf.Clamp(c.localScale.x * factor, escalaMinima, escalaMaxima);
            c.localScale = new Vector3(escala, escala, 1f);
        }
    }
}
