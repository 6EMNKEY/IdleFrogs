using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleFrogs.Game;
using IdleFrogs.UI;

namespace IdleFrogs.EditorTools
{
    // Al HUD de l'escena oberta (la del país): les monedes a dalt a la dreta,
    // com al mapa (gemes, la moneda del país i els ingressos per segon, una
    // sota l'altra), i el botó del compte a dalt a l'esquerra.
    //
    // Fa servir el que ja hi ha (fons, icona i text, amb els seus sprites):
    // agrupa cada trio en una "píndola" ancorada a la cantonada perquè no es
    // mogui amb la mida de la pantalla.
    public static class PreparadorDelHUD
    {
        [MenuItem("IdleFrogs/HUD/Monedes a dalt a la dreta i botó del compte")]
        public static void Colocar()
        {
            var informe = new List<string>();
            Component gemes = Primer<TextDeGemes>();
            Component doblers = Primer<TextDeDoblers>();
            Component idle = Primer<TextDeIngresIdle>();
            Transform hud = (gemes ?? doblers ?? idle)?.transform.parent;
            if (hud == null)
            {
                EditorUtility.DisplayDialog("HUD", "No hi ha cap text de gemes, doblers ni ingressos a l'escena oberta.", "D'acord");
                return;
            }

            // De dalt a baix, com al mapa.
            float y = 0;
            foreach (Component text in new[] { gemes, doblers, idle })
            {
                if (text == null) continue;
                RectTransform pindola = Pindola(text, out float alcada, out float ampladaEsquerra, out float ampladaDreta);
                if (pindola == null) { informe.Add($"{text.name}: no he trobat el seu fons"); continue; }
                if (y == 0) y = -(10 + alcada / 2);
                pindola.anchorMin = pindola.anchorMax = new Vector2(1, 1);
                pindola.anchoredPosition = new Vector2(-(15 + ampladaDreta), y);
                y -= alcada * 0.75f;
                informe.Add($"{pindola.name} a dalt a la dreta");
            }

            // El botó del compte (obre els ajustos), si encara no n'hi ha.
            bool hiEs = hud.GetComponentsInChildren<BotoDePanellGlobal>(true)
                .Any(b => new SerializedObject(b).FindProperty("accio").enumValueIndex == (int)AccioDeBoto.Configuracio);
            if (!hiEs)
            {
                float mida = 120;
                var go = new GameObject("BotoCompte", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Botó del compte");
                go.transform.SetParent(hud, false);
                var r = (RectTransform)go.transform;
                r.anchorMin = r.anchorMax = new Vector2(0, 1);
                r.sizeDelta = new Vector2(mida, mida);
                r.anchoredPosition = new Vector2(15 + mida / 2, -(15 + mida / 2));
                go.AddComponent<Image>().color = new Color(0.45f, 0.65f, 0.25f);
                go.AddComponent<Button>();
                var b = go.AddComponent<BotoDePanellGlobal>();
                var so = new SerializedObject(b);
                so.FindProperty("accio").enumValueIndex = (int)AccioDeBoto.Configuracio;
                so.ApplyModifiedPropertiesWithoutUndo();
                informe.Add("Botó del compte a dalt a l'esquerra (posa-hi la cara de la granota)");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("HUD", string.Join("\n", informe) + "\n\nRevisa-ho i desa l'escena (Ctrl+Z ho desfà).", "D'acord");
        }

        private static Component Primer<T>() where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.gameObject.scene == SceneManager.GetActiveScene());

        // Agrupa el text, el seu fons ("Fons" + el mateix nom) i la icona (la
        // imatge del costat, a la mateixa alçada) en un objecte nou. Si ja hi
        // són agrupats, torna el grup. alcada/amplades: les del fons i la icona.
        private static RectTransform Pindola(Component text, out float alcada, out float ampladaEsquerra, out float ampladaDreta)
        {
            alcada = ampladaEsquerra = ampladaDreta = 0;
            var rt = (RectTransform)text.transform;
            Transform pare = rt.parent;
            string sufix = text.name.StartsWith("Text") ? text.name.Substring(4) : text.name;

            if (pare.name == $"Pindola{sufix}")
            {
                RectTransform fet = (RectTransform)pare;
                Mides(fet, out alcada, out ampladaEsquerra, out ampladaDreta);
                return fet;
            }

            var fons = pare.Find($"Fons{sufix}") as RectTransform;
            if (fons == null) return null;
            // La icona: una imatge germana sense text ni botó, a l'alçada del fons i a la seva esquerra.
            RectTransform icona = pare.Cast<Transform>()
                .OfType<RectTransform>()
                .Where(t => t != fons && t != rt && t.GetComponent<Image>() != null && t.GetComponent<Button>() == null &&
                            t.GetComponentInChildren<TMPro.TMP_Text>(true) == null &&
                            Mathf.Abs(t.anchoredPosition.y - fons.anchoredPosition.y) < fons.rect.height / 2 &&
                            t.anchoredPosition.x < fons.anchoredPosition.x &&
                            t.anchorMin == fons.anchorMin && t.anchorMax == fons.anchorMax)
                .OrderBy(t => Mathf.Abs(t.anchoredPosition.y - fons.anchoredPosition.y))
                .FirstOrDefault();

            var go = new GameObject($"Pindola{sufix}", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Píndola");
            var grup = (RectTransform)go.transform;
            grup.SetParent(pare, false);
            grup.anchorMin = grup.anchorMax = fons.anchorMin;
            grup.sizeDelta = Vector2.zero;
            grup.anchoredPosition = fons.anchoredPosition;
            grup.SetSiblingIndex(fons.GetSiblingIndex());

            // El fons primer (a sota), després la icona i el text.
            foreach (RectTransform t in new[] { fons, icona, rt })
                if (t != null) Undo.SetTransformParent(t, grup, "Píndola");

            Mides(grup, out alcada, out ampladaEsquerra, out ampladaDreta);
            return grup;
        }

        // Fins on arriba el grup a cada costat del seu centre, i l'alçada del fons.
        private static void Mides(RectTransform grup, out float alcada, out float esquerra, out float dreta)
        {
            alcada = 0; esquerra = 0; dreta = 0;
            foreach (RectTransform t in grup)
            {
                float x0 = t.anchoredPosition.x - t.rect.width * t.pivot.x;
                float x1 = x0 + t.rect.width;
                esquerra = Mathf.Max(esquerra, -x0);
                dreta = Mathf.Max(dreta, x1);
                if (t.name.StartsWith("Fons")) alcada = t.rect.height;
            }
            if (alcada <= 0) alcada = 60;
        }
    }
}
