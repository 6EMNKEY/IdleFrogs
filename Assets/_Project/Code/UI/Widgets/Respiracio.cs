using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Animació de repòs sense frames: respira (s'estira i s'encongeix) i, si
    // el perfil ho diu, es balanceja. Els ajustos no són aquí: són a
    // Resources/Respiracio, un perfil per tipus (encarregats, granotes).
    // Cadascun va al seu ritme perquè no es moguin tots alhora.
    //
    // Va al mateix objecte que el SpriteRenderer. Les granotes només respiren
    // quan són al nenúfar (quietes o pescant).
    public class Respiracio : MonoBehaviour, IEscalaDePulsacio
    {
        [SerializeField] private TipusDeRespiracio tipus = TipusDeRespiracio.Encarregat;

        private SpriteRenderer _dibuix;
        private ControladorGranota _granota;
        private Vector3 _escala, _posicio;
        private Quaternion _rotacio;
        private float _fase, _variacio;
        private float _pulsacio = 1f;
        private bool _girat, _desplacat;

        public float FactorDePulsacio { set => _pulsacio = value; }

        private PerfilDeRespiracio Perfil => RespiracioConfigSO.Actual != null ? RespiracioConfigSO.Actual.Perfil(tipus) : null;

        private void Awake()
        {
            _dibuix = GetComponent<SpriteRenderer>();
            _granota = GetComponent<ControladorGranota>();
            _escala = transform.localScale;
            _posicio = transform.localPosition;
            _rotacio = transform.localRotation;
            _fase = Random.value * 100f;
            _variacio = Random.Range(-1f, 1f);
        }

        private void OnDisable()
        {
            transform.localScale = _escala;
            if (_girat) transform.localRotation = _rotacio;
            if (_desplacat) transform.localPosition = _posicio;
            _girat = _desplacat = false;
        }

        // Les granotes, només al nenúfar.
        private bool PotRespirar() =>
            _granota == null || _granota.estatActual == EstatGranota.Idle || _granota.estatActual == EstatGranota.Recolectant;

        private void Update()
        {
            PerfilDeRespiracio p = Perfil;
            bool respira = p != null && p.activa && PotRespirar();

            float s = 0f, angle = 0f;
            if (respira)
            {
                float t = Time.time * (1f + _variacio * p.variacioDelRitme) + (p.comencarALAtzar ? _fase : 0f);
                s = Mathf.Sin(t * p.respiracionsPerSegon * 2f * Mathf.PI) * p.estirament;
                angle = Mathf.Sin(t * p.balanceigsPerSegon * 2f * Mathf.PI) * p.graus;
            }

            float sx = respira && p.conservarElVolum ? 1f - s * 0.5f : 1f;
            var escala = new Vector3(_escala.x * sx * _pulsacio, _escala.y * (1f + s) * _pulsacio, _escala.z);
            transform.localScale = escala;

            // Girar i ancorar només si el perfil ho fa; si no, no es toquen
            // (una granota camina: la seva posició la porta ella).
            bool gira = respira && p.graus > 0f;
            if (gira || _girat)
            {
                transform.localRotation = gira ? _rotacio * Quaternion.Euler(0f, 0f, angle) : _rotacio;
                _girat = gira;
            }
            bool ancora = p != null && p.ancorarAlsPeus && (respira || _pulsacio != 1f);
            if (ancora || _desplacat)
            {
                transform.localPosition = ancora ? _posicio + Peus(_rotacio, _escala) - Peus(transform.localRotation, escala) : _posicio;
                _desplacat = ancora;
            }
        }

        // On són els peus (el mig de la part de baix del dibuix) respecte al
        // pivot, en coordenades del pare, amb aquesta rotació i escala.
        private Vector3 Peus(Quaternion rotacio, Vector3 escala)
        {
            if (_dibuix == null || _dibuix.sprite == null) return Vector3.zero;
            Bounds b = _dibuix.sprite.bounds;
            float x = _dibuix.flipX ? -b.center.x : b.center.x;
            return rotacio * new Vector3(x * escala.x, b.min.y * escala.y, 0f);
        }
    }
}
