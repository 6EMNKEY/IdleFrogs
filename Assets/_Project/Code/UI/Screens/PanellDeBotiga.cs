using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La botiga: un sol scroll horitzontal amb totes les seccions seguides
    // (Ofertes, Pase, Gratis, Gemes, Millores). Les pestanyes de dalt hi fan
    // de marcadors: tocar-ne una hi llisca, i en fer scroll s'il·lumina la de
    // la secció que es veu.
    //
    // Les targetes es munten soles a partir del catàleg, en columnes de dues
    // files: dues petites apilades, o una gran que ocupa tota la columna (o
    // dues columnes, la doble).
    public class PanellDeBotiga : MonoBehaviour
    {
        public static PanellDeBotiga Instancia { get; private set; }

        [Header("Obrir i tancar")]
        [SerializeField] private GameObject arrel;
        [Tooltip("El botó de la botiga del HUD")]
        [SerializeField] private Button botoObrir;
        [SerializeField] private Button botoTancar;

        [Header("Scroll")]
        [Tooltip("Horizontal marcat, Vertical desmarcat")]
        [SerializeField] private ScrollRect scroll;
        [Tooltip("El Content del scroll: pivot i ancoratge a l'esquerra. S'hi afegeix sol el layout")]
        [SerializeField] private RectTransform contingut;

        [Header("Pestanyes (en ordre: Ofertes, Pase, Gratis, Gemes, Millores)")]
        [SerializeField] private PestanyaDeBotiga[] pestanyes;

        [Header("Targetes")]
        [SerializeField] private TargetaDeProducte prefabPetita;
        [SerializeField] private TargetaDeProducte prefabGran;
        [Tooltip("La recompensa diària: la primera de Gratis, gran")]
        [SerializeField] private TargetaDelCalendari prefabCalendari;

        [Header("Mides")]
        [SerializeField] private float ampladaDeColumna = 300f;
        [SerializeField] private float espai = 16f;
        [SerializeField] private float espaiEntreSeccions = 56f;
        [SerializeField] private float marge = 24f;

        [Header("Missatge — \"Has rebut 500 gemes\"")]
        [SerializeField] private TMP_Text textMissatge;
        [SerializeField] private float segonsDelMissatge = 2.5f;

        private static readonly SeccioDeBotiga[] Ordre =
            { SeccioDeBotiga.Ofertes, SeccioDeBotiga.Pase, SeccioDeBotiga.Gratis, SeccioDeBotiga.Gemes, SeccioDeBotiga.Millores };

        private readonly Dictionary<SeccioDeBotiga, RectTransform> _seccions = new Dictionary<SeccioDeBotiga, RectTransform>();
        private readonly List<TargetaDeProducte> _targetes = new List<TargetaDeProducte>();
        private TargetaDelCalendari _calendari;
        private bool _muntada;
        private Coroutine _lliscar, _amagarMissatge;

        public bool EstaOberta => arrel != null && arrel.activeSelf;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoObrir != null) botoObrir.onClick.AddListener(Obrir);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (pestanyes != null)
                foreach (PestanyaDeBotiga p in pestanyes)
                    if (p != null) p.Inicialitzar(this);
            if (scroll != null) scroll.onValueChanged.AddListener(_ => IlluminarLaPestanya());

            if (arrel != null) arrel.SetActive(false);
            if (textMissatge != null) textMissatge.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Textos.EnCanviDIdioma += Refrescar;
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi += RefrescarSiOberta;
        }

        private void OnDisable()
        {
            Textos.EnCanviDIdioma -= Refrescar;
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi -= RefrescarSiOberta;
        }

        // El gestor pot néixer després del nostre OnEnable: ens hi subscrivim també aquí.
        private void Start()
        {
            if (GestorDeBotiga.Instancia != null)
            {
                GestorDeBotiga.Instancia.EnCanvi -= RefrescarSiOberta;
                GestorDeBotiga.Instancia.EnCanvi += RefrescarSiOberta;
            }
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // ---------------------------------------------------------------
        // Obrir i tancar
        // ---------------------------------------------------------------

        public void Obrir()
        {
            if (PanellDeMillora.Instancia != null) PanellDeMillora.Instancia.Tancar();
            if (PanellDEncarregats.Instancia != null) PanellDEncarregats.Instancia.Tancar();
            if (PanellDelMapa.Instancia != null) PanellDelMapa.Instancia.Tancar();

            if (arrel != null) arrel.SetActive(true);
            if (!_muntada) Muntar();

            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (g != null && g.Cataleg != null)
                foreach (ProducteDeBotiga p in g.Cataleg.productes) g.MarcarComVist(p);
            Refrescar();

            // Si hi ha alguna cosa gratis per recollir, s'obre allà.
            Canvas.ForceUpdateCanvases();
            AnarA(g != null && g.HiHaGratisPerRecollir ? SeccioDeBotiga.Gratis : SeccioDeBotiga.Ofertes, false);
        }

        public void Tancar()
        {
            if (arrel != null) arrel.SetActive(false);
        }

        // ---------------------------------------------------------------
        // Muntar
        // ---------------------------------------------------------------

        private void Muntar()
        {
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (g == null || g.Cataleg == null || contingut == null)
            {
                Debug.LogWarning("PanellDeBotiga: falta el GestorDeBotiga amb el seu catàleg, o el Content del scroll", this);
                return;
            }
            _muntada = true;

            HorizontalLayoutGroup fila = Layout<HorizontalLayoutGroup>(contingut.gameObject);
            fila.spacing = espaiEntreSeccions;
            fila.padding = new RectOffset((int)marge, (int)marge, (int)espai, (int)espai);
            fila.childControlWidth = true; fila.childControlHeight = true;
            fila.childForceExpandWidth = false; fila.childForceExpandHeight = true;
            ContentSizeFitter ajust = Layout<ContentSizeFitter>(contingut.gameObject);
            ajust.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ajust.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            foreach (SeccioDeBotiga s in Ordre)
            {
                var productes = new List<ProducteDeBotiga>();
                foreach (ProducteDeBotiga p in g.Cataleg.productes)
                    if (p != null && p.seccio == s) productes.Add(p);
                bool ambCalendari = s == SeccioDeBotiga.Gratis && prefabCalendari != null;
                if (productes.Count == 0 && !ambCalendari) continue;

                RectTransform seccio = NouObjecte($"Seccio: {s}", contingut);
                HorizontalLayoutGroup columnes = seccio.gameObject.AddComponent<HorizontalLayoutGroup>();
                columnes.spacing = espai;
                columnes.childControlWidth = true; columnes.childControlHeight = true;
                columnes.childForceExpandWidth = false; columnes.childForceExpandHeight = true;
                _seccions[s] = seccio;

                if (ambCalendari)
                {
                    RectTransform col = Columna(seccio, 1);
                    _calendari = Instantiate(prefabCalendari, col);
                    _calendari.Inicialitzar(this);
                }

                RectTransform pendent = null;   // una columna amb una petita sola
                foreach (ProducteDeBotiga p in productes)
                {
                    if (p.mida == MidaDeTargeta.Petita)
                    {
                        if (pendent == null) { pendent = Columna(seccio, 1); Targeta(prefabPetita, p, pendent); }
                        else { Targeta(prefabPetita, p, pendent); pendent = null; }
                        continue;
                    }
                    pendent = null;
                    Targeta(prefabGran != null ? prefabGran : prefabPetita, p, Columna(seccio, p.mida == MidaDeTargeta.GranDoble ? 2 : 1));
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contingut);
        }

        // Una columna de dues files; les targetes de dins es reparteixen l'alçada.
        private RectTransform Columna(RectTransform seccio, int columnes)
        {
            RectTransform col = NouObjecte("Columna", seccio);
            VerticalLayoutGroup v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = espai;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = true;
            LayoutElement e = col.gameObject.AddComponent<LayoutElement>();
            e.preferredWidth = ampladaDeColumna * columnes + espai * (columnes - 1);
            e.minWidth = e.preferredWidth;
            return col;
        }

        private void Targeta(TargetaDeProducte prefab, ProducteDeBotiga p, RectTransform columna)
        {
            if (prefab == null) return;
            TargetaDeProducte t = Instantiate(prefab, columna);
            t.name = $"Targeta: {p.id}";
            t.Omplir(p, this);
            _targetes.Add(t);
        }

        private static RectTransform NouObjecte(string nom, Transform pare)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(pare, false);
            return (RectTransform)go.transform;
        }

        private static T Layout<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T c) ? c : go.AddComponent<T>();

        // ---------------------------------------------------------------
        // Refrescar
        // ---------------------------------------------------------------

        private void RefrescarSiOberta()
        {
            if (EstaOberta) Refrescar();
        }

        private void Refrescar()
        {
            foreach (TargetaDeProducte t in _targetes) if (t != null) t.Refrescar();
            if (_calendari != null) _calendari.Refrescar();
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (pestanyes != null)
                foreach (PestanyaDeBotiga p in pestanyes)
                    if (p != null) p.PosarAvis(p.Seccio == SeccioDeBotiga.Gratis && g != null && g.HiHaGratisPerRecollir);
        }

        // ---------------------------------------------------------------
        // Pestanyes i scroll
        // ---------------------------------------------------------------

        // On comença una secció, en unitats del Content (des de l'esquerra).
        private float IniciDe(RectTransform seccio)
        {
            var cantons = new Vector3[4];
            seccio.GetWorldCorners(cantons);
            return contingut.InverseTransformPoint(cantons[0]).x - contingut.rect.xMin;
        }

        public void AnarA(SeccioDeBotiga s, bool lliscant = true)
        {
            if (scroll == null || contingut == null || !_seccions.TryGetValue(s, out RectTransform seccio)) return;

            float viewport = scroll.viewport != null ? scroll.viewport.rect.width : ((RectTransform)scroll.transform).rect.width;
            float recorregut = contingut.rect.width - viewport;
            float desti = recorregut > 0 ? Mathf.Clamp01((IniciDe(seccio) - marge) / recorregut) : 0f;

            if (_lliscar != null) StopCoroutine(_lliscar);
            if (!lliscant || !isActiveAndEnabled) { scroll.horizontalNormalizedPosition = desti; IlluminarLaPestanya(); return; }
            _lliscar = StartCoroutine(Lliscar(desti));
        }

        private IEnumerator Lliscar(float desti)
        {
            scroll.StopMovement();
            float inici = scroll.horizontalNormalizedPosition;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                scroll.horizontalNormalizedPosition = Mathf.Lerp(inici, desti, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            scroll.horizontalNormalizedPosition = desti;
            _lliscar = null;
        }

        // La pestanya de la secció que hi ha a l'esquerra de la vista.
        private void IlluminarLaPestanya()
        {
            if (scroll == null || contingut == null || pestanyes == null) return;
            float viewport = scroll.viewport != null ? scroll.viewport.rect.width : ((RectTransform)scroll.transform).rect.width;
            float esquerra = (contingut.rect.width - viewport) * scroll.horizontalNormalizedPosition;
            float mira = esquerra + viewport * 0.35f;

            SeccioDeBotiga actual = SeccioDeBotiga.Ofertes;
            bool trobada = false;
            foreach (SeccioDeBotiga s in Ordre)
            {
                if (!_seccions.TryGetValue(s, out RectTransform r)) continue;
                if (!trobada || IniciDe(r) <= mira) { actual = s; trobada = true; }
            }
            // Arribat al final, l'última, encara que sigui estreta.
            if (scroll.horizontalNormalizedPosition > 0.995f)
                for (int i = Ordre.Length - 1; i >= 0; i--)
                    if (_seccions.ContainsKey(Ordre[i])) { actual = Ordre[i]; break; }

            foreach (PestanyaDeBotiga p in pestanyes)
                if (p != null) p.Seleccionar(p.Seccio == actual);
        }

        // ---------------------------------------------------------------
        // El que has rebut
        // ---------------------------------------------------------------

        // Les gemes i els diners volen cap al comptador des de la targeta; la
        // resta es diu amb un missatge.
        public void Rebut(List<Recompensa> donades, RectTransform origen, bool primeraDoble)
        {
            if (donades == null || donades.Count == 0)
            {
                Missatge(Textos.T("botiga.no_compra"));
                return;
            }

            EfecteDeRecompensa efecte = EfecteDeRecompensa.Instancia;
            if (efecte != null && origen != null)
                foreach (Recompensa r in donades)
                {
                    if (r.tipus == TipusDeRecompensa.Gemes)
                        efecte.LlancarDesDe(TipusDeMoneda.Gemes, origen, r.quantitat * (primeraDoble ? 2 : 1));
                    else if (r.tipus == TipusDeRecompensa.TempsDIngressos && GestorDeBotiga.Instancia != null)
                        efecte.LlancarDesDe(TipusDeMoneda.Doblers, origen, GestorDeBotiga.Instancia.DoblersDeTemps(r.quantitat * 60));
                }

            Missatge(Textos.F("botiga.rebut", TextosDeBotiga.Descripcio(donades, " + ")));
            Refrescar();
        }

        private void Missatge(string text)
        {
            if (textMissatge == null) { Debug.Log(text); return; }
            textMissatge.text = text;
            textMissatge.gameObject.SetActive(true);
            if (_amagarMissatge != null) StopCoroutine(_amagarMissatge);
            _amagarMissatge = StartCoroutine(AmagarMissatge());
        }

        private IEnumerator AmagarMissatge()
        {
            yield return new WaitForSecondsRealtime(segonsDelMissatge);
            if (textMissatge != null) textMissatge.gameObject.SetActive(false);
        }
    }
}
