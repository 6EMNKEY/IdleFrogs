using UnityEngine;

namespace IdleFrogs.Game
{
    // Mosques de decoració que volen a l'atzar dins d'un rectangle. No toquen
    // l'economia: la producció la porta Panta, això només fa que el pantà
    // sembli viu. Va dins de Contingut perquè s'amagui amb el pantà sense comprar.
    public class EixamDeMosques : MonoBehaviour
    {
        [Header("Mosques")]
        [Tooltip("La mosca model. Es fa servir com a primera mosca i es clona per a la resta")]
        [SerializeField] private SpriteRenderer mosca;
        [SerializeField, Range(1, 8)] private int quantes = 4;
        [Tooltip("Marca-ho si el dibuix de la mosca mira cap a la dreta")]
        [SerializeField] private bool elDibuixMiraALaDreta = true;

        [Header("Zona — rectangle verd en seleccionar, en unitats locals")]
        [SerializeField] private Vector2 centreDeLaZona = Vector2.zero;
        [SerializeField] private Vector2 midaDeLaZona = new Vector2(3f, 3f);

        [Header("Vol")]
        [SerializeField] private float velocitatMinima = 0.5f;
        [SerializeField] private float velocitatMaxima = 1.2f;
        [Tooltip("Com de pressa corregeix la direcció. Més baix = corbes més amples")]
        [SerializeField] private float gir = 3f;
        [Tooltip("Tremolor del brunzit, en unitats locals")]
        [SerializeField] private float amplitudDelBrunzit = 0.04f;
        [SerializeField] private float frequenciaDelBrunzit = 8f;

        [Header("Ordre de dibuix")]
        [Tooltip("La mosca del davant (la de Y més baixa) té ordreBase + quantes - 1")]
        [SerializeField] private int ordreBase = 50;

        [Header("Quan una granota se'n menja una")]
        [Tooltip("Segons fins que en torna a entrar una per la vora de la zona")]
        [Min(0)] [SerializeField] private float segonsFinsQueTorna = 1.5f;

        private const float DistanciaDArribada = 0.15f;

        // Volant: va a la seva. Reservada: una llengua hi va, però encara vola.
        // Capturada: la mou la llengua. Menjada: amagada fins que torna.
        private enum EstatDeMosca { Volant, Reservada, Capturada, Menjada }
        private EstatDeMosca[] _estats;
        private float[] _tornaA;

        private SpriteRenderer[] _mosques;
        private Vector2[] _posicions;
        private Vector2[] _velocitats;
        private Vector2[] _objectius;
        private float[] _rapidesa;
        private float[] _llavors;
        private float[] _alcades;
        private int[] _ordres;

