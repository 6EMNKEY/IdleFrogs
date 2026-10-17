using System.Collections;
using UnityEngine;

namespace IdleFrogs.Game
{
    // La llengua de la granota: surt de la boca, atrapa una mosca i torna.
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
        [Tooltip("La mosca que atrapa. Apareix on la llengua anirà a buscar-la")]
        [SerializeField] private SpriteRenderer mosca;

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

        [Header("On apareix la mosca, respecte a la boca")]
        [Min(0)] [SerializeField] private float distanciaMinima = 0.6f;
        [Min(0)] [SerializeField] private float distanciaMaxima = 1.2f;
        [Tooltip("En graus: 0 = dreta, 90 = amunt, 180 = esquerra")]
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

        private void Awake()
        {
            _momentDelPic = MomentDelPic();
            Amagar();
        }

        private void OnDisable()
        {
            _actual = null;
            Amagar();
        }

        // La crida la granota. 'durada' pot ser més curta que Duracio quan la
        // recol·lecció va ràpida.
        public void Llancar(float durada)
        {
            if (!isActiveAndEnabled || durada <= 0) return;
            if (_actual != null) StopCoroutine(_actual);
            _actual = StartCoroutine(Llancament(PuntALAtzar(), durada));
        }

        private IEnumerator Llancament(Vector3 objectiu, float durada)
        {
            if (animador != null)
            {
                if (TeParametre(parametreDeVelocitat, AnimatorControllerParameterType.Float))
                    animador.SetFloat(parametreDeVelocitat, duracio / durada);
                if (TeParametre(trigger, AnimatorControllerParameterType.Trigger))
                    animador.SetTrigger(trigger);
            }

            bool enganxada = false;
            for (float t = 0f; t < durada; t += Time.deltaTime)
            {
                float u = t / durada;
                if (!enganxada && u >= _momentDelPic) enganxada = true;
                Pintar(objectiu, Mathf.Clamp01(extensio.Evaluate(u)), enganxada);
                yield return null;
            }

            Amagar();
            _actual = null;
        }

        private void Pintar(Vector3 objectiu, float e, bool enganxada)
        {
            Vector3 boca = transform.position;
            Vector3 cap = objectiu - boca;
            cap.z = 0f;
            float distancia = cap.magnitude;
            if (distancia < 1e-4f) return;

            Vector3 direccio = cap / distancia;
            float llargada = distancia * e;
            bool fora = llargada > 0.01f;

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
                // Abans d'atrapar-la espera al seu lloc; després viatja enganxada
                // a la punta, i quan la llengua és a la boca ja se l'ha menjada.
                mosca.enabled = !enganxada || fora;
                Vector3 on = enganxada ? extrem : objectiu;
                mosca.transform.position = new Vector3(on.x, on.y, mosca.transform.position.z);
            }
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
