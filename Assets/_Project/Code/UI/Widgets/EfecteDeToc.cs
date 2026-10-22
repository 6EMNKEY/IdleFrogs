using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IdleFrogs.UI
{
    // Unes espurnes petites allà on toques, perquè el toc es vegi. Surten a
    // tot arreu, damunt del món i de la UI, i no toquen res: només és visual.
    //
    // Va en un objecte que ocupi tota la pantalla dins d'un Canvas Overlay per
    // sobre de tot (el de l'efecte de recompensa ja serveix).
    [RequireComponent(typeof(RectTransform))]
    public class EfecteDeToc : MonoBehaviour
    {
        [SerializeField] private Sprite espurna;
        [SerializeField] private Color color = new Color(1f, 0.95f, 0.6f, 1f);

        [Header("Esclat")]
        [Range(1, 12)] [SerializeField] private int quantes = 5;
        [SerializeField] private Vector2 mida = new Vector2(22, 22);
        [Tooltip("Fins on arriben, en píxels de referència")]
        [SerializeField] private float distancia = 45f;
        [SerializeField] private float durada = 0.35f;

        private RectTransform _rt;
        private Canvas _canvas;
        private readonly Stack<Image> _lliures = new Stack<Image>();

        private void Awake()
        {
            _rt = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            Pointer p = Pointer.current;
            if (p == null || espurna == null || !p.press.wasPressedThisFrame) return;
            Esclatar(p.position.ReadValue());
        }

        private void Esclatar(Vector2 pantalla)
        {
            Camera cam = _canvas != null && _canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.rootCanvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, pantalla, cam, out Vector2 origen)) return;

            float gir = Random.Range(0f, 360f);
            for (int i = 0; i < quantes; i++)
            {
                float angle = (gir + 360f * i / quantes + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                StartCoroutine(Volar(origen, dir * distancia * Random.Range(0.6f, 1f)));
            }
        }

        private IEnumerator Volar(Vector2 origen, Vector2 recorregut)
        {
            Image img = Agafar();
            RectTransform tr = img.rectTransform;
            float rotacio = Random.Range(-180f, 180f);

            for (float t = 0f; t < durada; t += Time.unscaledDeltaTime)
            {
                float u = t / durada;
                float sortida = 1f - (1f - u) * (1f - u);      // ràpid al principi
                tr.anchoredPosition = origen + recorregut * sortida;
                tr.localRotation = Quaternion.Euler(0f, 0f, rotacio * u);
                tr.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, u);
                Color c = color; c.a = color.a * (1f - u * u);
                img.color = c;
                yield return null;
            }

            img.gameObject.SetActive(false);
            _lliures.Push(img);
        }

        private Image Agafar()
        {
            Image img;
            if (_lliures.Count > 0) img = _lliures.Pop();
            else
            {
                var go = new GameObject("Espurna", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_rt, false);
                img = go.GetComponent<Image>();
                img.raycastTarget = false;   // no ha de tapar res
            }
            img.sprite = espurna;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = _rt.pivot;
            img.rectTransform.sizeDelta = mida;
            img.transform.SetAsLastSibling();
            img.gameObject.SetActive(true);
            return img;
        }
    }
}
