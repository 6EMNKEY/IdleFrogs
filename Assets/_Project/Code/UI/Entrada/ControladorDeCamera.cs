using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Desplaça la càmera arrossegant, només en horitzontal (la via va cap a
    // la dreta), amb inèrcia en deixar anar i sense sortir del món.
    //
    // No llegeix l'entrada: li diu què fer el DetectorDeTocs, que és qui
    // decideix si un gest és un toc o un arrossegament.
    //
    // Va a la mateixa càmera (ha de ser ortogràfica).
    [RequireComponent(typeof(Camera))]
    public class ControladorDeCamera : MonoBehaviour
    {
        [Header("Límits del món")]
        [Tooltip("La vora esquerra de la càmera no passarà d'aquí. Buit = on comença la càmera.")]
        [SerializeField] private Transform limitEsquerre;
        [Tooltip("La vora dreta no passarà d'aquí. Buit = sense límit fix (només el de sota).")]
        [SerializeField] private Transform limitDret;

        // Només pots anar fins on hi ha alguna cosa a fer. Sense
        // això el jugador nou arrossega cap a 30 pantans buits i es perd.
        [Header("No passar del que ja s'ha desbloquejat")]
        [SerializeField] private LlistaDePantans llistaDePantans;
        [Tooltip("Unitats que es deixen veure més enllà del darrer pantà o barrera visible")]
        [SerializeField] private float margeDespresDelDarrerVisible = 3f;

        [Header("Inèrcia")]
        [Tooltip("Com més alt, abans s'atura en deixar anar. 0 = sense inèrcia.")]
        [SerializeField] private float frenada = 5f;

        private Camera _camera;
        private float _xInicial;
        private float _velocitat;      // unitats/segon, només X
        private bool _arrossegant;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _xInicial = transform.position.x;

            if (!_camera.orthographic)
                Debug.LogWarning("ControladorDeCamera: la càmera hauria de ser ortogràfica", this);
        }

        // Unitats de món per píxel de pantalla. Amb càmera ortogràfica és
        // constant: l'alçada visible és 2 × orthographicSize.
        private float UnitatsPerPixel =>
            Screen.height > 0 ? 2f * _camera.orthographicSize / Screen.height : 0f;

        public void Aturar() => _velocitat = 0f;

        public void ComencarArrossegament()
        {
            _arrossegant = true;
            _velocitat = 0f;
        }

        // deltaPantalla en píxels. El dit va cap a la dreta → el món també,
        // o sigui que la càmera va cap a l'esquerra: d'aquí el signe menys.
        public void Arrossegar(Vector2 deltaPantalla)
        {
            if (!_arrossegant) return;

            float dx = -deltaPantalla.x * UnitatsPerPixel;
            MoureX(dx);

            // Velocitat suavitzada, per a la inèrcia en deixar anar.
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _velocitat = Mathf.Lerp(_velocitat, dx / dt, 0.4f);
        }

        public void Amollar()
        {
            _arrossegant = false;
            if (frenada <= 0f) _velocitat = 0f;
        }

        private void LateUpdate()
        {
            if (!_arrossegant && Mathf.Abs(_velocitat) > 0.01f)
            {
                float dt = Time.unscaledDeltaTime;
                MoureX(_velocitat * dt);
                _velocitat *= Mathf.Exp(-frenada * dt);
            }

            // Encara que no es mogui: comprar un pantà o obrir una barrera
            // amplia el límit, i treure un pantà de la vista l'estreny.
            Limitar();
        }

        private void MoureX(float dx)
        {
            Vector3 p = transform.position;
            p.x += dx;
            transform.position = p;
            Limitar();
        }

        private void Limitar()
        {
            float mitjaAmplada = _camera.orthographicSize * _camera.aspect;

            float esquerre = limitEsquerre != null ? limitEsquerre.position.x : _xInicial - mitjaAmplada;
            float dret = LimitDretEfectiu();

            float minX = esquerre + mitjaAmplada;
            float maxX = dret - mitjaAmplada;

            Vector3 p = transform.position;
            // Si el món visible és més estret que la pantalla, la càmera es
            // queda arrambada a l'esquerra (on hi ha l'estació).
            float x = maxX < minX ? minX : Mathf.Clamp(p.x, minX, maxX);

            if (!Mathf.Approximately(x, p.x))
            {
                p.x = x;
                transform.position = p;
                _velocitat = 0f;   // xocar amb la vora mata la inèrcia
            }
        }

        private float LimitDretEfectiu()
        {
            float dret = limitDret != null ? limitDret.position.x : float.PositiveInfinity;
            if (llistaDePantans == null) return float.IsInfinity(dret) ? _xInicial + 1000f : dret;

            float darrer = float.NegativeInfinity;

            for (int i = 0; i < llistaDePantans.Quantitat; i++)
            {
                Panta p = llistaDePantans.Obtenir(i);
                if (p != null && p.Estat != EstatDePanta.Bloquejat)
                    darrer = Mathf.Max(darrer, p.transform.position.x);
            }

            // Una barrera es veu quan ja l'has atrapada (tens comprat el que hi
            // ha davant) o quan ja és oberta.
            for (int n = 0; n < llistaDePantans.QuantitatDeBarreres; n++)
            {
                Barrera b = llistaDePantans.ObtenirBarrera(n);
                if (b != null && (b.EstaOberta || llistaDePantans.EsPotIniciarBarrera(n)))
                    darrer = Mathf.Max(darrer, b.transform.position.x);
            }

            if (float.IsNegativeInfinity(darrer)) return float.IsInfinity(dret) ? _xInicial + 1000f : dret;
            return Mathf.Min(dret, darrer + margeDespresDelDarrerVisible);
        }
    }
}
