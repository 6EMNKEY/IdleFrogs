using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // Posa el GestorDAudio a l'escena (si no hi és) i hi omple tots els
    // efectes i la música a partir dels fitxers d'Audio/SFX i Audio/Musica,
    // amb volums de partida. Es pot tornar a executar després de canviar
    // els fitxers (mateix nom = mateix so).
    public static class ConfiguradorDAudio
    {
        private const string Sfx = "Assets/_Project/Audio/SFX";
        private const string Musica = "Assets/_Project/Audio/Musica/musica_bucle.ogg";

        // so, fitxer, volum, variació de to, interval mínim
        private static readonly (So so, string fitxer, float volum, float to, float interval)[] Efectes =
        {
            (So.Toc, "toc", 0.6f, 0.06f, 0f),
            (So.Obrir, "obrir", 0.55f, 0.03f, 0f),
            (So.Tancar, "tancar", 0.5f, 0.03f, 0f),
            (So.Acceptar, "acceptar", 0.8f, 0.02f, 0f),
            (So.Cancellar, "cancellar", 0.7f, 0.02f, 0f),
            (So.Error, "error", 0.55f, 0.02f, 0.15f),
            (So.Comprar, "comprar", 0.85f, 0.03f, 0f),
            (So.Millorar, "millorar", 0.8f, 0.04f, 0.05f),
            (So.Vendre, "vendre", 0.8f, 0.03f, 0f),
            (So.Desbloquejar, "desbloquejar", 0.9f, 0f, 0f),
            (So.Moneda, "moneda", 0.45f, 0.08f, 0.05f),
            (So.Gema, "gema", 0.5f, 0.08f, 0.05f),
            (So.Recompensa, "recompensa", 0.85f, 0f, 0.2f),
            (So.Potenciador, "potenciador", 0.6f, 0f, 0.2f),
        };

        [MenuItem("IdleFrogs/Configurar l'àudio")]
        public static void Configurar()
        {
            var gestor = UnityEngine.Object.FindFirstObjectByType<GestorDAudio>(FindObjectsInactive.Include);
            if (gestor == null)
            {
                var go = new GameObject("Audio");
                Undo.RegisterCreatedObjectUndo(go, "Crear l'àudio");
                gestor = go.AddComponent<GestorDAudio>();
            }

            var so = new SerializedObject(gestor);
            so.FindProperty("musicaPerDefecte").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Musica);
            SerializedProperty llista = so.FindProperty("efectes");
            llista.arraySize = Efectes.Length;
            int sensefitxer = 0;
            for (int i = 0; i < Efectes.Length; i++)
            {
                var e = Efectes[i];
                SerializedProperty el = llista.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("so").enumValueIndex = Array.IndexOf(Enum.GetValues(typeof(So)), e.so);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Sfx}/{e.fitxer}.wav");
                if (clip == null) sensefitxer++;
                SerializedProperty clips = el.FindPropertyRelative("clips");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                el.FindPropertyRelative("volum").floatValue = e.volum;
                el.FindPropertyRelative("variacioDeTo").floatValue = e.to;
                el.FindPropertyRelative("intervalMinim").floatValue = e.interval;
            }
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(gestor.gameObject.scene);
            Selection.activeObject = gestor;
            Debug.Log(sensefitxer == 0
                ? "Àudio: GestorDAudio configurat amb 14 efectes i la música. Desa l'escena."
                : $"Àudio: configurat, però falten {sensefitxer} fitxers a {Sfx}");
        }
    }
}
