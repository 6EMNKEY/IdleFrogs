using System.Collections;
using UnityEngine;

namespace IdleFrogs.Game
{
    // La llengua de la granota: surt de la boca, atrapa una mosca i torna.
    //
    // La mosca és una de les de l'eixam del pantà: la lliure més a prop. La
    // llengua la persegueix mentre vola, se l'enduu i, a la boca, se la menja.
    // Si no en queda cap de lliure (sis granotes i quatre mosques), en surt
    // una de pròpia cap al centre de l'eixam.
    //
    // Va en un objecte fill de la granota col·locat a la boca. La llargada i
    // el ritme els porta aquest script amb una corba; el cos, si té Animator,
    // només rep el trigger i la velocitat perquè vagi al mateix temps.
    // Així la llengua arriba a la mosca sigui on sigui.
    public class LlenguaDeGranota : MonoBehaviour
    {
        [Header("Peces")]
        [Tooltip("La banda de la llengua, dibuixada horitzontal. Pivot a l'esquerra (la boca) " +
                 "i Draw Mode = Sliced perquè s'estiri sense deformar les puntes")]
        [SerializeField] private SpriteRenderer tram;
        [Tooltip("El cap enganxós. Es col·loca sol a l'extrem")]
        [SerializeField] private SpriteRenderer punta;
        [Tooltip("Mosca de recanvi: només surt quan l'eixam no en té cap de lliure, o si el pantà no en té")]
        [SerializeField] private SpriteRenderer mosca;

        [Header("Boca")]
        [Tooltip("El SpriteRenderer del cos de la granota")]
        [SerializeField] private SpriteRenderer cos;
        [Tooltip("El cos amb la boca oberta. Es posa mentre la llengua és fora")]
        [SerializeField] private Sprite bocaOberta;

