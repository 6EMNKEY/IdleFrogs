using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleFrogs.Game;
using IdleFrogs.UI;

namespace IdleFrogs.EditorTools
{
    // Peces per als generadors d'escenes de UI (arena, recerca): escena amb
    // càmera, EventSystem i Canvas; caixes, textos i botons amb àncores
    // (proporcions del pare), llistes amb scroll, i les connexions.
    public static class EinesDEscenes
    {
        public static readonly Color Verd = new Color(0.55f, 0.77f, 0.17f);
        public static readonly Color Pergami = new Color(0.93f, 0.81f, 0.6f);
        public static readonly Color Panell = new Color(0.84f, 0.73f, 0.65f);
        public static readonly Color Vora = new Color(0.33f, 0.05f, 0.05f);
        public static readonly Color Fosc = new Color(0.37f, 0.26f, 0.19f);
        public static readonly Color Vermell = new Color(0.85f, 0.2f, 0.2f);
        public static readonly Color BotoVerd = new Color(0.3f, 0.7f, 0.25f);
        public static readonly Color BotoBlau = new Color(0.25f, 0.5f, 0.85f);
        public static readonly Color Ombra = new Color(0f, 0f, 0f, 0.6f);

        public static bool PotSubstituir(string ruta, string titol)
        {
            if (System.IO.File.Exists(ruta) &&
                !EditorUtility.DisplayDialog(titol, $"{ruta} ja existeix. La vols substituir?", "Substituir", "Cancel·lar"))
                return false;
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        // Escena buida amb càmera, EventSystem i un Canvas a 1920×1080 (apaïsat).
        public static Transform NovaEscena(string nomDelCanvas, out Scene escena)
        {
            escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            Camera camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Verd;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject(nomDelCanvas, typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalat = canvasGo.AddComponent<CanvasScaler>();
            escalat.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalat.referenceResolution = new Vector2(1920, 1080);
            escalat.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvasGo.transform;
        }

        public static void Desar(Scene escena, string ruta)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
            EditorSceneManager.SaveScene(escena, ruta);
            AfegirAlBuild(ruta);
        }

        public static RectTransform Buit(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(pare, false);
            var r = (RectTransform)go.transform;
            Ancorar(r, xMin, yMin, xMax, yMax);
            return r;
        }

        public static Image Caixa(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax, Color color)
        {
            Image i = Buit(nom, pare, xMin, yMin, xMax, yMax).gameObject.AddComponent<Image>();
            i.color = color;
            return i;
        }

        public static void Ancorar(RectTransform r, float xMin, float yMin, float xMax, float yMax)
        {
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        public static TMP_Text Text(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax, string text,
                                    float mida, Color color, TextAlignmentOptions alineacio = TextAlignmentOptions.Center)
        {
            var t = Buit(nom, pare, xMin, yMin, xMax, yMax).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.color = color;
            t.alignment = alineacio;
            t.enableAutoSizing = true;
            t.fontSizeMin = 12;
            t.fontSizeMax = mida;
            t.raycastTarget = false;
            return t;
        }

        public static TMP_Text Localitzat(TMP_Text t, string clau)
        {
            var loc = t.gameObject.AddComponent<TextLocalitzat>();
            var so = new SerializedObject(loc);
            so.FindProperty("clau").stringValue = clau;
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        // Un botó de color amb un text a dins (el text es pot traduir amb 'clau').
        public static Button Boto(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax,
                                  Color color, string text, float mida = 40, string clau = null)
        {
            Image i = Caixa(nom, pare, xMin, yMin, xMax, yMax, color);
            Button b = i.gameObject.AddComponent<Button>();
            TMP_Text t = Text("Text", i.transform, 0.05f, 0.05f, 0.95f, 0.95f, text, mida, Color.white);
            if (!string.IsNullOrEmpty(clau)) Localitzat(t, clau);
            return b;
        }

        public static TMP_Text TextDe(Button b) => b.GetComponentInChildren<TMP_Text>(true);

        // Fletxes < > a les vores de la pantalla.
        public static Button Fletxa(string nom, Transform pare, float xMin, float xMax, string signe)
        {
            Image i = Caixa(nom, pare, xMin, 0.42f, xMax, 0.58f, Pergami);
            Text("Text", i.transform, 0, 0, 1, 1, signe, 70, Color.black);
            return i.gameObject.AddComponent<Button>();
        }

        public static Button Creu(Transform pare, float xMin, float yMin, float xMax, float yMax)
        {
            Image i = Caixa("BotoTancar", pare, xMin, yMin, xMax, yMax, Vermell);
            Text("X", i.transform, 0, 0, 1, 1, "X", 60, Color.white);
            return i.gameObject.AddComponent<Button>();
        }

        // Una llista amb scroll vertical: torna el Content, amb un
        // VerticalLayoutGroup (o una graella si midaDeCella no és zero).
        public static RectTransform Llista(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax,
                                           Vector2 midaDeCella = default)
        {
            Image fons = Caixa(nom, pare, xMin, yMin, xMax, yMax, new Color(0, 0, 0, 0.15f));
            var scroll = fons.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            Image vista = Caixa("Viewport", fons.transform, 0, 0, 1, 1, Color.white);
            vista.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform contingut = Buit("Content", vista.transform, 0, 1, 1, 1);
            contingut.pivot = new Vector2(0.5f, 1f);
            if (midaDeCella == default)
            {
                var v = contingut.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 8;
                v.padding = new RectOffset(8, 8, 8, 8);
                v.childControlWidth = true;
                v.childControlHeight = false;
                v.childForceExpandHeight = false;
            }
            else
            {
                var g = contingut.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = midaDeCella;
                g.spacing = new Vector2(10, 10);
                g.padding = new RectOffset(10, 10, 10, 10);
                g.childAlignment = TextAnchor.UpperCenter;
            }
            var ajust = contingut.gameObject.AddComponent<ContentSizeFitter>();
            ajust.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)vista.transform;
            scroll.content = contingut;
            return contingut;
        }

        public static void Assignar(Object desti, string camp, Object valor)
        {
            var so = new SerializedObject(desti);
            SerializedProperty p = so.FindProperty(camp);
            if (p == null) { Debug.LogWarning($"EinesDEscenes: {desti.GetType().Name} no té {camp}"); return; }
            p.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Assignar(Object desti, string camp, int valor)
        {
            var so = new SerializedObject(desti);
            SerializedProperty p = so.FindProperty(camp);
            if (p == null) { Debug.LogWarning($"EinesDEscenes: {desti.GetType().Name} no té {camp}"); return; }
            p.intValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AssignarLlista(Object desti, string camp, Object[] valors)
        {
            var so = new SerializedObject(desti);
            SerializedProperty p = so.FindProperty(camp);
            if (p == null) { Debug.LogWarning($"EinesDEscenes: {desti.GetType().Name} no té {camp}"); return; }
            p.arraySize = valors.Length;
            for (int i = 0; i < valors.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valors[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // La gespa del país al fons (just després del fons), sense tapar el que s'indiqui.
        public static GespaDelPrestigi Gespa(Image fons, params RectTransform[] aEvitar)
        {
            var go = new GameObject("Gespa", typeof(RectTransform));
            go.transform.SetParent(fons.transform.parent, false);
            Ancorar((RectTransform)go.transform, 0, 0, 1, 1);
            go.transform.SetSiblingIndex(fons.transform.GetSiblingIndex() + 1);
            var gespa = go.AddComponent<GespaDelPrestigi>();
            Assignar(gespa, "perfilPerDefecte", PerfilDelPrimerPais());
            AssignarLlista(gespa, "aEvitar", aEvitar.Cast<Object>().ToArray());
            return gespa;
        }

        public static PerfilDeDecoracio PerfilDelPrimerPais()
        {
            var sistemes = Resources.Load<GameObject>("Sistemes");
            var g = sistemes != null ? sistemes.GetComponentInChildren<GestorDePaisos>(true) : null;
            if (g == null || g.Paisos == null) return null;
            foreach (PaisSO p in g.Paisos)
                if (p != null && !p.esCiutatComercial) return p.decoracio;
            return null;
        }

        // Afegeix un gestor global a Resources/Sistemes (el seu propi objecte), si no hi és.
        public static void AfegirASistemes<T>() where T : Component
        {
            const string ruta = "Assets/_Project/Resources/Sistemes.prefab";
            if (!System.IO.File.Exists(ruta)) { Debug.LogWarning($"EinesDEscenes: no hi ha {ruta}; posa {typeof(T).Name} a mà"); return; }
            GameObject arrel = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                if (arrel.GetComponentInChildren<T>(true) != null) return;
                var go = new GameObject(typeof(T).Name);
                go.transform.SetParent(arrel.transform, false);
                go.AddComponent<T>();
                PrefabUtility.SaveAsPrefabAsset(arrel, ruta);
                Debug.Log($"EinesDEscenes: {typeof(T).Name} afegit a Resources/Sistemes");
            }
            finally { PrefabUtility.UnloadPrefabContents(arrel); }
        }

        public static void AfegirAlBuild(string ruta)
        {
            var escenes = EditorBuildSettings.scenes.ToList();
            if (escenes.Any(e => e.path == ruta)) return;
            escenes.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenes.ToArray();
        }

        // Un asset, creat si no existeix (amb les carpetes).
        public static T Asset<T>(string ruta, System.Action<T> enCrear = null) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (a != null) return a;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ruta));
            a = ScriptableObject.CreateInstance<T>();
            enCrear?.Invoke(a);
            AssetDatabase.CreateAsset(a, ruta);
            return a;
        }
    }
}
