using System.Collections.Generic;
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
    // Muntatge del mapa com a escena pròpia. Dos menús, en ordre:
    //
    // 1. Amb l'escena del país oberta: hi afegeix EscenaDelPais, treu els
    //    sistemes globals de l'escena i en fa Resources/Sistemes i
    //    Resources/UIGlobal (els instancia Arrencada en arrencar), posa
    //    BotoDePanellGlobal als botons del HUD i treu el panell de mapa vell.
    // 2. Crea Assets/Scenes/Mapa.unity amb la càmera, els països, els llocs,
    //    el HUD i el GestorDelMapa, i el posa a la llista d'escenes del build.
    //
    // Fes un commit abans: el pas 1 mou objectes de l'escena.
    public static class PreparadorDelMapa
    {
        private const string Recursos = "Assets/_Project/Resources";
        private const string RutaDelMapa = "Assets/Scenes/Mapa.unity";

        // ---------------------------------------------------------------
        // 1. L'escena del país
        // ---------------------------------------------------------------

        [MenuItem("IdleFrogs/Mapa/1. Preparar l'escena del país")]
        public static void PrepararElPais()
        {
            Scene escena = SceneManager.GetActiveScene();
            if (!EditorUtility.DisplayDialog("Preparar l'escena del país",
                    $"Es modificarà {escena.name}: es treuen els sistemes globals cap a Resources/Sistemes i " +
                    "la confirmació i la botiga cap a Resources/UIGlobal. Has fet un commit abans?", "Endavant", "Cancel·lar"))
                return;

            System.IO.Directory.CreateDirectory(Recursos);
            var informe = new List<string>();

            // EscenaDelPais amb les seves peces.
            var pais = Object.FindFirstObjectByType<EscenaDelPais>(FindObjectsInactive.Include);
            if (pais == null)
            {
                var go = new GameObject("EscenaDelPais");
                Undo.RegisterCreatedObjectUndo(go, "EscenaDelPais");
                pais = go.AddComponent<EscenaDelPais>();
            }
            var so = new SerializedObject(pais);
            so.FindProperty("llistaDePantans").objectReferenceValue = Object.FindFirstObjectByType<LlistaDePantans>(FindObjectsInactive.Include);
            so.FindProperty("tren").objectReferenceValue = Object.FindFirstObjectByType<ControladorTren>(FindObjectsInactive.Include);
            so.FindProperty("estacio").objectReferenceValue = Object.FindFirstObjectByType<Estacio>(FindObjectsInactive.Include);
            so.FindProperty("tenda").objectReferenceValue = Object.FindFirstObjectByType<Tenda>(FindObjectsInactive.Include);
            so.ApplyModifiedPropertiesWithoutUndo();
            informe.Add("EscenaDelPais connectat");

            // Els botons del HUD que obrien panells que ara són globals.
            BotoGlobal(Object.FindFirstObjectByType<PanellDeBotiga>(FindObjectsInactive.Include), "botoObrir", AccioDeBoto.Botiga, informe);
            BotoGlobal(Object.FindFirstObjectByType<PanellDeConfiguracio>(FindObjectsInactive.Include), "botoObrir", AccioDeBoto.Configuracio, informe);
            GameObject botoMapa = BuscarPerNom("BotoMapa");
            if (botoMapa != null && botoMapa.GetComponent<Button>() != null && botoMapa.GetComponent<BotoDePanellGlobal>() == null)
            {
                var b = Undo.AddComponent<BotoDePanellGlobal>(botoMapa);
                var sb = new SerializedObject(b);
                sb.FindProperty("accio").enumValueIndex = (int)AccioDeBoto.Mapa;
                sb.ApplyModifiedPropertiesWithoutUndo();
                informe.Add("BotoMapa → obre l'escena del mapa");
            }

            // El panell de mapa vell (el seu script ja no existeix).
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go != null && go.name == "PanellDeMapa" && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                {
                    Undo.DestroyObjectImmediate(go);
                    informe.Add("Panell de mapa vell esborrat");
                    break;
                }

            // Sistemes: els gestors globals, cadascun amb el seu objecte.
            var sistemes = new GameObject("Sistemes");
            foreach (Component c in new Component[]
            {
                Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<CurrencyManager>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<GestorDeBotiga>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<GestorDePaisos>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<GestorDAudio>(FindObjectsInactive.Include),
            })
            {
                if (c == null) continue;
                c.transform.SetParent(sistemes.transform, false);
                informe.Add($"{c.GetType().Name} → Sistemes");
            }
            GuardarPrefab(sistemes, "Sistemes", informe);

            // UIGlobal: la confirmació (amb el seu Canvas) i la botiga (en un
            // Canvas nou com el de les finestres, per sota de la confirmació).
            var ui = new GameObject("UIGlobal");
            var confirmacio = Object.FindFirstObjectByType<FinestraDeConfirmacio>(FindObjectsInactive.Include);
            Canvas canvasDeConfirmacio = confirmacio != null ? confirmacio.GetComponentInParent<Canvas>(true) : null;
            if (canvasDeConfirmacio != null)
            {
                canvasDeConfirmacio.rootCanvas.transform.SetParent(ui.transform, false);
                informe.Add("Canvas de la confirmació → UIGlobal");
            }
            MoureAUnCanvasNou<PanellDeBotiga>(ui, "CanvasBotiga", 40, informe);
            MoureAUnCanvasNou<PanellDInformacioDeProducte>(ui, "CanvasInformacio", 45, informe);
            MoureAUnCanvasNou<PanellDeConfiguracio>(ui, "CanvasConfiguracio", 42, informe);
            MoureAUnCanvasNou<EfecteDeToc>(ui, "CanvasEspurnes", 110, informe);
            GuardarPrefab(ui, "UIGlobal", informe);

            AfegirAlBuild(escena.path, 0);
            EditorSceneManager.MarkSceneDirty(escena);
            Debug.Log("PreparadorDelMapa:\n- " + string.Join("\n- ", informe));
            EditorUtility.DisplayDialog("Fet", string.Join("\n", informe) + "\n\nDesa l'escena.", "D'acord");
        }

        private static void BotoGlobal(MonoBehaviour panell, string camp, AccioDeBoto accio, List<string> informe)
        {
            if (panell == null) return;
            var so = new SerializedObject(panell);
            SerializedProperty p = so.FindProperty(camp);
            var boto = p != null ? p.objectReferenceValue as Button : null;
            if (boto == null) return;
            if (boto.GetComponent<BotoDePanellGlobal>() == null)
            {
                var b = Undo.AddComponent<BotoDePanellGlobal>(boto.gameObject);
                var sb = new SerializedObject(b);
                sb.FindProperty("accio").enumValueIndex = (int)accio;
                sb.ApplyModifiedPropertiesWithoutUndo();
            }
            // El panell serà al prefab i no pot apuntar a l'escena.
            p.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            informe.Add($"{boto.name} → {accio} (BotoDePanellGlobal)");
        }

        // Treu el panell del seu Canvas i el posa en un de nou amb la mateixa
        // configuració (escalat, raycaster) i l'ordre que li toca.
        private static void MoureAUnCanvasNou<T>(GameObject ui, string nom, int ordre, List<string> informe) where T : MonoBehaviour
        {
            T panell = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (panell == null) return;
            Canvas original = panell.GetComponentInParent<Canvas>(true);
            if (original == null) return;
            original = original.rootCanvas;
            if (original.transform.IsChildOf(ui.transform)) return;

            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(ui.transform, false);
            Canvas c = go.AddComponent<Canvas>();
            EditorUtility.CopySerialized(original, c);
            c.overrideSorting = false;
            c.sortingOrder = ordre;
            CanvasScaler escalat = original.GetComponent<CanvasScaler>();
            if (escalat != null) EditorUtility.CopySerialized(escalat, go.AddComponent<CanvasScaler>());
            go.AddComponent<GraphicRaycaster>();

            panell.transform.SetParent(go.transform, false);
            informe.Add($"{typeof(T).Name} → UIGlobal/{nom} (ordre {ordre})");
        }

        private static void GuardarPrefab(GameObject arrel, string nom, List<string> informe)
        {
            string ruta = $"{Recursos}/{nom}.prefab";
            if (arrel.transform.childCount == 0)
            {
                Object.DestroyImmediate(arrel);
                informe.Add($"{nom}: no hi havia res per moure");
                return;
            }
            AvisarDeReferenciesALEscena(arrel, nom, informe);
            PrefabUtility.SaveAsPrefabAsset(arrel, ruta, out bool fet);
            if (!fet) { informe.Add($"{nom}: NO s'ha pogut desar {ruta}"); return; }
            // A l'escena no hi han de ser: els crea Arrencada en arrencar.
            Object.DestroyImmediate(arrel);
            informe.Add($"{ruta} creat");
        }

        // Un prefab no pot apuntar a objectes de l'escena: aquestes referències
        // es perdran i s'hauran de refer (o buscar-les per codi).
        private static void AvisarDeReferenciesALEscena(GameObject arrel, string nom, List<string> informe)
        {
            foreach (Component c in arrel.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                SerializedProperty p = new SerializedObject(c).GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    GameObject desti = p.objectReferenceValue is Component d ? d.gameObject : p.objectReferenceValue as GameObject;
                    if (desti == null || !desti.scene.IsValid() || desti.transform.IsChildOf(arrel.transform)) continue;
                    informe.Add($"ATENCIÓ {nom}: {c.gameObject.name}.{c.GetType().Name}.{p.propertyPath} apuntava a {desti.name} (de l'escena) i es perd");
                }
            }
        }

        private static GameObject BuscarPerNom(string nom) =>
            Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(g => g.name == nom);

        // ---------------------------------------------------------------
        // 2. L'escena del mapa
        // ---------------------------------------------------------------

        [MenuItem("IdleFrogs/Mapa/2. Crear l'escena del mapa")]
        public static void CrearElMapa()
        {
            if (System.IO.File.Exists(RutaDelMapa) &&
                !EditorUtility.DisplayDialog("Crear l'escena del mapa", $"{RutaDelMapa} ja existeix. La vols substituir?", "Substituir", "Cancel·lar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Fons del mapa: el dibuix que ja hi havia, si hi és.
            var fons = new GameObject("FonsDelMapa");
            var srFons = fons.AddComponent<SpriteRenderer>();
            srFons.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Sprites/Mapa/mapa.png");
            srFons.sortingOrder = -100;

            // Càmera amb arrossegament, zoom i tocs.
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            Camera camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.36f, 0.27f, 0.2f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.AddComponent<AudioListener>();
            var control = cam.AddComponent<ControladorDeCameraDelMapa>();
            Assignar(control, "fonsDelMapa", srFons);
            var detector = cam.AddComponent<DetectorDeTocs>();
            Assignar(detector, "cameraDelMon", camera);
            Assignar(detector, "controladorDeCamera", control);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            // Els països, en fila: col·loca'ls sobre el dibuix.
            var arrelDePaisos = new GameObject("Paisos");
            List<PaisSO> paisos = Paisos();
            for (int i = 0; i < paisos.Count; i++)
                CrearPais(paisos[i], i, arrelDePaisos.transform, paisos.Count);

            // Els llocs especials.
            var llocs = new GameObject("Llocs");
            CrearLloc(AccioDelMapa.Botiga, new Vector3(-6, -5, 0), llocs.transform);
            CrearLloc(AccioDelMapa.Missions, new Vector3(-2, -5, 0), llocs.transform);
            CrearLloc(AccioDelMapa.Esdeveniments, new Vector3(2, -5, 0), llocs.transform);
            CrearLloc(AccioDelMapa.Prestigi, new Vector3(6, -5, 0), llocs.transform);

            // HUD: tornar, gemes i missatge.
            var canvasGo = new GameObject("CanvasHUD", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var escalat = canvasGo.AddComponent<CanvasScaler>();
            escalat.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalat.referenceResolution = new Vector2(1920, 1080);
            escalat.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Button tornar = BotoUI("BotoTornar", canvasGo.transform, new Vector2(0, 1), new Vector2(140, -70), new Vector2(220, 90), "mapa.tornar");
            TMP_Text gemes = TextUI("TextGemes", canvasGo.transform, new Vector2(1, 1), new Vector2(-180, -60), new Vector2(300, 70), "0");
            gemes.gameObject.AddComponent<TextDeGemes>();
            TMP_Text missatge = TextUI("TextMissatge", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(1200, 90), string.Empty);

            var gestor = new GameObject("GestorDelMapa").AddComponent<GestorDelMapa>();
            Assignar(gestor, "cameraDelMapa", control);
            Assignar(gestor, "botoTornar", tornar);
            Assignar(gestor, "textMissatge", missatge);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RutaDelMapa));
            EditorSceneManager.SaveScene(escena, RutaDelMapa);
            AfegirAlBuild(RutaDelMapa, -1);
            Debug.Log($"PreparadorDelMapa: {RutaDelMapa} creada amb {paisos.Count} països i 4 llocs. Posa'ls sobre el dibuix.");
        }

        private static List<PaisSO> Paisos()
        {
            // L'ordre del GestorDePaisos (dins de Resources/Sistemes, si ja hi és).
            var sistemes = Resources.Load<GameObject>("Sistemes");
            var gestor = sistemes != null ? sistemes.GetComponentInChildren<GestorDePaisos>(true) : null;
            if (gestor != null && gestor.Paisos != null) return gestor.Paisos.Where(p => p != null).ToList();
            return AssetDatabase.FindAssets("t:PaisSO")
                .Select(g => AssetDatabase.LoadAssetAtPath<PaisSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null).OrderBy(p => p.name).ToList();
        }

        private static void CrearPais(PaisSO pais, int i, Transform pare, int total)
        {
            var go = new GameObject(pais.name);
            go.transform.SetParent(pare, false);
            go.transform.position = new Vector3((i - (total - 1) / 2f) * 4f, 2f, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            // Els dibuixos pais1, pais2... del mapa, si n'hi ha per a aquest.
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Sprites/Mapa/pais{i + 1}.png");
            sr.sortingOrder = 0;
            if (sr.sprite != null) go.AddComponent<PolygonCollider2D>();
            else go.AddComponent<CircleCollider2D>().radius = 1.2f;

            var p = go.AddComponent<PaisDelMapa>();
            Assignar(p, "pais", pais);
            Assignar(p, "dibuix", sr);
            Assignar(p, "textNom", TextMon("Nom", go.transform, new Vector3(0, 1.6f, 0), 4f, pais.name));
            Assignar(p, "textPreu", TextMon("Preu", go.transform, new Vector3(0, -0.2f, 0), 3.2f, "1M"));

            // El cartellet de guanys, amb el seu botó de recollir.
            var guanys = new GameObject("Guanys");
            guanys.transform.SetParent(go.transform, false);
            guanys.transform.localPosition = new Vector3(0, -1.5f, 0);
            var srG = guanys.AddComponent<SpriteRenderer>();
            srG.sortingOrder = 20;
            guanys.AddComponent<BoxCollider2D>().size = new Vector2(2.4f, 0.8f);
            guanys.AddComponent<BotoRecollirDelMapa>();
            Assignar(p, "arrelDeGuanys", guanys);
            Assignar(p, "textGuanys", TextMon("TextGuanys", guanys.transform, Vector3.zero, 3f, "+0"));
        }

        private static void CrearLloc(AccioDelMapa accio, Vector3 posicio, Transform pare)
        {
            var go = new GameObject(accio.ToString());
            go.transform.SetParent(pare, false);
            go.transform.position = posicio;
            go.AddComponent<SpriteRenderer>().sortingOrder = 5;
            go.AddComponent<BoxCollider2D>().size = new Vector2(2.5f, 2f);
            var punt = go.AddComponent<PuntDelMapa>();
            var so = new SerializedObject(punt);
            so.FindProperty("accio").enumValueIndex = (int)accio;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assignar(punt, "textNom", TextMon("Nom", go.transform, new Vector3(0, -1.3f, 0), 3.2f, accio.ToString()));
        }

        // Un text del món (TextMeshPro, no UGUI), per sobre del dibuix.
        private static TMP_Text TextMon(string nom, Transform pare, Vector3 local, float mida, string text)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(pare, false);
            go.transform.localPosition = local;
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = mida;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(6, 1.5f);
            t.sortingOrder = 30;
            return t;
        }

        private static TMP_Text TextUI(string nom, Transform pare, Vector2 ancora, Vector2 pos, Vector2 mida, string text)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(pare, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = ancora;
            r.anchoredPosition = pos;
            r.sizeDelta = mida;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = 48;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private static Button BotoUI(string nom, Transform pare, Vector2 ancora, Vector2 pos, Vector2 mida, string clau)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(pare, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = ancora;
            r.anchoredPosition = pos;
            r.sizeDelta = mida;
            go.AddComponent<Image>().color = new Color(0.55f, 0.35f, 0.2f);
            Button b = go.AddComponent<Button>();
            TMP_Text t = TextUI("Text", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, mida, clau);
            var loc = t.gameObject.AddComponent<TextLocalitzat>();
            Assignar(loc, "clau", clau);
            return b;
        }

        private static void Assignar(Object desti, string camp, Object valor)
        {
            var so = new SerializedObject(desti);
            SerializedProperty p = so.FindProperty(camp);
            if (p == null) { Debug.LogWarning($"PreparadorDelMapa: {desti.GetType().Name} no té {camp}"); return; }
            p.objectReferenceValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assignar(Object desti, string camp, string valor)
        {
            var so = new SerializedObject(desti);
            SerializedProperty p = so.FindProperty(camp);
            if (p == null) { Debug.LogWarning($"PreparadorDelMapa: {desti.GetType().Name} no té {camp}"); return; }
            p.stringValue = valor;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // posicio -1 = al final; 0 = la primera (la que s'obre en arrencar).
        private static void AfegirAlBuild(string ruta, int posicio)
        {
            var escenes = EditorBuildSettings.scenes.ToList();
            escenes.RemoveAll(e => e.path == ruta);
            var nova = new EditorBuildSettingsScene(ruta, true);
            if (posicio < 0 || posicio > escenes.Count) escenes.Add(nova);
            else escenes.Insert(posicio, nova);
            EditorBuildSettings.scenes = escenes.ToArray();
        }
    }
}
