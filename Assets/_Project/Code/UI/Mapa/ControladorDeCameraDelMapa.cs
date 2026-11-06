using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleFrogs.UI
{
    // La càmera del mapa: s'arrossega en tots dos eixos (amb inèrcia), fa
    // zoom amb dos dits o amb la roda del ratolí (cap al punt on apuntes) i
    // no surt mai del dibuix del mapa.
    //
    // L'arrossegament el decideix el DetectorDeTocs (toc o arrossegament);
    // el zoom el llegeix ella mateixa. Va a la càmera (ortogràfica).
    [RequireComponent(typeof(Camera))]
    public class ControladorDeCameraDelMapa : MonoBehaviour, ICameraArrossegable
    {
        [Header("Límits")]
        [Tooltip("El dibuix de fons del mapa: la càmera no en surt")]
        [SerializeField] private SpriteRenderer fonsDelMapa;
        [Tooltip("Si no hi ha fons: el rectangle del món que es pot veure")]
        [SerializeField] private Rect limits = new Rect(-20, -12, 40, 24);

        [Header("Zoom (mida ortogràfica = mitja alçada visible)")]
        [SerializeField] private float midaMinima = 3f;
        [Tooltip("0 = tant com permet el mapa")]
        [SerializeField] private float midaMaxima = 0f;
        [Tooltip("Com de ràpid fa zoom la roda del ratolí")]
        [SerializeField] private float velocitatDeLaRoda = 0.0015f;

        [Header("Inèrcia")]
        [Tooltip("Com més alt, abans s'atura en deixar anar. 0 = sense inèrcia")]
        [SerializeField] private float frenada = 5f;

        private Camera _camera;
        private Vector2 _velocitat;
        private bool _arrossegant;
        private bool _pessigant;
        private float _distanciaAnterior;
        private Vector2 _mitjaAnterior;

        public bool HaFetZoom { get; private set; }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (!_camera.orthographic)
                Debug.LogWarning("ControladorDeCameraDelMapa: la càmera hauria de ser ortogràfica", this);
        }

        private Rect Limits
        {
            get
            {
                if (fonsDelMapa == null || fonsDelMapa.sprite == null) return limits;
                Bounds b = fonsDelMapa.bounds;
                return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            }
        }

        private float UnitatsPerPixel => Screen.height > 0 ? 2f * _camera.orthographicSize / Screen.height : 0f;

        // ---------------------------------------------------------------
        // Arrossegar (ho mana el DetectorDeTocs)
        // ---------------------------------------------------------------

        public void Aturar()
        {
            _velocitat = Vector2.zero;
            HaFetZoom = false;
        }

        public void ComencarArrossegament()
        {
            _arrossegant = true;
            _velocitat = Vector2.zero;
        }

        public void Arrossegar(Vector2 deltaPantalla)
        {
            // Amb dos dits mana el pessic, que ja mou la càmera.
            if (!_arrossegant || _pessigant) return;
            Vector2 d = -deltaPantalla * UnitatsPerPixel;
            Moure(d);
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _velocitat = Vector2.Lerp(_velocitat, d / dt, 0.4f);
        }

        public void Amollar()
        {
            _arrossegant = false;
            if (frenada <= 0f) _velocitat = Vector2.zero;
        }

        // ---------------------------------------------------------------
        // Zoom
        // ---------------------------------------------------------------

        private void Update()
        {
            Pessic();
            Roda();
        }

        private void Pessic()
        {
            Touchscreen pantalla = Touchscreen.current;
            if (pantalla == null) { _pessigant = false; return; }

            int premuts = 0;
            Vector2 a = default, b = default;
            foreach (var toc in pantalla.touches)
            {
                if (!toc.press.isPressed) continue;
                if (premuts == 0) a = toc.position.ReadValue();
                else if (premuts == 1) b = toc.position.ReadValue();
                premuts++;
            }

            if (premuts < 2)
            {
                _pessigant = false;
                return;
            }

            float distancia = Vector2.Distance(a, b);
            Vector2 mitja = (a + b) / 2f;
            if (!_pessigant)
            {
                _pessigant = true;
                HaFetZoom = true;
                _velocitat = Vector2.zero;
            }
            else if (distancia > 1f && _distanciaAnterior > 1f)
            {
                // Separar els dits = apropar-se (la mida es fa petita).
                ZoomCap(_camera.orthographicSize * _distanciaAnterior / distancia, mitja);
                // I el mapa segueix el punt mig dels dits.
                Moure(-(mitja - _mitjaAnterior) * UnitatsPerPixel);
            }
            _distanciaAnterior = distancia;
            _mitjaAnterior = mitja;
        }

        private void Roda()
        {
            Mouse ratoli = Mouse.current;
            if (ratoli == null) return;
            float roda = ratoli.scroll.ReadValue().y;
            if (Mathf.Abs(roda) < 0.01f) return;
            ZoomCap(_camera.orthographicSize * (1f - roda * velocitatDeLaRoda), ratoli.position.ReadValue());
        }

        // Canvia la mida mantenint quiet el punt del món que hi ha sota 'pantalla'.
        private void ZoomCap(float mida, Vector2 pantalla)
        {
            Vector3 abans = PuntDelMon(pantalla);
            _camera.orthographicSize = Mathf.Clamp(mida, midaMinima, MidaMaxima);
            Vector3 despres = PuntDelMon(pantalla);
            Moure(abans - despres);
        }

        private Vector3 PuntDelMon(Vector2 pantalla) =>
            _camera.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, -transform.position.z));

        // La més gran que encara cap dins del mapa (sense veure'n la vora).
        private float MidaMaxima
        {
            get
            {
                Rect r = Limits;
                float cap = Mathf.Min(r.height / 2f, r.width / 2f / Mathf.Max(0.01f, _camera.aspect));
                return midaMaxima > 0 ? Mathf.Min(midaMaxima, cap) : cap;
            }
        }

        // ---------------------------------------------------------------
        // Moure i limitar
        // ---------------------------------------------------------------

        // Centra la càmera en un punt (el país on ets, en obrir el mapa).
        public void CentrarEn(Vector3 punt)
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(punt.x, punt.y, p.z);
            Limitar();
        }

        private void LateUpdate()
        {
            if (!_arrossegant && !_pessigant && _velocitat.sqrMagnitude > 0.0001f)
            {
                float dt = Time.unscaledDeltaTime;
                Moure(_velocitat * dt);
                _velocitat *= Mathf.Exp(-frenada * dt);
            }
            Limitar();
        }

        private void Moure(Vector2 d)
        {
            transform.position += new Vector3(d.x, d.y, 0f);
            Limitar();
        }

        private void Limitar()
        {
            if (_camera == null) return;
            if (_camera.orthographicSize > MidaMaxima) _camera.orthographicSize = MidaMaxima;

            Rect r = Limits;
            float mitjaAlcada = _camera.orthographicSize;
            float mitjaAmplada = mitjaAlcada * _camera.aspect;
            Vector3 p = transform.position;
            float x = r.width <= 2 * mitjaAmplada ? r.center.x : Mathf.Clamp(p.x, r.xMin + mitjaAmplada, r.xMax - mitjaAmplada);
            float y = r.height <= 2 * mitjaAlcada ? r.center.y : Mathf.Clamp(p.y, r.yMin + mitjaAlcada, r.yMax - mitjaAlcada);
            if (!Mathf.Approximately(x, p.x) || !Mathf.Approximately(y, p.y))
            {
                if (!Mathf.Approximately(x, p.x)) _velocitat.x = 0;
                if (!Mathf.Approximately(y, p.y)) _velocitat.y = 0;
                transform.position = new Vector3(x, y, p.z);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Rect r = Limits;
            Gizmos.color = new Color(0.3f, 0.8f, 1f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
#endif
    }
}
