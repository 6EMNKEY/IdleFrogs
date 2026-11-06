using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IdleFrogs.UI
{
    // Tot el que es pot tocar creix una mica en tocar-ho: el jugador veu que
    // el toc ha arribat. No cal posar res a cap botó:
    //
    // - Els botons de la UI (Button, Toggle) creixen mentre els prems i
    //   tornen en deixar-los anar.
    // - Les coses del món i del mapa (pantans, granotes, tren, països...)
    //   igual: el DetectorDeTocs les fa créixer mentre les prems.
    //
    // Per treure-ho d'un botó concret: el component SensePulsacio. Els que
    // ocupen gairebé tota la pantalla (el fons fosc que tanca una finestra)
    // ja no creixen.
    public class PulsacioVisual : MonoBehaviour
    {
        private const float Escala = 1.08f;
        private const float Velocitat = 22f;
        private const float DuradaDelCop = 0.09f;

        private class Animacio
        {
            public Transform transform;
            public Vector3 original;
            public float actual = 1f;
            public bool premut;
            public float premutDes;
            public float fiDelCop;
        }

        private static PulsacioVisual _instancia;
        private readonly Dictionary<Transform, Animacio> _actives = new Dictionary<Transform, Animacio>();
        private readonly List<Transform> _perTreure = new List<Transform>();
        private readonly List<RaycastResult> _resultats = new List<RaycastResult>();
        private PointerEventData _dades;
        private EventSystem _esDeLesDades;
        private Transform _premut;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (_instancia != null) return;
            var go = new GameObject("PulsacioVisual");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<PulsacioVisual>();
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
        }

        // Un cop ràpid: creix i torna (les coses del món, en tocar-les).
        public static void Cop(Transform t)
        {
            if (_instancia == null || t == null) return;
            _instancia.Animar(t).fiDelCop = Time.unscaledTime + DuradaDelCop;
        }

        // Mentre el dit és a sobre (les coses del món). Si arrossegues, Amollar.
        public static void Premer(Transform t)
        {
            if (_instancia == null || t == null) return;
            Animacio a = _instancia.Animar(t);
            a.premut = true;
            a.premutDes = Time.unscaledTime;
        }

        // Un toc molt ràpid també es veu: es queda gran almenys DuradaDelCop.
        public static void Amollar(Transform t)
        {
            if (_instancia == null || t == null || !_instancia._actives.TryGetValue(t, out Animacio a)) return;
            a.premut = false;
            a.fiDelCop = Mathf.Max(a.fiDelCop, a.premutDes + DuradaDelCop);
        }

        private Animacio Animar(Transform t)
        {
            // Si ja s'està animant, la mida de debò és la que es va guardar al principi.
            if (!_actives.TryGetValue(t, out Animacio a))
            {
                a = new Animacio { transform = t, original = t.localScale };
                _actives.Add(t, a);
            }
            return a;
        }

        private void Update()
        {
            Pointer p = Pointer.current;
            if (p != null)
            {
                if (p.press.wasPressedThisFrame)
                {
                    Transform boto = BotoSota(p.position.ReadValue());
                    if (boto != null)
                    {
                        Animar(boto).premut = true;
                        _premut = boto;
                    }
                }
                if (!p.press.isPressed && _premut != null)
                {
                    if (_actives.TryGetValue(_premut, out Animacio a)) a.premut = false;
                    _premut = null;
                }
            }

            float k = 1f - Mathf.Exp(-Velocitat * Time.unscaledDeltaTime);
            _perTreure.Clear();
            foreach (Animacio a in _actives.Values)
            {
                Transform t = a.transform;
                if (t == null) { _perTreure.Add(t); continue; }
                // Si s'amaga a mitja animació (un botó que tanca el seu panell),
                // es queda amb la mida de sempre per quan torni a sortir.
                if (!t.gameObject.activeInHierarchy)
                {
                    t.localScale = a.original;
                    _perTreure.Add(t);
                    continue;
                }

                bool gran = a.premut || Time.unscaledTime < a.fiDelCop;
                a.actual = Mathf.Lerp(a.actual, gran ? Escala : 1f, k);
                if (!gran && Mathf.Abs(a.actual - 1f) < 0.002f)
                {
                    t.localScale = a.original;
                    _perTreure.Add(t);
                    continue;
                }
                t.localScale = a.original * a.actual;
            }
            foreach (Transform t in _perTreure) Treure(t);
        }

        private void Treure(Transform t)
        {
            if (t != null) { _actives.Remove(t); return; }
            // Destruït: les claus nul·les de Unity no es troben pel valor.
            var mortes = new List<Transform>();
            foreach (Transform k in _actives.Keys) if (k == null) mortes.Add(k);
            foreach (Transform k in mortes) _actives.Remove(k);
        }

        // El botó de la UI que hi ha sota el dit, si n'hi ha cap que es pugui prémer.
        private Transform BotoSota(Vector2 posicio)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return null;
            if (_dades == null || _esDeLesDades != es)
            {
                _dades = new PointerEventData(es);
                _esDeLesDades = es;
            }
            _dades.position = posicio;
            _resultats.Clear();
            es.RaycastAll(_dades, _resultats);
            if (_resultats.Count == 0) return null;

            GameObject tocat = _resultats[0].gameObject;
            Selectable s = tocat.GetComponentInParent<Button>();
            if (s == null) s = tocat.GetComponentInParent<Toggle>();
            if (s == null || !s.IsInteractable() || s.GetComponentInParent<SensePulsacio>() != null) return null;
            if (OcupaLaPantalla(s.transform as RectTransform)) return null;
            return s.transform;
        }

        private static bool OcupaLaPantalla(RectTransform r)
        {
            if (r == null) return false;
            Canvas c = r.GetComponentInParent<Canvas>();
            RectTransform arrel = c != null ? c.rootCanvas.transform as RectTransform : null;
            if (arrel == null) return false;
            Vector2 mida = Vector2.Scale(r.rect.size, r.lossyScale);
            Vector2 pantalla = Vector2.Scale(arrel.rect.size, arrel.lossyScale);
            return mida.x >= pantalla.x * 0.9f && mida.y >= pantalla.y * 0.9f;
        }
    }
}