        [Header("Ritme d'una llenguada")]
        [Tooltip("Segons que dura a velocitat normal. Si la recol·lecció és més ràpida, s'accelera")]
        [Min(0.05f)] [SerializeField] private float duracio = 0.45f;
        [Tooltip("Llargada al llarg de la llenguada (horitzontal: 0 a 1 del temps; vertical: 0 = a la boca, " +
                 "1 = a la mosca). La mosca s'enganxa al punt més alt")]
        [SerializeField] private AnimationCurve extensio = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.18f, 0f),     // anticipació: el cos s'ajup, la llengua encara no surt
            new Keyframe(0.40f, 1f),     // arriba a la mosca
            new Keyframe(0.78f, 0f),     // ja és a la boca
            new Keyframe(1f, 0f));       // empassar

        [Header("Mosca de recanvi, respecte a la boca")]
        [Min(0)] [SerializeField] private float distanciaMinima = 0.6f;
        [Min(0)] [SerializeField] private float distanciaMaxima = 1.2f;
        [Tooltip("Graus a cada costat de la direcció cap al centre de l'eixam")]
        [Range(0, 90)] [SerializeField] private float obertura = 20f;
        [Tooltip("Si el pantà no té eixam. En graus: 0 = dreta, 90 = amunt, 180 = esquerra")]
        [SerializeField] private float angleMinim = 30f;
        [SerializeField] private float angleMaxim = 150f;

        [Header("Cos (opcional)")]
        [Tooltip("Si la granota té Animator, rep el trigger i el multiplicador de velocitat")]
        [SerializeField] private Animator animador;
        [SerializeField] private string trigger = "Llengua";
        [Tooltip("Paràmetre float per al Speed Multiplier de l'estat de la llenguada")]
        [SerializeField] private string parametreDeVelocitat = "VelocitatLlengua";

        public float Duracio => duracio;

        private Coroutine _actual;
        private float _momentDelPic;
        private Sprite _bocaTancada;

        // On és la boca amb la granota sense girar (la posició del prefab).
        private float _xDeLaBoca;
        private bool _xGuardada;

        // La mosca de l'eixam que porta la llenguada en curs, per tornar-la
        // si s'interromp.
        private EixamDeMosques _eixam;
        private int _moscaDeLEixam = -1;

        private void Awake()
        {
            _momentDelPic = MomentDelPic();
            // La boca tancada és el que tingui posat el cos d'entrada.
            if (cos != null) _bocaTancada = cos.sprite;
            Amagar();
        }

        // Quan la granota es gira, la boca passa a l'altra banda del cos.
        // Pot cridar-se abans d'Awake (granotes d'un pantà encara sense comprar).
        public void Girar(bool girada)
        {
            if (!_xGuardada) { _xDeLaBoca = Mathf.Abs(transform.localPosition.x); _xGuardada = true; }
            Vector3 p = transform.localPosition;
            transform.localPosition = new Vector3(girada ? -_xDeLaBoca : _xDeLaBoca, p.y, p.z);
        }

        private void OnDisable()
        {
            _actual = null;
            AlliberarLaMosca();
            Amagar();
        }

        // La crida la granota. 'durada' pot ser més curta que Duracio quan la
        // recol·lecció va ràpida. 'eixam' pot ser null. Amb 'atrapa' a false
        // la mosca esquiva just quan la llengua hi arriba.
        public void Llancar(float durada, EixamDeMosques eixam = null, bool atrapa = true)
        {
            if (!isActiveAndEnabled || durada <= 0) return;
            if (_actual != null) StopCoroutine(_actual);
            AlliberarLaMosca();
            _actual = StartCoroutine(Llancament(durada, eixam, atrapa));
        }

        private IEnumerator Llancament(float durada, EixamDeMosques eixam, bool atrapa)
        {
            if (animador != null)
            {
                if (TeParametre(parametreDeVelocitat, AnimatorControllerParameterType.Float))
                    animador.SetFloat(parametreDeVelocitat, duracio / durada);
                if (TeParametre(trigger, AnimatorControllerParameterType.Trigger))
                    animador.SetTrigger(trigger);
            }

            _eixam = eixam;
            _moscaDeLEixam = eixam != null ? eixam.Reservar(transform.position) : -1;
            bool deLEixam = _moscaDeLEixam >= 0;

            Vector3 objectiu = deLEixam ? eixam.Posicio(_moscaDeLEixam)
                             : eixam != null ? PuntCap(eixam.Centre)
                             : PuntALAtzar();
            Transform enduta = null;
            bool enganxada = false;    // la llengua ja ha arribat (l'agafi o no)
            Vector3 fuga = Vector3.zero;
            float momentDeFuga = 0f;

            for (float t = 0f; t < durada; t += Time.deltaTime)
            {
                float u = t / durada;
                float e = Mathf.Clamp01(extensio.Evaluate(u));

                if (!enganxada)
                {
                    // Vola mentre la llengua hi va: es persegueix on sigui ara.
                    if (deLEixam) objectiu = eixam.Posicio(_moscaDeLEixam);
                    if (u >= _momentDelPic)
                    {
                        enganxada = true;
                        fuga = DireccioDeFuga(objectiu);
                        momentDeFuga = t;
                        if (atrapa && deLEixam) enduta = eixam.Capturar(_moscaDeLEixam);
                        else if (deLEixam)
                        {
                            eixam.Esquivar(_moscaDeLEixam, fuga);
                            _moscaDeLEixam = -1;
                        }
                    }
                }

                Vector3 extrem = Pintar(objectiu, e, enganxada && atrapa, !deLEixam);

                // La de recanvi, quan falla: se'n va volant i desapareix quan
                // la llengua ja és a la boca.
                if (!atrapa && enganxada && !deLEixam && mosca != null)
                {
                    Vector3 on = objectiu + fuga * (t - momentDeFuga) * 3f;
                    mosca.enabled = e > 0.001f;
                    mosca.transform.position = new Vector3(on.x, on.y, mosca.transform.position.z);
                }

                if (enduta != null)
                {
                    enduta.position = new Vector3(extrem.x, extrem.y, enduta.position.z);
                    // A la boca: menjada.
                    if (e <= 0.001f)
                    {
                        eixam.Menjada(_moscaDeLEixam);
                        _moscaDeLEixam = -1;
                        enduta = null;
                    }
                }
                yield return null;
            }

            // Si l'últim fotograma no arriba just a zero, igualment se l'ha menjat.
            if (_moscaDeLEixam >= 0 && enganxada && atrapa) { eixam.Menjada(_moscaDeLEixam); _moscaDeLEixam = -1; }
            AlliberarLaMosca();
            Amagar();
            _actual = null;
        }

        // Cap a un costat de la llengua, a l'atzar: sembla que l'esquivi.
        private Vector3 DireccioDeFuga(Vector3 objectiu)
        {
            Vector3 cap = objectiu - transform.position;
            cap.z = 0f;
            if (cap.sqrMagnitude < 1e-6f) return Vector3.up;
            Vector3 costat = new Vector3(-cap.y, cap.x, 0f).normalized * (Random.value < 0.5f ? -1f : 1f);
            return (costat + cap.normalized * 0.5f).normalized;
        }

        private void AlliberarLaMosca()
        {
            if (_eixam != null && _moscaDeLEixam >= 0) _eixam.Alliberar(_moscaDeLEixam);
            _moscaDeLEixam = -1;
        }

        // Torna on queda l'extrem de la llengua.
        private Vector3 Pintar(Vector3 objectiu, float e, bool enganxada, bool moscaPropia)
        {
            Vector3 boca = transform.position;
            Vector3 cap = objectiu - boca;
            cap.z = 0f;
            float distancia = cap.magnitude;
            if (distancia < 1e-4f) return boca;

            Vector3 direccio = cap / distancia;
            float llargada = distancia * e;
            bool fora = llargada > 0.01f;

            // Mateixa corba que la llengua: s'obre quan surt i es tanca quan
            // torna, també quan les llenguades s'acceleren.
            if (cos != null && bocaOberta != null) cos.sprite = fora ? bocaOberta : _bocaTancada;

            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direccio.y, direccio.x) * Mathf.Rad2Deg);
            Vector3 extrem = boca + direccio * llargada;

            if (tram != null)
            {
                tram.enabled = fora;
                if (fora) Estirar(tram, llargada);
            }
            if (punta != null)
            {
                punta.enabled = fora;
                punta.transform.position = new Vector3(extrem.x, extrem.y, punta.transform.position.z);
            }
            if (mosca != null)
            {
                // La de recanvi: abans d'atrapar-la espera al seu lloc; després
                // viatja enganxada a la punta, i a la boca ja se l'ha menjada.
                mosca.enabled = moscaPropia && (!enganxada || fora);
                Vector3 on = enganxada ? extrem : objectiu;
                mosca.transform.position = new Vector3(on.x, on.y, mosca.transform.position.z);
            }
            return extrem;
        }

        // Sliced: la mida en unitats locals. Simple: s'escala, i les puntes
        // es deformen (per això es recomana Sliced).
        private static void Estirar(SpriteRenderer sr, float llargadaAlMon)
        {
            float escalaPare = Mathf.Abs(sr.transform.parent != null ? sr.transform.parent.lossyScale.x : 1f);
            if (escalaPare < 1e-4f) escalaPare = 1f;
            float local = llargadaAlMon / escalaPare;

            if (sr.drawMode != SpriteDrawMode.Simple)
            {
                sr.size = new Vector2(local, sr.size.y);
            }
            else if (sr.sprite != null && sr.sprite.bounds.size.x > 0)
            {
                Vector3 s = sr.transform.localScale;
                sr.transform.localScale = new Vector3(local / sr.sprite.bounds.size.x, s.y, s.z);
            }
        }

        // Cap a on hi ha l'eixam, amb una mica de desviació i sense passar-se'n.
        private Vector3 PuntCap(Vector3 centre)
        {
            Vector3 d = centre - transform.position;
            d.z = 0f;
            float base_ = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float angle = (base_ + Random.Range(-obertura, obertura)) * Mathf.Deg2Rad;
            float r = Mathf.Min(d.magnitude, Random.Range(distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima)));
            return transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
        }

        private Vector3 PuntALAtzar()
        {
            float angle = Random.Range(angleMinim, angleMaxim) * Mathf.Deg2Rad;
            float r = Random.Range(distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima));
            return transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
        }

        private float MomentDelPic()
        {
            float millor = 0f, valor = float.NegativeInfinity;
            if (extensio == null) return 0.5f;
            foreach (Keyframe k in extensio.keys)
                if (k.value > valor) { valor = k.value; millor = k.time; }
            return millor;
        }

        private bool TeParametre(string nom, AnimatorControllerParameterType tipus)
        {
            if (string.IsNullOrEmpty(nom) || animador.runtimeAnimatorController == null) return false;
            foreach (AnimatorControllerParameter p in animador.parameters)
                if (p.name == nom && p.type == tipus) return true;
            return false;
        }

        private void Amagar()
        {
            if (tram != null) tram.enabled = false;
            if (punta != null) punta.enabled = false;
            if (mosca != null) mosca.enabled = false;
            if (cos != null && _bocaTancada != null) cos.sprite = _bocaTancada;
        }

#if UNITY_EDITOR
        private void OnValidate() => _momentDelPic = MomentDelPic();

        // L'arc on pot aparèixer la mosca (groc).
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            Vector3 o = transform.position;
            const int passos = 16;
            for (int i = 0; i < passos; i++)
            {
                float a0 = Mathf.Lerp(angleMinim, angleMaxim, i / (float)passos) * Mathf.Deg2Rad;
                float a1 = Mathf.Lerp(angleMinim, angleMaxim, (i + 1) / (float)passos) * Mathf.Deg2Rad;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1));
                Gizmos.DrawLine(o + d0 * distanciaMinima, o + d1 * distanciaMinima);
                Gizmos.DrawLine(o + d0 * distanciaMaxima, o + d1 * distanciaMaxima);
            }
        }
#endif
    }
}
