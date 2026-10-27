using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // L'inspector de la decoració: les capes del perfil, editables aquí però
    // desades al seu asset (Ctrl+S i Ctrl+Z hi funcionen), una llavor per
    // capa, i el botó de fornejar: desar el que es veu (retocat a mà o no)
    // com el camp del país.
    //
    // El camp generat a l'editor és una previsualització: no es desa amb
    // l'escena. El joc el genera en carregar el país.
    [CustomEditor(typeof(DecoracioDeGespa))]
    public class EditorDeDecoracioDeGespa : Editor
    {
        private const string ClauEnViu = "IdleFrogs.DecoracioEnViu";
        private const string CarpetaDePerfils = "Assets/_Project/Data/Decoracio";

        public override void OnInspectorGUI()
        {
            var d = (DecoracioDeGespa)target;
            DrawDefaultInspector();

            PerfilDeDecoracio perfil = d.perfil;
            EditorGUILayout.Space(8);

            if (perfil == null)
            {
                EditorGUILayout.HelpBox("Arrossega el perfil del país que vols previsualitzar, o crea'n un.", MessageType.Info);
                if (GUILayout.Button("Crear un perfil nou", GUILayout.Height(26))) CrearPerfil(d);
                return;
            }

            bool enViu = EditorPrefs.GetBool(ClauEnViu, false);
            bool nouEnViu = EditorGUILayout.ToggleLeft("Regenerar en canviar qualsevol valor de les capes", enViu);
            if (nouEnViu != enViu) EditorPrefs.SetBool(ClauEnViu, nouEnViu);

            bool canviat = DibuixarCapes(d, perfil);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Previsualitzar", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generar (llavors)", GUILayout.Height(28))) Generar(d);
                if (GUILayout.Button("Esborrar", GUILayout.Height(28))) d.Esborrar();
            }

            Fornejat(d, perfil);

            EditorGUILayout.HelpBox(
                $"{d.Quantitat} elements a la previsualització. No es desa amb l'escena: en joc es genera " +
                "el perfil del país. Per retocar a mà, mou-los i fes Fornejar.", MessageType.None);

            if (canviat && nouEnViu) Generar(d);
        }

        // ---------------------------------------------------------------
        // Capes
        // ---------------------------------------------------------------

        private static bool DibuixarCapes(DecoracioDeGespa d, PerfilDeDecoracio perfil)
        {
            EditorGUILayout.LabelField($"Capes de {perfil.name}", EditorStyles.boldLabel);

            var so = new SerializedObject(perfil);
            so.Update();
            SerializedProperty capes = so.FindProperty(nameof(PerfilDeDecoracio.capes));
            bool canviat = false;
            int aTreure = -1;

            for (int i = 0; i < capes.arraySize; i++)
            {
                SerializedProperty capa = capes.GetArrayElementAtIndex(i);
                string nom = capa.FindPropertyRelative(nameof(CapaDeDecoracio.nom)).stringValue;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    capa.isExpanded = EditorGUILayout.Foldout(capa.isExpanded, string.IsNullOrEmpty(nom) ? $"Capa {i + 1}" : nom, true);
                    if (!capa.isExpanded) continue;

                    EditorGUI.BeginChangeCheck();
                    Camp(capa, nameof(CapaDeDecoracio.nom));
                    Camp(capa, nameof(CapaDeDecoracio.variants));
                    Camp(capa, nameof(CapaDeDecoracio.llavor));
                    Camp(capa, nameof(CapaDeDecoracio.densitat));
                    Camp(capa, nameof(CapaDeDecoracio.distanciaMinima));
                    Camp(capa, nameof(CapaDeDecoracio.nomsAEvitar));
                    Camp(capa, nameof(CapaDeDecoracio.marge));
                    Camp(capa, nameof(CapaDeDecoracio.capaDeDibuix));
                    Camp(capa, nameof(CapaDeDecoracio.ordreDeDibuix));
                    canviat |= EditorGUI.EndChangeCheck();

                    SerializedProperty llavor = capa.FindPropertyRelative(nameof(CapaDeDecoracio.llavor));
                    SerializedProperty desades = capa.FindPropertyRelative(nameof(CapaDeDecoracio.llavorsDesades));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Llavor nova"))
                        {
                            llavor.intValue = Random.Range(1, int.MaxValue);
                            canviat = true;
                            so.ApplyModifiedProperties();
                            Generar(d);
                        }
                        if (GUILayout.Button($"Desar la llavor {llavor.intValue}"))
                        {
                            int n = desades.arraySize;
                            desades.InsertArrayElementAtIndex(n);
                            SerializedProperty nova = desades.GetArrayElementAtIndex(n);
                            nova.FindPropertyRelative(nameof(LlavorDesada.llavor)).intValue = llavor.intValue;
                            nova.FindPropertyRelative(nameof(LlavorDesada.nota)).stringValue = string.Empty;
                        }
                        if (GUILayout.Button("Treure la capa", GUILayout.Width(110))) aTreure = i;
                    }

                    for (int k = 0; k < desades.arraySize; k++)
                    {
                        SerializedProperty l = desades.GetArrayElementAtIndex(k);
                        int valor = l.FindPropertyRelative(nameof(LlavorDesada.llavor)).intValue;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(l.FindPropertyRelative(nameof(LlavorDesada.nota)), new GUIContent(valor.ToString()));
                            if (GUILayout.Button("Fer servir", GUILayout.Width(80)))
                            {
                                llavor.intValue = valor;
                                so.ApplyModifiedProperties();
                                Generar(d);
                            }
                        }
                    }
                }
            }

            if (aTreure >= 0)
            {
                capes.DeleteArrayElementAtIndex(aTreure);
                canviat = true;
            }
            if (GUILayout.Button("+ Afegir una capa"))
            {
                int n = capes.arraySize;
                capes.InsertArrayElementAtIndex(n);
                SerializedProperty nova = capes.GetArrayElementAtIndex(n);
                nova.FindPropertyRelative(nameof(CapaDeDecoracio.nom)).stringValue = $"Capa {n + 1}";
                nova.FindPropertyRelative(nameof(CapaDeDecoracio.llavor)).intValue = Random.Range(1, int.MaxValue);
                nova.FindPropertyRelative(nameof(CapaDeDecoracio.variants)).arraySize = 0;
                nova.FindPropertyRelative(nameof(CapaDeDecoracio.llavorsDesades)).arraySize = 0;
                nova.isExpanded = true;
            }

            so.ApplyModifiedProperties();
            return canviat;
        }

        private static void Camp(SerializedProperty capa, string nom) =>
            EditorGUILayout.PropertyField(capa.FindPropertyRelative(nom), true);

        // ---------------------------------------------------------------
        // Fornejar
        // ---------------------------------------------------------------

        private static void Fornejat(DecoracioDeGespa d, PerfilDeDecoracio perfil)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Fornejar (retocs a mà)", EditorStyles.boldLabel);

            if (perfil.campFornejat != null)
            {
                EditorGUILayout.HelpBox(
                    $"{perfil.name} fa servir el camp fornejat {perfil.campFornejat.name}: el joc el posa tal qual. " +
                    "Si canvies capes o llavors, torna a fornejar o treu-lo.", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Mostrar el camp fornejat")) d.Posar(perfil);
                    if (GUILayout.Button("Treure el camp fornejat"))
                    {
                        Undo.RecordObject(perfil, "Treure el camp fornejat");
                        perfil.campFornejat = null;
                        EditorUtility.SetDirty(perfil);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(d.Contenidor == null))
            {
                if (GUILayout.Button("Fornejar: desar el que es veu com el camp d'aquest perfil", GUILayout.Height(26)))
                    Fornejar(d, perfil);
            }
        }

        // Una còpia del camp que es veu, sense la marca de previsualització
        // (un objecte que no es desa no pot anar dins d'un prefab), desada com
        // a prefab al costat del perfil.
        private static void Fornejar(DecoracioDeGespa d, PerfilDeDecoracio perfil)
        {
            Transform camp = d.Contenidor;
            if (camp == null) return;

            System.IO.Directory.CreateDirectory(CarpetaDePerfils);
            string ruta = $"{CarpetaDePerfils}/Camp_{perfil.name}.prefab";

            GameObject copia = Object.Instantiate(camp.gameObject);
            copia.name = DecoracioDeGespa.NomDelContenidor;
            // L'arrel a zero: el joc el posa com a fill de la decoració amb
            // la posició local del prefab, i els fills ja hi són relatius.
            copia.transform.localPosition = Vector3.zero;
            foreach (Transform t in copia.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.None;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copia, ruta);
            Object.DestroyImmediate(copia);
            if (prefab == null)
            {
                Debug.LogError($"DecoracioDeGespa: no s'ha pogut desar {ruta}");
                return;
            }

            Undo.RecordObject(perfil, "Fornejar el camp");
            perfil.campFornejat = prefab;
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
            Debug.Log($"DecoracioDeGespa: {perfil.name} fa servir ara {ruta} ({prefab.GetComponentsInChildren<SpriteRenderer>(true).Length} elements)");
        }

        // ---------------------------------------------------------------

        private static void Generar(DecoracioDeGespa d)
        {
            int posats = d.Generar(d.perfil);
            Debug.Log($"DecoracioDeGespa: {d.perfil.name}, {posats} elements");
        }

        private static void CrearPerfil(DecoracioDeGespa d)
        {
            System.IO.Directory.CreateDirectory(CarpetaDePerfils);
            var p = ScriptableObject.CreateInstance<PerfilDeDecoracio>();
            p.capes.Add(new CapaDeDecoracio { nom = "Gespa", llavor = Random.Range(1, int.MaxValue) });
            string ruta = AssetDatabase.GenerateUniqueAssetPath($"{CarpetaDePerfils}/Decoracio.asset");
            AssetDatabase.CreateAsset(p, ruta);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(d, "Crear perfil");
            d.perfil = p;
            EditorUtility.SetDirty(d);
            EditorSceneManager.MarkSceneDirty(d.gameObject.scene);
        }
    }
}