        private void Awake()
        {
            if (mosca == null)
            {
                Debug.LogError($"[{name}] EixamDeMosques sense mosca model", this);
                enabled = false;
                return;
            }

            // Les posicions es treballen en l'espai local de l'eixam.
            if (mosca.transform.parent != transform)
                mosca.transform.SetParent(transform, true);

            _mosques = new SpriteRenderer[quantes];
            _posicions = new Vector2[quantes];
            _velocitats = new Vector2[quantes];
            _objectius = new Vector2[quantes];
            _rapidesa = new float[quantes];
            _llavors = new float[quantes];
            _alcades = new float[quantes];
            _ordres = new int[quantes];
            _estats = new EstatDeMosca[quantes];
            _tornaA = new float[quantes];

            for (int i = 0; i < quantes; i++)
            {
                _mosques[i] = i == 0 ? mosca : Instantiate(mosca, transform);
                _mosques[i].name = $"Mosca {i + 1}";
                _posicions[i] = PuntALAtzar();
                _objectius[i] = PuntALAtzar();
                _rapidesa[i] = Random.Range(velocitatMinima, velocitatMaxima);
                _llavors[i] = Random.value * 100f;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float suavitat = 1f - Mathf.Exp(-gir * dt);

            for (int i = 0; i < quantes; i++)
            {
                if (_estats[i] == EstatDeMosca.Menjada)
                {
                    if (Time.time < _tornaA[i]) continue;
                    Reapareixer(i);
                }
                if (_estats[i] == EstatDeMosca.Capturada)
                {
                    // La porta la llengua: només cal saber on és per a l'ordre.
                    _alcades[i] = _mosques[i].transform.localPosition.y;
                    continue;
                }

                Vector2 cap = _objectius[i] - _posicions[i];
                if (cap.sqrMagnitude < DistanciaDArribada * DistanciaDArribada)
                {
                    _objectius[i] = PuntALAtzar();
                    _rapidesa[i] = Random.Range(velocitatMinima, velocitatMaxima);
                    cap = _objectius[i] - _posicions[i];
                }

                Vector2 desitjada = cap.normalized * _rapidesa[i];
                _velocitats[i] = Vector2.Lerp(_velocitats[i], desitjada, suavitat);
                _posicions[i] += _velocitats[i] * dt;

                float t = Time.time * frequenciaDelBrunzit;
                Vector2 brunzit = new Vector2(
                    Mathf.PerlinNoise(_llavors[i], t) - 0.5f,
                    Mathf.PerlinNoise(_llavors[i] + 37f, t) - 0.5f) * (2f * amplitudDelBrunzit);

                Vector2 visible = _posicions[i] + brunzit;
                Transform tr = _mosques[i].transform;
                tr.localPosition = new Vector3(visible.x, visible.y, tr.localPosition.z);
                _alcades[i] = visible.y;

                if (Mathf.Abs(_velocitats[i].x) > 0.05f)
                    _mosques[i].flipX = elDibuixMiraALaDreta ? _velocitats[i].x < 0f : _velocitats[i].x > 0f;
            }

            OrdreDeMosques.Calcular(_alcades, ordreBase, _ordres);
            for (int i = 0; i < quantes; i++)
                _mosques[i].sortingOrder = _ordres[i];
        }

        // ---------------------------------------------------------------
        // Per a la llengua de les granotes
        // ---------------------------------------------------------------

        public Vector3 Centre => transform.TransformPoint(centreDeLaZona);

        // La mosca lliure més a prop de 'desDe', o -1 si no en queda cap.
        // Reservada, cap altra granota no la triarà.
        public int Reservar(Vector3 desDe)
        {
            if (_mosques == null) return -1;
            int millor = -1;
            float millorDistancia = float.PositiveInfinity;
            for (int i = 0; i < quantes; i++)
            {
                if (_estats[i] != EstatDeMosca.Volant) continue;
                float d = ((Vector2)(_mosques[i].transform.position - desDe)).sqrMagnitude;
                if (d < millorDistancia) { millorDistancia = d; millor = i; }
            }
            if (millor >= 0) _estats[millor] = EstatDeMosca.Reservada;
            return millor;
        }

        public Vector3 Posicio(int i) => Valida(i) ? _mosques[i].transform.position : Centre;

        // A partir d'ara la mou qui l'ha capturada.
        public Transform Capturar(int i)
        {
            if (!Valida(i)) return null;
            _estats[i] = EstatDeMosca.Capturada;
            return _mosques[i].transform;
        }

        public void Menjada(int i)
        {
            if (!Valida(i)) return;
            _estats[i] = EstatDeMosca.Menjada;
            _mosques[i].enabled = false;
            _tornaA[i] = Time.time + segonsFinsQueTorna;
        }

        // La llengua ha fallat: la mosca s'escapa amb una estrebada en la
        // direcció que li diuen (la de la llengua) i se'n va lluny d'allà.
        public void Esquivar(int i, Vector3 direccioAlMon)
        {
            if (!Valida(i) || _estats[i] == EstatDeMosca.Menjada) return;
            if (_estats[i] == EstatDeMosca.Capturada)
                _posicions[i] = _mosques[i].transform.localPosition;

            Vector2 dir = transform.InverseTransformDirection(direccioAlMon);
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Random.insideUnitCircle.normalized;

            _velocitats[i] = dir * velocitatMaxima * 2.5f;
            Vector2 meitat = midaDeLaZona / 2f;
            Vector2 lluny = _posicions[i] + dir * midaDeLaZona.magnitude;
            _objectius[i] = new Vector2(
                Mathf.Clamp(lluny.x, centreDeLaZona.x - meitat.x, centreDeLaZona.x + meitat.x),
                Mathf.Clamp(lluny.y, centreDeLaZona.y - meitat.y, centreDeLaZona.y + meitat.y));
            _estats[i] = EstatDeMosca.Volant;
        }

        // Si la llenguada s'interromp, la mosca torna a volar des d'on sigui.
        public void Alliberar(int i)
        {
            if (!Valida(i) || _estats[i] == EstatDeMosca.Menjada) return;
            if (_estats[i] == EstatDeMosca.Capturada)
                _posicions[i] = _mosques[i].transform.localPosition;
            _estats[i] = EstatDeMosca.Volant;
        }

        private bool Valida(int i) => _mosques != null && i >= 0 && i < quantes;

        // Entra per una vora a l'atzar i se'n va cap a dins.
        private void Reapareixer(int i)
        {
            Vector2 meitat = midaDeLaZona / 2f;
            Vector2 p = PuntALAtzar() - centreDeLaZona;
            if (Random.value < 0.5f) p.x = Mathf.Sign(p.x == 0 ? 1 : p.x) * meitat.x;
            else p.y = Mathf.Sign(p.y == 0 ? 1 : p.y) * meitat.y;

            _posicions[i] = centreDeLaZona + p;
            _velocitats[i] = Vector2.zero;
            _objectius[i] = PuntALAtzar();
            _mosques[i].enabled = true;
            _estats[i] = EstatDeMosca.Volant;
        }

        private Vector2 PuntALAtzar() => centreDeLaZona + new Vector2(
            (Random.value - 0.5f) * midaDeLaZona.x,
            (Random.value - 0.5f) * midaDeLaZona.y);

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(centreDeLaZona, midaDeLaZona);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }

    public static class OrdreDeMosques
    {
        // Com més avall (Y més baixa), més a prop de la càmera i més amunt en
        // l'ordre. Els ordres van de ordreBase a ordreBase + n - 1, sense
        // repetir-se: amb Y iguals guanya la d'índex més alt, i així no
        // parpellegen quan es creuen.
        public static void Calcular(float[] alcades, int ordreBase, int[] ordres)
        {
            int n = alcades.Length;
            for (int i = 0; i < n; i++)
            {
                int darrere = 0;
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    if (alcades[j] > alcades[i] || (alcades[j] == alcades[i] && j < i))
                        darrere++;
                }
                ordres[i] = ordreBase + darrere;
            }
        }
    }
}
