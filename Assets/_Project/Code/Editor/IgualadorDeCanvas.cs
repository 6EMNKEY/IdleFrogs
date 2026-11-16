using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleFrogs.UI;

namespace IdleFrogs.EditorTools
{
    // Posa tots els Canvas de pantalla del joc a la mateixa mida: Scale With
    // Screen Size, 1920×1080, Match 0,5 (el joc és apaïsat). Així un número
    // mesura el mateix a totes les escenes i a tots els mòbils.
    //
    // Els que tenien una altra escala es reescalen per dins (posicions, mides,
    // lletra, vores, separacions) perquè en una pantalla 16:9 es vegin igual
    // que abans. En pantalles més allargades, els que eren verticals (el HUD
    // de l'escena principal) es veuran una mica més petits: és el que toca.
    public static class IgualadorDeCanvas
    {
        public static readonly Vector2 Referencia = new Vector2(1920, 1080);
        public const float Match = 0.5f;

        [MenuItem("IdleFrogs/HUD/Igualar tots els canvas (1920×1080, 0,5)")]
        public static void IgualarTots()
        {
            if (!EditorUtility.DisplayDialog("Igualar els canvas",
                    "Es canviaran i es desaran totes les escenes d'Assets/Scenes i Resources/UIGlobal. Has fet un commit abans?",
                    "Endavant", "Cancel·lar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string oberta = SceneManager.GetActiveScene().path;
            var informe = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
                bool canviat = false;
                foreach (GameObject arrel in escena.GetRootGameObjects())
                    foreach (CanvasScaler s in arrel.GetComponentsInChildren<CanvasScaler>(true))
                        canviat |= Igualar(s, $"{escena.name}/{s.name}", informe);
                if (canviat) EditorSceneManager.SaveScene(escena);
            }

            const string uiGlobal = "Assets/_Project/Resources/UIGlobal.prefab";
            if (System.IO.File.Exists(uiGlobal))
            {
                GameObject contingut = PrefabUtility.LoadPrefabContents(uiGlobal);
                try
                {
                    bool canviat = false;
                    foreach (CanvasScaler s in contingut.GetComponentsInChildren<CanvasScaler>(true))
                        canviat |= Igualar(s, $"UIGlobal/{s.name}", informe);
                    if (canviat) PrefabUtility.SaveAsPrefabAsset(contingut, uiGlobal);
                }
                finally { PrefabUtility.UnloadPrefabContents(contingut); }
            }

            if (!string.IsNullOrEmpty(oberta)) EditorSceneManager.OpenScene(oberta, OpenSceneMode.Single);
            string text = informe.Count > 0 ? string.Join("\n", informe) : "Ja estaven tots iguals.";
            Debug.Log("IgualadorDeCanvas:\n" + text);
            EditorUtility.DisplayDialog("Igualar els canvas", text, "D'acord");
        }

        // Torna si l'ha canviat.
        private static bool Igualar(CanvasScaler s, string nom, List<string> informe)
        {
            Canvas c = s.GetComponent<Canvas>();
            if (c == null || c.renderMode == RenderMode.WorldSpace) return false;
            if (s.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && s.referenceResolution == Referencia &&
                s.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight && Mathf.Approximately(s.matchWidthOrHeight, Match))
                return false;

            // Quant es veia en una pantalla 16:9 de 1920×1080 abans i quant es veurà ara (1).
            float k = Escala(s, Referencia);
            if (Mathf.Abs(k - 1f) > 0.01f) Reescalar(s.transform, k);

            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = Referencia;
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = Match;
            EditorUtility.SetDirty(s);
            informe.Add(Mathf.Abs(k - 1f) > 0.01f ? $"{nom}: contingut ×{k:0.###}" : $"{nom}: només l'escala");
            return true;
        }

        // L'escala que posava un CanvasScaler en una pantalla d'aquesta mida.
        private static float Escala(CanvasScaler s, Vector2 pantalla)
        {
            switch (s.uiScaleMode)
            {
                case CanvasScaler.ScaleMode.ConstantPixelSize:
                    return s.scaleFactor;
                case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                    Vector2 r = s.referenceResolution;
                    switch (s.screenMatchMode)
                    {
                        case CanvasScaler.ScreenMatchMode.Expand: return Mathf.Min(pantalla.x / r.x, pantalla.y / r.y);
                        case CanvasScaler.ScreenMatchMode.Shrink: return Mathf.Max(pantalla.x / r.x, pantalla.y / r.y);
                        default:
                            float w = Mathf.Log(pantalla.x / r.x, 2), h = Mathf.Log(pantalla.y / r.y, 2);
                            return Mathf.Pow(2, Mathf.Lerp(w, h, s.matchWidthOrHeight));
                    }
                default:
                    return 1f;
            }
        }

        // Tot el que es mesura en unitats del Canvas, multiplicat per k.
        public static void Reescalar(Transform arrel, float k)
        {
            foreach (RectTransform r in arrel.GetComponentsInChildren<RectTransform>(true))
            {
                if (r == arrel) continue;
                r.anchoredPosition *= k;
                r.sizeDelta *= k;
                EditorUtility.SetDirty(r);
            }
            foreach (TMP_Text t in arrel.GetComponentsInChildren<TMP_Text>(true))
            {
                t.fontSize *= k;
                t.fontSizeMin *= k;
                t.fontSizeMax *= k;
                t.margin *= k;
                EditorUtility.SetDirty(t);
            }
            foreach (Image i in arrel.GetComponentsInChildren<Image>(true))
            {
                // Les vores d'un sprite tallat es mesuren en unitats del Canvas.
                if (i.type == Image.Type.Sliced || i.type == Image.Type.Tiled)
                {
                    i.pixelsPerUnitMultiplier /= k;
                    EditorUtility.SetDirty(i);
                }
            }
            foreach (HorizontalOrVerticalLayoutGroup g in arrel.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true))
            {
                g.spacing *= k;
                g.padding = Escalar(g.padding, k);
                EditorUtility.SetDirty(g);
            }
            foreach (GridLayoutGroup g in arrel.GetComponentsInChildren<GridLayoutGroup>(true))
            {
                g.cellSize *= k;
                g.spacing *= k;
                g.padding = Escalar(g.padding, k);
                EditorUtility.SetDirty(g);
            }
            foreach (LayoutElement e in arrel.GetComponentsInChildren<LayoutElement>(true))
            {
                if (e.minWidth > 0) e.minWidth *= k;
                if (e.minHeight > 0) e.minHeight *= k;
                if (e.preferredWidth > 0) e.preferredWidth *= k;
                if (e.preferredHeight > 0) e.preferredHeight *= k;
                EditorUtility.SetDirty(e);
            }
            foreach (Shadow o in arrel.GetComponentsInChildren<Shadow>(true))
            {
                o.effectDistance *= k;
                EditorUtility.SetDirty(o);
            }
            // Els efectes que fan servir píxels del Canvas.
            foreach (EfecteDeRecompensa e in arrel.GetComponentsInChildren<EfecteDeRecompensa>(true))
                Multiplicar(e, k, "midaDeLIcona", "radiMinim", "radiMaxim");
            foreach (EfecteDeToc e in arrel.GetComponentsInChildren<EfecteDeToc>(true))
                Multiplicar(e, k, "mida", "distancia");
        }

        private static RectOffset Escalar(RectOffset p, float k) =>
            new RectOffset(Mathf.RoundToInt(p.left * k), Mathf.RoundToInt(p.right * k),
                           Mathf.RoundToInt(p.top * k), Mathf.RoundToInt(p.bottom * k));

        private static void Multiplicar(Object o, float k, params string[] camps)
        {
            var so = new SerializedObject(o);
            foreach (string c in camps)
            {
                SerializedProperty p = so.FindProperty(c);
                if (p == null) continue;
                if (p.propertyType == SerializedPropertyType.Float) p.floatValue *= k;
                else if (p.propertyType == SerializedPropertyType.Vector2) p.vector2Value *= k;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
