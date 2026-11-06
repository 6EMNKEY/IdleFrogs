using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La mosca daurada: de tant en tant creua la pantalla. Si la toques,
    // pregunta si vols veure un anunci per 1 o 2 hores de producció (a
    // l'atzar); si no la toques, se'n va i torna més tard.
    //
    // Vola en coordenades de la pantalla (no del món): encara que moguis la
    // càmera, no se'n surt. No surt fins que s'acaba la guia.
    //
    // Va en un objecte de l'escena del país amb un SpriteRenderer (el dibuix
    // de la mosca) i un Collider2D, amb un Sorting Order per sobre de tot.
    [RequireComponent(typeof(SpriteRenderer))]
    public class MoscaDaurada : MonoBehaviour, IEsPotTocar
    {
        [Header("Quan surt (segons)")]
        [SerializeField] private float primeraAparicio = 90f;
        [SerializeField] private float minimEntreAparicions = 240f;
        [SerializeField] private float maximEntreAparicions = 480f;

        [Header("Vol")]
        [Tooltip("Segons que tarda a creuar la pantalla")]
        [SerializeField] private float segonsDeVol = 12f;
        [Tooltip("Entre quines alçades de la pantalla vola (0 = a baix, 1 = a dalt)")]
        [SerializeField] private Vector2 alcada = new Vector2(0.35f, 0.8f);
        [Tooltip("Com puja i baixa mentre vola (fracció de l'alçada de la pantalla)")]
        [SerializeField] private float onada = 0.06f;
        [SerializeField] private float onadesPerVol = 3f;
        [Tooltip("El dibuix mira a la dreta: es gira quan vola cap a l'esquerra")]
        [SerializeField] private bool miraALaDreta = true;

        [Header("Recompensa: hores de producció, a l'atzar entre aquestes")]
        [Min(1)] [SerializeField] private int horesMinimes = 1;
        [Min(1)] [SerializeField] private int horesMaximes = 2;

        private SpriteRenderer _dibuix;
        private Collider2D _collider;
        private float _espera;
        private float _temps;
        private bool _volant;
        private bool _capALaDreta;
        private float _alcada;

        private void Awake()
        {
            _dibuix = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _espera = primeraAparicio;
            Amagar();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (!_volant)
            {
                if (!PotSortir()) return;
                _espera -= dt;
                if (_espera <= 0f) Sortir();
                return;
            }

            _temps += dt;
            float t = _temps / Mathf.Max(0.1f, segonsDeVol);
            if (t >= 1f) { Marxar(); return; }
            Posar(t);
        }

        private static bool PotSortir()
        {
            GameManager gm = GameManager.Instancia;
            if (gm == null || !gm.HiHaPais || !gm.GuiaAcabada) return false;
            // Amb una pregunta oberta no: no es pot tocar.
            return FinestraDeConfirmacio.Instancia == null || !FinestraDeConfirmacio.Instancia.EstaOberta;
        }

        private void Sortir()
        {
            _volant = true;
            _temps = 0f;
            _capALaDreta = Random.value < 0.5f;
            _alcada = Random.Range(alcada.x, alcada.y);
            if (_dibuix != null)
            {
                _dibuix.enabled = true;
                _dibuix.flipX = _capALaDreta != miraALaDreta;
            }
            if (_collider != null) _collider.enabled = true;
            Posar(0f);
        }

        // t de 0 a 1: d'una vora de la pantalla a l'altra, una mica per fora.
        private void Posar(float t)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float x = Mathf.Lerp(-0.08f, 1.08f, _capALaDreta ? t : 1f - t);
            float y = _alcada + Mathf.Sin(t * onadesPerVol * 2f * Mathf.PI) * onada;
            Vector3 p = cam.ViewportToWorldPoint(new Vector3(x, y, Mathf.Abs(cam.transform.position.z - transform.position.z)));
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        private void Marxar()
        {
            Amagar();
            _espera = Random.Range(minimEntreAparicions, Mathf.Max(minimEntreAparicions, maximEntreAparicions));
        }

        private void Amagar()
        {
            _volant = false;
            if (_dibuix != null) _dibuix.enabled = false;
            if (_collider != null) _collider.enabled = false;
        }

        public void EnTocar()
        {
            if (!_volant) return;
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            Vector3 on = transform.position;
            Marxar();
            if (g == null) return;

            int hores = Random.Range(horesMinimes, Mathf.Max(horesMinimes, horesMaximes) + 1);
            double doblers = g.DoblersDeTemps(hores * 3600.0);
            if (!(doblers > 0)) return;

            GestorDAudio.Sonar(So.Toc);
            string quant = FormatadorDeNombres.Doblers(doblers) + Icones.Moneda;
            FinestraDeConfirmacio.Preguntar(
                Textos.T("mosca.titol"),
                Textos.F("mosca.text", quant, hores),
                () => g.VeureAnunci(vist => Cobrar(vist, doblers, on)),
                textAcceptar: Textos.T(g.SenseAnuncis ? "botiga.recollir" : "botiga.anunci"));
        }

        private static void Cobrar(bool vist, double doblers, Vector3 on)
        {
            if (!vist || CurrencyManager.Instancia == null) { GestorDAudio.Sonar(So.Error); return; }
            CurrencyManager.Instancia.AfegirDoblers(doblers);
            GestorDAudio.Sonar(So.Recompensa);
            if (EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.LlancarDesDelMon(TipusDeMoneda.Doblers, on, doblers);
        }
    }
}
