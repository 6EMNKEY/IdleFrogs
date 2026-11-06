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
    // Crea Assets/Scenes/Prestigi.unity (com el panell del Figma: a l'esquerra
    // el pergamí, el pantà, la granota i les cinc estrelles; a la dreta la
    // informació i el botó) i Resources/Prestigi.asset si no hi és. Tot són
    // quadrats de colors: hi poses els dibuixos a sobre.
    public static class PreparadorDelPrestigi
    {
        private const string RutaDeLEscena = "Assets/Scenes/Prestigi.unity";
        private const string RutaDeLaConfig = "Assets/_Project/Resources/Prestigi.asset";

        private static readonly Color Verd = new Color(0.55f, 0.77f, 0.17f);
        private static readonly Color Pergami = new Color(0.93f, 0.81f, 0.6f);
        private static readonly Color Aigua = new Color(0.1f, 0.9f, 0.92f);
        private static readonly Color Granota = new Color(0.45f, 0.65f, 0.25f);
        private static readonly Color Groc = new Color(1f, 0.88f, 0.1f);
        private static readonly Color Panell = new Color(0.84f, 0.73f, 0.65f);
        private static readonly Color Vora = new Color(0.33f, 0.05f, 0.05f);
        private static readonly Color Fosc = new Color(0.37f, 0.26f, 0.19f);
        private static readonly Color Vermell = new Color(0.85f, 0.2f, 0.2f);
        private static readonly Color BotoVerd = new Color(0.3f, 0.7f, 0.25f);

        [MenuItem("IdleFrogs/Prestigi/Crear l'escena del prestigi")]
        public static void Crear()
        {
            if (System.IO.File.Exists(RutaDeLEscena) &&
                !EditorUtility.DisplayDialog("Crear l'escena del prestigi", $"{RutaDeLEscena} ja existeix. La vols substituir?", "Substituir", "Cancel·lar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CrearLaConfig();

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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

            var canvasGo = new GameObject("CanvasPrestigi", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalat = canvasGo.AddComponent<CanvasScaler>();
            escalat.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalat.referenceResolution = new Vector2(1920, 1080);
            escalat.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform arrel = canvasGo.transform;

            Image fons = Caixa("Fons", arrel, 0, 0, 1, 1, Verd);

            // --- Esquerra ---------------------------------------------------
            Image pergami = Caixa("Pergami", arrel, 0.07f, 0.80f, 0.46f, 0.93f, Pergami);
            Localitzat(Text("TextPrestigi", pergami.transform, 0.05f, 0.1f, 0.95f, 0.9f, "PRESTIGI", 70, Color.black), "prestigi.titol");

            Image panta = Caixa("Panta", arrel, 0.08f, 0.24f, 0.45f, 0.79f, Aigua);
            panta.preserveAspect = true;
            Image granota = Caixa("Granota", panta.transform, 0.22f, 0.3f, 0.62f, 0.8f, Granota);
            granota.preserveAspect = true;
            Image estrellesGranota = Caixa("EstrellesDeLaGranota", granota.transform, 0, 0, 1, 1, Color.white);
            estrellesGranota.preserveAspect = true;
            estrellesGranota.enabled = false;

            var estrelles = new Image[5];
            var fila = new GameObject("Estrelles", typeof(RectTransform)).transform;
            fila.SetParent(arrel, false);
            Ancorar((RectTransform)fila, 0.08f, 0.07f, 0.45f, 0.22f);
            for (int i = 0; i < estrelles.Length; i++)
            {
                estrelles[i] = Caixa($"Estrella{i + 1}", fila, i / 5f + 0.02f, 0.05f, (i + 1) / 5f - 0.02f, 0.95f, Color.white);
                estrelles[i].preserveAspect = true;
            }

            // --- Dreta --------------------------------------------------------
            Image panell = Caixa("Panell", arrel, 0.51f, 0.07f, 0.93f, 0.88f, Panell);
            panell.gameObject.AddComponent<Outline>().effectColor = Vora;
            Transform p = panell.transform;

            TMP_Text nom = Text("TextNomDelPais", p, 0.05f, 0.87f, 0.85f, 0.97f, "País", 64, Color.black);
            Localitzat(Text("TextMillora", p, 0.05f, 0.79f, 0.95f, 0.87f, "MILLORA DE PRESTIGI", 40, Vora), "prestigi.millora");
            TMP_Text explicacio = Text("TextExplicacio", p, 0.05f, 0.63f, 0.95f, 0.79f, "...", 32, Color.black);

            Image caixaFosca = Caixa("Detalls", p, 0.05f, 0.04f, 0.95f, 0.61f, Fosc);
            caixaFosca.gameObject.AddComponent<Outline>().effectColor = Vora;
            Transform d = caixaFosca.transform;
            TMP_Text ingressos = Fila(d, "Ingressos", "prestigi.ingressos", 0.82f, 0.96f);
            TMP_Text gemes = Fila(d, "Gemes", "prestigi.gemes", 0.68f, 0.82f);
            TMP_Text dificultat = Fila(d, "Dificultat", "prestigi.dificultat", 0.54f, 0.68f);
            TMP_Text conserves = Fila(d, "Conserves", "prestigi.conserves", 0.40f, 0.54f);
            Localitzat(conserves, "prestigi.conserva");
            TMP_Text tens = Text("TextTens", d, 0.05f, 0.25f, 0.95f, 0.38f, "", 34, Color.white);

            Image boto = Caixa("BotoPrestigi", d, 0.15f, 0.04f, 0.85f, 0.23f, BotoVerd);
            Button botoPrestigi = boto.gameObject.AddComponent<Button>();
            TMP_Text textBoto = Text("Text", boto.transform, 0.05f, 0.05f, 0.95f, 0.95f, "Prestigi", 44, Color.white);

            Image tancar = Caixa("BotoTancar", arrel, 0.905f, 0.83f, 0.955f, 0.92f, Vermell);
            Button botoTancar = tancar.gameObject.AddComponent<Button>();
            Text("X", tancar.transform, 0, 0, 1, 1, "X", 60, Color.white);

            Button anterior = Fletxa("BotoAnterior", arrel, 0.005f, 0.045f, "<");
            Button seguent = Fletxa("BotoSeguent", arrel, 0.955f, 0.995f, ">");

            // --- El gestor ----------------------------------------------------
            var gestor = new GameObject("GestorDelPrestigi").AddComponent<GestorDelPrestigi>();
            var so = new SerializedObject(gestor);
            so.FindProperty("fons").objectReferenceValue = fons;
            so.FindProperty("dibuixDelPanta").objectReferenceValue = panta;
            so.FindProperty("dibuixDeLaGranota").objectReferenceValue = granota;
            so.FindProperty("estrellesDeLaGranota").objectReferenceValue = estrellesGranota;
            SerializedProperty llista = so.FindProperty("estrelles");
            llista.arraySize = estrelles.Length;
            for (int i = 0; i < estrelles.Length; i++) llista.GetArrayElementAtIndex(i).objectReferenceValue = estrelles[i];
            // Sense els sprites de les estrelles, els quadrats de mostra: encesa groga.
            so.FindProperty("colorEncesa").colorValue = Groc;
            so.FindProperty("textNomDelPais").objectReferenceValue = nom;
            so.FindProperty("textExplicacio").objectReferenceValue = explicacio;
            so.FindProperty("textIngressos").objectReferenceValue = ingressos;
            so.FindProperty("textGemes").objectReferenceValue = gemes;
            so.FindProperty("textDificultat").objectReferenceValue = dificultat;
            so.FindProperty("textTens").objectReferenceValue = tens;
            so.FindProperty("botoPrestigi").objectReferenceValue = botoPrestigi;
            so.FindProperty("textDelBoto").objectReferenceValue = textBoto;
            so.FindProperty("botoTancar").objectReferenceValue = botoTancar;
            so.FindProperty("botoAnterior").objectReferenceValue = anterior;
            so.FindProperty("botoSeguent").objectReferenceValue = seguent;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RutaDeLEscena));
            AfegirLaGespa(gestor);
            EditorSceneManager.SaveScene(escena, RutaDeLEscena);
            AfegirAlBuild(RutaDeLEscena);
            Debug.Log($"PreparadorDelPrestigi: {RutaDeLEscena} creada. Posa els dibuixos a Resources/Prestigi i a les imatges.");
        }

        private static void CrearLaConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PrestigiConfigSO>(RutaDeLaConfig);
            if (config == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RutaDeLaConfig));
                config = ScriptableObject.CreateInstance<PrestigiConfigSO>();
                AssetDatabase.CreateAsset(config, RutaDeLaConfig);
                Debug.Log($"PreparadorDelPrestigi: {RutaDeLaConfig} creat amb els valors per defecte");
            }
            if (config.aspectePerDefecte == null)
            {
                config.aspectePerDefecte = AspecteDelPrefabDelPanta();
                EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
        }

        // El que porta el prefab del pantà: el que fan servir els països sense aspecte propi.
        private static AspectePantaSO AspecteDelPrefabDelPanta()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab Panta"))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                Panta panta = go != null ? go.GetComponentInChildren<Panta>(true) : null;
                if (panta == null) continue;
                var a = new SerializedObject(panta).FindProperty("aspecte").objectReferenceValue as AspectePantaSO;
                if (a != null) return a;
            }
            return null;
        }

        // A l'escena del prestigi ja creada: la gespa del país al fons.
        [MenuItem("IdleFrogs/Prestigi/Afegir la gespa a l'escena oberta")]
        public static void AfegirLaGespaALEscenaOberta()
        {
            var gestor = Object.FindFirstObjectByType<GestorDelPrestigi>(FindObjectsInactive.Include);
            if (gestor == null)
            {
                EditorUtility.DisplayDialog("Gespa", "Obre l'escena Prestigi primer (no hi ha cap GestorDelPrestigi).", "D'acord");
                return;
            }
            AfegirLaGespa(gestor);
            EditorSceneManager.MarkSceneDirty(gestor.gameObject.scene);
        }

        // Just després del Fons (darrere de tot el que hi ha), evitant el pantà.
        private static void AfegirLaGespa(GestorDelPrestigi gestor)
        {
            var so = new SerializedObject(gestor);
            var fons = so.FindProperty("fons").objectReferenceValue as Image;
            var panta = so.FindProperty("dibuixDelPanta").objectReferenceValue as Image;
            if (fons == null) { Debug.LogWarning("PreparadorDelPrestigi: el GestorDelPrestigi no té Fons; la gespa no se sap on posar"); return; }

            var gespa = so.FindProperty("gespa").objectReferenceValue as GespaDelPrestigi;
            if (gespa == null)
            {
                var go = new GameObject("Gespa", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Gespa");
                go.transform.SetParent(fons.transform.parent, false);
                Ancorar((RectTransform)go.transform, 0, 0, 1, 1);
                gespa = go.AddComponent<GespaDelPrestigi>();
            }
            gespa.transform.SetSiblingIndex(fons.transform.GetSiblingIndex() + 1);

            var sg = new SerializedObject(gespa);
            if (sg.FindProperty("perfilPerDefecte").objectReferenceValue == null)
                sg.FindProperty("perfilPerDefecte").objectReferenceValue = PerfilDelPrimerPais();
            SerializedProperty evitar = sg.FindProperty("aEvitar");
            if (panta != null && evitar.arraySize == 0)
            {
                evitar.arraySize = 1;
                evitar.GetArrayElementAtIndex(0).objectReferenceValue = panta.rectTransform;
            }
            sg.ApplyModifiedPropertiesWithoutUndo();

            so.FindProperty("gespa").objectReferenceValue = gespa;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("PreparadorDelPrestigi: gespa afegida al fons");
        }

        // La del primer país del GestorDePaisos (a Resources/Sistemes).
        private static PerfilDeDecoracio PerfilDelPrimerPais()
        {
            var sistemes = Resources.Load<GameObject>("Sistemes");
            var g = sistemes != null ? sistemes.GetComponentInChildren<GestorDePaisos>(true) : null;
            if (g == null || g.Paisos == null) return null;
            foreach (PaisSO p in g.Paisos)
                if (p != null && !p.esCiutatComercial) return p.decoracio;
            return null;
        }

        [MenuItem("IdleFrogs/Prestigi/Omplir l'aspecte per defecte")]
        public static void OmplirLAspecte()
        {
            CrearLaConfig();
            var config = AssetDatabase.LoadAssetAtPath<PrestigiConfigSO>(RutaDeLaConfig);
            Debug.Log($"PreparadorDelPrestigi: aspecte per defecte = {(config.aspectePerDefecte != null ? config.aspectePerDefecte.name : "cap (posa'l a mà)")}");
        }

        // Una etiqueta a l'esquerra (traduïda) i el valor a la dreta, que posa el gestor.
        private static TMP_Text Fila(Transform pare, string nom, string clau, float yMin, float yMax)
        {
            Localitzat(Text($"Etiqueta{nom}", pare, 0.05f, yMin, 0.5f, yMax, nom, 36, Color.white, TextAlignmentOptions.Left), clau);
            return Text($"Text{nom}", pare, 0.5f, yMin, 0.95f, yMax, "", 36, Color.white, TextAlignmentOptions.Right);
        }

        private static Button Fletxa(string nom, Transform pare, float xMin, float xMax, string signe)
        {
            Image i = Caixa(nom, pare, xMin, 0.42f, xMax, 0.58f, Pergami);
            Text("Text", i.transform, 0, 0, 1, 1, signe, 70, Color.black);
            return i.gameObject.AddComponent<Button>();
        }

        // Tot amb àncores (proporcions del pare) i marges a zero: s'adapta sol a la pantalla.
        private static Image Caixa(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax, Color color)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(pare, false);
            Ancorar((RectTransform)go.transform, xMin, yMin, xMax, yMax);
            Image i = go.AddComponent<Image>();
            i.color = color;
            return i;
        }

        private static void Ancorar(RectTransform r, float xMin, float yMin, float xMax, float yMax)
        {
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static TMP_Text Text(string nom, Transform pare, float xMin, float yMin, float xMax, float yMax, string text,
                                     float mida, Color color, TextAlignmentOptions alineacio = TextAlignmentOptions.Center)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(pare, false);
            Ancorar((RectTransform)go.transform, xMin, yMin, xMax, yMax);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.color = color;
            t.alignment = alineacio;
            t.enableAutoSizing = true;
            t.fontSizeMin = 14;
            t.fontSizeMax = mida;
            t.raycastTarget = false;
            return t;
        }

        private static TMP_Text Localitzat(TMP_Text t, string clau)
        {
            var loc = t.gameObject.AddComponent<TextLocalitzat>();
            var so = new SerializedObject(loc);
            so.FindProperty("clau").stringValue = clau;
            so.ApplyModifiedPropertiesWithoutUndo();
            return t;
        }

        private static void AfegirAlBuild(string ruta)
        {
            var escenes = EditorBuildSettings.scenes.ToList();
            if (escenes.Any(e => e.path == ruta)) return;
            escenes.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenes.ToArray();
        }
    }
}
