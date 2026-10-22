using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    public enum TipusDeMoneda { Doblers, Gemes }

    // L'esclat de monedes o gemes que surt d'un punt i vola cap al comptador
    // del HUD. Només és visual: els doblers i les gemes ja els ha donat qui
    // crida, abans o després, i el comptador s'actualitza pel seu compte.
    //
    // Va en un objecte que ocupi tota la pantalla dins d'un Canvas per sobre
    // dels panells, perquè les icones no quedin tapades.
    [RequireComponent(typeof(RectTransform))]
    public class EfecteDeRecompensa : MonoBehaviour
    {
        public static EfecteDeRecompensa Instancia { get; private set; }

        [Header("Cap on volen (els comptadors del HUD)")]
        [SerializeField] private RectTransform destiDoblers;
        [SerializeField] private RectTransform destiGemes;

        [Header("Icones")]
        [SerializeField] private Sprite iconaDoblers;
        [SerializeField] private Sprite iconaGemes;
        [SerializeField] private Vector2 midaDeLIcona = new Vector2(64, 64);

        [Header("Mida de l'esclat — de mínim a màxim segons la recompensa")]
        [Tooltip("Una recompensa petita comparada amb el que ja tens surt amb el mínim; " +
                 "una que iguala o supera el que tenies (o una compra) amb el màxim")]
        [Range(1, 60)] [SerializeField] private int quantesMinimes = 5;
        [Range(1, 60)] [SerializeField] private int quantesMaximes = 25;

        [Header("Esclat")]
        [Tooltip("Fins on s'escampen al voltant de l'origen, en píxels de referència")]
        [SerializeField] private float radiMinim = 80f;
        [SerializeField] private float radiMaxim = 240f;
        [SerializeField] private float duracioDeLEsclat = 0.35f;
        [SerializeField] private float pausa = 0.1f;

        [Header("Viatge")]
        [SerializeField] private float duracioDelViatge = 0.6f;
        [Tooltip("Com s'esgranen: segons entre la primera i l'última que surten cap al comptador")]
        [SerializeField] private float escampatDeSortida = 0.3f;
        [Tooltip("Quant es corben de camí (0 = recte)")]
        [Range(0, 1)] [SerializeField] private float corba = 0.35f;
        [SerializeField] private float escalaEnArribar = 0.6f;

        [Header("El comptador quan n'hi arriba una")]
        [SerializeField] private float copDEscala = 1.2f;
        [SerializeField] private float duracioDelCop = 0.15f;

        private RectTransform _rt;
        private Canvas _canvas;
        private readonly Stack<Image> _lliures = new Stack<Image>();

        // El que ja és al compte però el comptador encara no ensenya: es va
        // alliberant a mesura que arriben les icones, i així el número puja
        // a poc a poc. Un per moneda.
        private readonly double[] _retingut = new double[2];
        public event System.Action<TipusDeMoneda> EnCanviDelQueEsMostra;

        // Els comptadors del HUD ensenyen el saldo menys això.
        public double Retingut(TipusDeMoneda tipus) => _retingut[(int)tipus];

        // Un esclat: el que queda per alliberar i entre quantes icones.
        private class Llancament
        {
            public TipusDeMoneda tipus;
            public double queda;
            public int iconesQueden;
        }

        // El "bot" del comptador: es reinicia cada cop que n'arriba una.
        private float _copDoblers, _copGemes;
        private Vector3 _escalaDoblers = Vector3.one, _escalaGemes = Vector3.one;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            _rt = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (destiDoblers != null) _escalaDoblers = destiDoblers.localScale;
            if (destiGemes != null) _escalaGemes = destiGemes.localScale;
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // Si s'apaga a mitges, les corrutines s'aturen: el que quedava retingut
        // s'allibera de cop perquè el comptador no es quedi enganxat a sota.
        private void OnDisable()
        {
            for (int i = 0; i < _retingut.Length; i++)
            {
                if (_retingut[i] <= 0) continue;
                _retingut[i] = 0;
                EnCanviDelQueEsMostra?.Invoke((TipusDeMoneda)i);
            }
            foreach (Transform fill in transform)
                if (fill.gameObject.activeSelf && fill.TryGetComponent(out Image img)) Tornar(img);
        }

        // ---------------------------------------------------------------
        // Per cridar
        // ---------------------------------------------------------------

        // 'quantitat' és el que s'acaba de donar i ja és al comptador: el que
        // es tenia abans es dedueix restant-ho. Amb 'esCompra' surt el màxim.
        // La moneda del país on ets (AspecteDelPais).
        public void EstablirIconaDeDoblers(Sprite icona)
        {
            if (icona != null) iconaDoblers = icona;
        }

        public void Llancar(TipusDeMoneda tipus, Vector2 posicioDePantalla, double quantitat, bool esCompra = false)
        {
            RectTransform desti = Desti(tipus);
            Sprite icona = tipus == TipusDeMoneda.Gemes ? iconaGemes : iconaDoblers;
            if (desti == null || icona == null || !isActiveAndEnabled) return;

            float intensitat = IntensitatDeRecompensa.Calcular(quantitat, Saldo(tipus) - quantitat, esCompra);
            int quantes = Mathf.RoundToInt(Mathf.Lerp(quantesMinimes, Mathf.Max(quantesMinimes, quantesMaximes), intensitat));
            // Les gemes es compten d'una en una: mai més icones que gemes guanyades.
            if (tipus == TipusDeMoneda.Gemes)
                quantes = Mathf.Min(quantes, Mathf.Max(1, (int)System.Math.Floor(quantitat)));
            float radi = Mathf.Lerp(radiMinim, Mathf.Max(radiMinim, radiMaxim), intensitat);
            Vector2 origen = ALocal(posicioDePantalla);

            var llancament = new Llancament { tipus = tipus, queda = System.Math.Max(0, quantitat), iconesQueden = quantes };
            _retingut[(int)tipus] += llancament.queda;
            EnCanviDelQueEsMostra?.Invoke(tipus);

            for (int i = 0; i < quantes; i++)
            {
                float retard = quantes > 1 ? escampatDeSortida * i / (quantes - 1) : 0f;
                StartCoroutine(Volar(llancament, icona, origen, radi, retard));
            }
        }

        // Des d'un element de la UI (el text de la recompensa, una carta...).
        public void LlancarDesDe(TipusDeMoneda tipus, RectTransform origen, double quantitat, bool esCompra = false)
        {
            if (origen == null) return;
            Llancar(tipus, PuntDePantalla(origen), quantitat, esCompra);
        }

        // Des d'un punt del món (un pantà, la tenda...).
        public void LlancarDesDelMon(TipusDeMoneda tipus, Vector3 posicio, double quantitat, bool esCompra = false)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Llancar(tipus, cam.WorldToScreenPoint(posicio), quantitat, esCompra);
        }

        // On és un element a la pantalla. Serveix per guardar-lo abans que
        // desaparegui (una carta que es ven) i llançar després.
        public static Vector2 PuntDePantalla(RectTransform rt)
        {
            Canvas c = rt.GetComponentInParent<Canvas>();
            return RectTransformUtility.WorldToScreenPoint(CameraDe(c), rt.position);
        }

        private static double Saldo(TipusDeMoneda tipus)
        {
            if (CurrencyManager.Instancia == null) return 0;
            return tipus == TipusDeMoneda.Gemes ? CurrencyManager.Instancia.gemesActuals : CurrencyManager.Instancia.doblersActuals;
        }

        // ---------------------------------------------------------------
        // Animació
        // ---------------------------------------------------------------

        private IEnumerator Volar(Llancament llancament, Sprite icona, Vector2 origen, float radi, float retard)
        {
            TipusDeMoneda tipus = llancament.tipus;
            Image img = Agafar(icona);
            RectTransform tr = img.rectTransform;

            // 1. Esclat: surt de l'origen cap a un punt a l'atzar del voltant.
            Vector2 esclat = origen + Random.insideUnitCircle.normalized * radi * Random.Range(0.4f, 1f);
            for (float t = 0f; t < duracioDeLEsclat; t += Time.unscaledDeltaTime)
            {
                float u = t / duracioDeLEsclat;
                tr.anchoredPosition = Vector2.LerpUnclamped(origen, esclat, SortidaCubica(u));
                tr.localScale = Vector3.one * SortidaAmbRebot(u);
                yield return null;
            }
            tr.anchoredPosition = esclat;
            tr.localScale = Vector3.one;

            // 2. Espera, esgranades perquè no arribin totes de cop.
            float espera = pausa + retard;
            for (float t = 0f; t < espera; t += Time.unscaledDeltaTime) yield return null;

            // 3. Viatge corbat cap al comptador, accelerant al final. El destí
            //    es llegeix cada fotograma per si el HUD es mou.
            float costat = Random.value < 0.5f ? -1f : 1f;
            for (float t = 0f; t < duracioDelViatge; t += Time.unscaledDeltaTime)
            {
                float u = t / duracioDelViatge;
                float e = u * u;
                Vector2 desti = DestiLocal(tipus);
                Vector2 meitat = (esclat + desti) / 2f;
                Vector2 perpendicular = new Vector2(-(desti - esclat).y, (desti - esclat).x) * (corba * costat);
                Vector2 control = meitat + perpendicular;
                tr.anchoredPosition = Bezier(esclat, control, desti, e);
                tr.localScale = Vector3.one * Mathf.Lerp(1f, escalaEnArribar, e);
                yield return null;
            }

            Tornar(img);
            Copejar(tipus);
            Alliberar(llancament);
        }

        private void Update()
        {
            AplicarCop(destiDoblers, ref _copDoblers, _escalaDoblers);
            AplicarCop(destiGemes, ref _copGemes, _escalaGemes);
        }

        // Cada icona allibera la seva part; l'última, el que quedi, perquè
        // els decimals no deixin el comptador a una unitat del total.
        private void Alliberar(Llancament l)
        {
            if (l.iconesQueden <= 0) return;
            double part = l.iconesQueden == 1 ? l.queda : l.queda / l.iconesQueden;
            l.queda -= part;
            l.iconesQueden--;

            int i = (int)l.tipus;
            _retingut[i] = System.Math.Max(0, _retingut[i] - part);
            EnCanviDelQueEsMostra?.Invoke(l.tipus);
        }

        private void Copejar(TipusDeMoneda tipus)
        {
            if (tipus == TipusDeMoneda.Gemes) _copGemes = duracioDelCop;
            else _copDoblers = duracioDelCop;
        }

        private void AplicarCop(RectTransform desti, ref float queda, Vector3 escalaBase)
        {
            if (desti == null || queda <= 0f) return;
            queda -= Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(1f - queda / duracioDelCop);
            float bot = Mathf.Sin(u * Mathf.PI);
            desti.localScale = escalaBase * Mathf.Lerp(1f, copDEscala, queda > 0f ? bot : 0f);
        }

        // ---------------------------------------------------------------
        // Icones reaprofitades
        // ---------------------------------------------------------------

        private Image Agafar(Sprite icona)
        {
            Image img;
            if (_lliures.Count > 0) img = _lliures.Pop();
            else
            {
                var go = new GameObject("Icona", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_rt, false);
                img = go.GetComponent<Image>();
                img.raycastTarget = false;   // no ha de tapar cap botó
            }
            img.sprite = icona;
            // Ancorada al pivot del contenidor: així anchoredPosition i el punt
            // local que dona ScreenPointToLocalPointInRectangle són el mateix.
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = _rt.pivot;
            img.rectTransform.sizeDelta = midaDeLIcona;
            img.transform.SetAsLastSibling();
            img.gameObject.SetActive(true);
            return img;
        }

        private void Tornar(Image img)
        {
            img.gameObject.SetActive(false);
            _lliures.Push(img);
        }

        // ---------------------------------------------------------------
        // Coordenades
        // ---------------------------------------------------------------

        private RectTransform Desti(TipusDeMoneda tipus) =>
            tipus == TipusDeMoneda.Gemes ? destiGemes : destiDoblers;

        private Vector2 DestiLocal(TipusDeMoneda tipus)
        {
            RectTransform desti = Desti(tipus);
            Canvas c = desti.GetComponentInParent<Canvas>();
            return ALocal(RectTransformUtility.WorldToScreenPoint(CameraDe(c), desti.position));
        }

        private Vector2 ALocal(Vector2 pantalla)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, pantalla, CameraDe(_canvas), out Vector2 local);
            return local;
        }

        // Overlay no fa servir càmera; Screen Space - Camera i World Space sí.
        private static Camera CameraDe(Canvas c)
        {
            if (c == null) return null;
            Canvas arrel = c.rootCanvas;
            return arrel.renderMode == RenderMode.ScreenSpaceOverlay ? null : arrel.worldCamera;
        }

        private static float SortidaCubica(float u) => 1f - Mathf.Pow(1f - u, 3f);

        private static float SortidaAmbRebot(float u)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = u - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float m = 1f - t;
            return m * m * a + 2f * m * t * b + t * t * c;
        }
    }
}
