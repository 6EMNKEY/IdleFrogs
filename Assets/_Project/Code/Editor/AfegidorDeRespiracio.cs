using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IdleFrogs.Game;
using IdleFrogs.UI;

namespace IdleFrogs.EditorTools
{
    // La respiració: els ajustos (Resources/Respiracio) i posar el component
    // a totes les siluetes d'encarregat i a totes les granotes, als prefabs i
    // a l'escena oberta.
    public static class AfegidorDeRespiracio
    {
        private const string RutaDelsAjustos = "Assets/_Project/Resources/Respiracio.asset";

        [MenuItem("IdleFrogs/Respiració/Obrir els ajustos")]
        public static void ObrirElsAjustos()
        {
            Selection.activeObject = EinesDEscenes.Asset<RespiracioConfigSO>(RutaDelsAjustos);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("IdleFrogs/Respiració/Afegir als encarregats i a les granotes")]
        public static void Afegir()
        {
            EinesDEscenes.Asset<RespiracioConfigSO>(RutaDelsAjustos);
            AssetDatabase.SaveAssets();
            var informe = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null) continue;
                if (prefab.GetComponentInChildren<SiluetaDEncarregat>(true) == null &&
                    prefab.GetComponentInChildren<ControladorGranota>(true) == null) continue;

                GameObject contingut = PrefabUtility.LoadPrefabContents(ruta);
                try
                {
                    int n = Posar(contingut.GetComponentsInChildren<SiluetaDEncarregat>(true), TipusDeRespiracio.Encarregat, false)
                          + Posar(contingut.GetComponentsInChildren<ControladorGranota>(true), TipusDeRespiracio.Granota, false);
                    if (n > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contingut, ruta);
                        informe.Add($"{System.IO.Path.GetFileNameWithoutExtension(ruta)}: {n}");
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(contingut); }
            }

            Scene escena = SceneManager.GetActiveScene();
            int aLEscena = Posar(DeLEscena<SiluetaDEncarregat>(escena), TipusDeRespiracio.Encarregat, true)
                         + Posar(DeLEscena<ControladorGranota>(escena), TipusDeRespiracio.Granota, true);
            if (aLEscena > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                informe.Add($"{escena.name}: {aLEscena} (desa l'escena)");
            }

            EditorUtility.DisplayDialog("Respiració",
                (informe.Count > 0 ? "Afegida a:\n" + string.Join("\n", informe) : "Ja la tenien tots.") +
                "\n\nEls ajustos són a Resources/Respiracio (IdleFrogs → Respiració → Obrir els ajustos).", "D'acord");
        }

        private static T[] DeLEscena<T>(Scene escena) where T : Component
        {
            var r = new List<T>();
            foreach (T c in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.gameObject.scene == escena) r.Add(c);
            return r.ToArray();
        }

        // Posa (o corregeix el tipus de) la respiració. Torna quants n'ha tocat.
        private static int Posar(Component[] on, TipusDeRespiracio tipus, bool ambUndo)
        {
            int n = 0;
            foreach (Component c in on)
            {
                Respiracio r = c.GetComponent<Respiracio>();
                bool nou = r == null;
                if (nou) r = ambUndo ? Undo.AddComponent<Respiracio>(c.gameObject) : c.gameObject.AddComponent<Respiracio>();
                var so = new SerializedObject(r);
                SerializedProperty p = so.FindProperty("tipus");
                if (!nou && p.enumValueIndex == (int)tipus) continue;
                p.enumValueIndex = (int)tipus;
                so.ApplyModifiedPropertiesWithoutUndo();
                n++;
            }
            return n;
        }
    }
}
