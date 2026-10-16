using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // Els botons de DecoracioDeGespa: provar llavors, quedar-se la bona.
    [CustomEditor(typeof(DecoracioDeGespa))]
    public class EditorDeDecoracioDeGespa : Editor
    {
        private const string ClauEnViu = "IdleFrogs.DecoracioEnViu";

        public override void OnInspectorGUI()
        {
            var d = (DecoracioDeGespa)target;

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool canviat = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Generar", EditorStyles.boldLabel);

            bool enViu = EditorPrefs.GetBool(ClauEnViu, false);
            bool nouEnViu = EditorGUILayout.ToggleLeft(
                "Regenerar en canviar qualsevol valor (mateixa llavor)", enViu);
            if (nouEnViu != enViu) EditorPrefs.SetBool(ClauEnViu, nouEnViu);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Llavor nova", GUILayout.Height(28)))
                {
                    Undo.RecordObject(d, "Llavor nova");
                    d.llavor = Random.Range(1, int.MaxValue);
                    Generar(d);
                }
                if (GUILayout.Button("Regenerar", GUILayout.Height(28))) Generar(d);
                if (GUILayout.Button("Esborrar", GUILayout.Height(28)))
                {
                    d.Esborrar();
                    MarcarBrut(d);
                }
            }

            if (GUILayout.Button($"Desar la llavor {d.llavor}"))
            {
                Undo.RecordObject(d, "Desar llavor");
                d.llavorsDesades.Add(new LlavorDesada { llavor = d.llavor, nota = string.Empty });
                MarcarBrut(d);
            }

            if (d.llavorsDesades.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Llavors desades", EditorStyles.boldLabel);
                for (int i = 0; i < d.llavorsDesades.Count; i++)
                {
                    LlavorDesada l = d.llavorsDesades[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string nota = string.IsNullOrEmpty(l.nota) ? string.Empty : $" — {l.nota}";
                        EditorGUILayout.LabelField($"{l.llavor}{nota}");
                        if (GUILayout.Button("Fer servir", GUILayout.Width(80)))
                        {
                            Undo.RecordObject(d, "Fer servir llavor");
                            d.llavor = l.llavor;
                            Generar(d);
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox(
                $"{d.Quantitat} elements generats. Es guarden amb l'escena; Ctrl+Z no els desfà, " +
                "però amb la mateixa llavor tornes a tenir el mateix camp.", MessageType.None);

            if (canviat && nouEnViu) Generar(d);
        }

        private static void Generar(DecoracioDeGespa d)
        {
            int posats = d.Generar();
            MarcarBrut(d);
            Debug.Log($"DecoracioDeGespa: llavor {d.llavor}, {posats} elements");
        }

        private static void MarcarBrut(DecoracioDeGespa d)
        {
            EditorUtility.SetDirty(d);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(d.gameObject.scene);
        }
    }
}
