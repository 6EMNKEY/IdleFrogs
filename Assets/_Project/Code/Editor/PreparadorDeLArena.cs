using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleFrogs.Game;
using IdleFrogs.UI;
using static IdleFrogs.EditorTools.EinesDEscenes;

namespace IdleFrogs.EditorTools
{
    // Crea Assets/Scenes/Arena.unity, Resources/Arena.asset (amb les rareses i
    // un catàleg de lluitadors i bosses de mostra) i posa GestorDeLArena i
    // GestorDeRecerca a Resources/Sistemes. Tot són quadrats de colors.
    public static class PreparadorDeLArena
    {
        private const string RutaDeLEscena = "Assets/Scenes/Arena.unity";
        private const string RutaDeLaConfig = "Assets/_Project/Resources/Arena.asset";
        private const string CarpetaDeMostra = "Assets/_Project/Data/Arena";

        private static readonly Color Aigua = new Color(0.1f, 0.75f, 0.85f);
        private static readonly Color Nenufar = new Color(0.3f, 0.6f, 0.25f);

        [MenuItem("IdleFrogs/Arena/Crear l'escena de l'arena")]
        public static void Crear()
        {
            if (!PotSubstituir(RutaDeLEscena, "Crear l'escena de l'arena")) return;

            CrearLaConfig();
            AfegirASistemes<GestorDeLArena>();
            AfegirASistemes<GestorDeRecerca>();

            Transform arrel = NovaEscena("CanvasArena", out Scene escena);
            Image fons = Caixa("Fons", arrel, 0, 0, 1, 1, Verd);

            // --- Dalt -------------------------------------------------------
            TMP_Text nom = Text("TextNomDelPais", arrel, 0.06f, 0.9f, 0.3f, 0.98f, "País", 56, Color.black, TextAlignmentOptions.Left);
            TMP_Text punts = Text("TextPunts", arrel, 0.3f, 0.9f, 0.5f, 0.98f, "0", 44, Color.white);
            TMP_Text tickets = Text("TextTickets", arrel, 0.5f, 0.9f, 0.68f, 0.98f, "", 36, Color.black);
            TMP_Text cicle = Text("TextCicle", arrel, 0.06f, 0.84f, 0.45f, 0.9f, "", 32, Color.black, TextAlignmentOptions.Left);
            Button refrescar = Boto("BotoRefrescar", arrel, 0.69f, 0.9f, 0.88f, 0.98f, BotoBlau, "Refresh", 34);
            Button tancar = Creu(arrel, 0.93f, 0.9f, 0.98f, 0.98f);
            Button anterior = Fletxa("BotoAnterior", arrel, 0.005f, 0.045f, "<");
            Button seguent = Fletxa("BotoSeguent", arrel, 0.955f, 0.995f, ">");

            // --- Pantans i botons de baix --------------------------------------
            RectTransform contingut = Buit("Contingut", arrel, 0, 0, 1, 1);
            var pantans = new PantaDeLArena[ModelDeLArena.Pantans];
            var rectsDelsPantans = new RectTransform[pantans.Length];
            for (int i = 0; i < pantans.Length; i++)
            {
                float x0 = 0.06f + i * 0.3f;
                pantans[i] = Panta(i, contingut, x0, x0 + 0.28f, out rectsDelsPantans[i]);
            }
            Button lluitadors = Boto("BotoLluitadors", contingut, 0.25f, 0.03f, 0.48f, 0.14f, BotoVerd, "Fighters", 44, "arena.lluitadors");
            Button gacha = Boto("BotoGacha", contingut, 0.52f, 0.03f, 0.75f, 0.14f, BotoBlau, "Summon", 44, "arena.gacha");
            TMP_Text missatge = Text("TextMissatge", contingut, 0.1f, 0.14f, 0.9f, 0.18f, "", 34, Color.black);

            TMP_Text bloquejada = Text("TextBloquejada", arrel, 0.1f, 0.4f, 0.9f, 0.6f, "", 56, Color.black);

            // --- Panells (per sobre de tot) ------------------------------------
            PanellDeLluitadors panellLl = PanellDeLluitadors(arrel);
            PanellDelGacha panellGa = PanellDelGacha(arrel);

            GespaDelPrestigi gespa = Gespa(fons, rectsDelsPantans);

            var pantalla = new GameObject("PantallaDeLArena").AddComponent<PantallaDeLArena>();
            Assignar(pantalla, "fons", fons);
            Assignar(pantalla, "gespa", gespa);
            Assignar(pantalla, "textNomDelPais", nom);
            Assignar(pantalla, "textPunts", punts);
            Assignar(pantalla, "textTickets", tickets);
            Assignar(pantalla, "textCicle", cicle);
            Assignar(pantalla, "botoRefrescar", refrescar);
            Assignar(pantalla, "textRefrescar", TextDe(refrescar));
            AssignarLlista(pantalla, "pantans", pantans);
            Assignar(pantalla, "textBloquejada", bloquejada);
            Assignar(pantalla, "contingut", contingut.gameObject);
            Assignar(pantalla, "botoLluitadors", lluitadors);
            Assignar(pantalla, "botoGacha", gacha);
            Assignar(pantalla, "textMissatge", missatge);
            Assignar(pantalla, "panellDeLluitadors", panellLl);
            Assignar(pantalla, "panellDelGacha", panellGa);
            Assignar(pantalla, "botoTancar", tancar);
            Assignar(pantalla, "botoAnterior", anterior);
            Assignar(pantalla, "botoSeguent", seguent);

            Desar(escena, RutaDeLEscena);
            Debug.Log($"PreparadorDeLArena: {RutaDeLEscena} creada");
        }

        // Un pantà: el boss a dalt, la vida i l'estat al mig, el pantà i el nenúfar a baix.
        private static PantaDeLArena Panta(int index, Transform pare, float x0, float x1, out RectTransform rect)
        {
            rect = Buit($"Panta{index + 1}", pare, x0, 0.19f, x1, 0.83f);
            Transform p = rect;
            Image insignia = Caixa("Insignia", p, 0f, 0.9f, 0.18f, 0.99f, Color.gray);
            TMP_Text raresa = Text("TextRaresa", insignia.transform, 0, 0, 1, 1, "D", 40, Color.white);
            TMP_Text nomBoss = Text("TextNomDelBoss", p, 0.2f, 0.9f, 1f, 0.99f, "Boss", 36, Color.black, TextAlignmentOptions.Left);
            Image boss = Caixa("Boss", p, 0.2f, 0.55f, 0.8f, 0.89f, Color.gray);
            boss.preserveAspect = true;

            Image fonsBarra = Caixa("Vida", p, 0.05f, 0.5f, 0.95f, 0.54f, new Color(0.2f, 0.05f, 0.05f));
            Image barra = Caixa("Barra", fonsBarra.transform, 0, 0, 1, 1, new Color(0.85f, 0.15f, 0.15f));
            TMP_Text vida = Text("TextVida", p, 0.05f, 0.45f, 0.95f, 0.5f, "", 28, Color.black);
            TMP_Text regen = Text("TextRegeneracio", p, 0.05f, 0.41f, 0.95f, 0.45f, "", 26, Color.black);
            TMP_Text estat = Text("TextEstat", p, 0.02f, 0.36f, 0.98f, 0.41f, "", 30, Color.white);

            Image panta = Caixa("DibuixDelPanta", p, 0f, 0f, 1f, 0.31f, Aigua);
            panta.preserveAspect = true;
            Image nenufar = Caixa("Nenufar", p, 0.3f, 0.04f, 0.7f, 0.27f, Nenufar);
            Button botoNenufar = nenufar.gameObject.AddComponent<Button>();
            Image lluitador = Caixa("Lluitador", nenufar.transform, 0.15f, 0.15f, 0.85f, 0.95f, new Color(1, 1, 1, 0.25f));
            lluitador.preserveAspect = true;
            lluitador.raycastTarget = false;
            TMP_Text textLluitador = Text("TextLluitador", p, 0f, 0.31f, 1f, 0.36f, "", 28, Color.black);

            var c = rect.gameObject.AddComponent<PantaDeLArena>();
            Assignar(c, "index", index);
            Assignar(c, "dibuixDelPanta", panta);
            Assignar(c, "boss", boss);
            Assignar(c, "textNomDelBoss", nomBoss);
            Assignar(c, "insignia", insignia);
            Assignar(c, "textRaresa", raresa);
            Assignar(c, "barraDeVida", barra.rectTransform);
            Assignar(c, "textVida", vida);
            Assignar(c, "textRegeneracio", regen);
            Assignar(c, "textEstat", estat);
            Assignar(c, "nenufar", botoNenufar);
            Assignar(c, "lluitador", lluitador);
            Assignar(c, "textLluitador", textLluitador);
            return c;
        }

        // Una fila de la llista de lluitadors (també serveix de carta del gacha).
        private static TargetaDeLluitador Targeta(Transform pare, bool fila)
        {
            Image fons = Caixa(fila ? "FilaDeLluitador" : "CartaDeLluitador", pare, 0, 0, 1, 1, Pergami);
            Button boto = fons.gameObject.AddComponent<Button>();
            var t = fons.gameObject.AddComponent<TargetaDeLluitador>();
            Transform f = fons.transform;
            Image icona, insignia;
            TMP_Text nom, raresa, nivell, dany, on = null;
            Button millorar = null;
            if (fila)
            {
                fons.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
                icona = Caixa("Icona", f, 0.01f, 0.08f, 0.09f, 0.92f, Color.gray);
                insignia = Caixa("Insignia", f, 0.1f, 0.55f, 0.16f, 0.92f, Color.gray);
                raresa = Text("TextRaresa", insignia.transform, 0, 0, 1, 1, "D", 34, Color.white);
                nom = Text("TextNom", f, 0.17f, 0.55f, 0.5f, 0.92f, "Nom", 34, Color.black, TextAlignmentOptions.Left);
                nivell = Text("TextNivell", f, 0.1f, 0.08f, 0.25f, 0.5f, "", 30, Color.black, TextAlignmentOptions.Left);
                dany = Text("TextDany", f, 0.25f, 0.08f, 0.5f, 0.5f, "", 30, Color.black, TextAlignmentOptions.Left);
                on = Text("TextOn", f, 0.5f, 0.08f, 0.68f, 0.92f, "", 28, Vora);
                millorar = Boto("BotoMillorar", f, 0.7f, 0.12f, 0.98f, 0.88f, BotoVerd, "", 30);
            }
            else
            {
                icona = Caixa("Icona", f, 0.1f, 0.35f, 0.9f, 0.95f, Color.gray);
                insignia = Caixa("Insignia", f, 0.05f, 0.82f, 0.3f, 0.98f, Color.gray);
                raresa = Text("TextRaresa", insignia.transform, 0, 0, 1, 1, "D", 30, Color.white);
                nom = Text("TextNom", f, 0.05f, 0.18f, 0.95f, 0.34f, "Nom", 28, Color.black);
                nivell = Text("TextNivell", f, 0.05f, 0.02f, 0.5f, 0.17f, "", 22, Color.black);
                dany = Text("TextDany", f, 0.5f, 0.02f, 0.95f, 0.17f, "", 22, Color.black);
                boto.interactable = false;
            }
            icona.preserveAspect = true;
            Assignar(t, "icona", icona);
            Assignar(t, "textNom", nom);
            Assignar(t, "insignia", insignia);
            Assignar(t, "textRaresa", raresa);
            Assignar(t, "textNivell", nivell);
            Assignar(t, "textDany", dany);
            Assignar(t, "textOn", on);
            Assignar(t, "boto", boto);
            Assignar(t, "botoMillorar", millorar);
            Assignar(t, "textMillorar", millorar != null ? TextDe(millorar) : null);
            return t;
        }

        private static PanellDeLluitadors PanellDeLluitadors(Transform arrel)
        {
            Image ombra = Caixa("PanellDeLluitadors", arrel, 0, 0, 1, 1, Ombra);
            Image caixa = Caixa("Caixa", ombra.transform, 0.1f, 0.06f, 0.9f, 0.94f, Panell);
            caixa.gameObject.AddComponent<Outline>().effectColor = Vora;
            Transform c = caixa.transform;
            TMP_Text titol = Text("TextTitol", c, 0.05f, 0.9f, 0.85f, 0.98f, "Fighters", 50, Color.black);
            Button tancar = Creu(c, 0.92f, 0.9f, 0.99f, 0.99f);
            RectTransform llista = Llista("Llista", c, 0.03f, 0.12f, 0.97f, 0.88f);
            TargetaDeLluitador plantilla = Targeta(llista, true);
            TMP_Text buit = Text("TextBuit", c, 0.1f, 0.4f, 0.9f, 0.6f, "", 40, Color.black);
            Localitzat(buit, "arena.buit");
            Button treure = Boto("BotoTreure", c, 0.35f, 0.02f, 0.65f, 0.1f, Vermell, "Remove", 36, "arena.treure");

            var p = ombra.gameObject.AddComponent<PanellDeLluitadors>();
            Assignar(p, "arrel", ombra.gameObject);
            Assignar(p, "textTitol", titol);
            Assignar(p, "contingut", llista);
            Assignar(p, "plantilla", plantilla);
            Assignar(p, "textBuit", buit);
            Assignar(p, "botoTreure", treure);
            Assignar(p, "botoTancar", tancar);
            return p;
        }

        private static PanellDelGacha PanellDelGacha(Transform arrel)
        {
            Image ombra = Caixa("PanellDelGacha", arrel, 0, 0, 1, 1, Ombra);
            Image caixa = Caixa("Caixa", ombra.transform, 0.1f, 0.06f, 0.9f, 0.94f, Panell);
            caixa.gameObject.AddComponent<Outline>().effectColor = Vora;
            Transform c = caixa.transform;
            Localitzat(Text("TextTitol", c, 0.05f, 0.9f, 0.85f, 0.98f, "Summon", 50, Color.black), "arena.gacha");
            Button tancar = Creu(c, 0.92f, 0.9f, 0.99f, 0.99f);

            TMP_Text comuns = Text("TextTicketsComuns", c, 0.05f, 0.8f, 0.48f, 0.88f, "", 36, Color.black);
            TMP_Text premium = Text("TextTicketsPremium", c, 0.52f, 0.8f, 0.95f, 0.88f, "", 36, Color.black);
            Button comuX1 = Boto("BotoComuX1", c, 0.05f, 0.68f, 0.25f, 0.79f, BotoVerd, "x1", 40);
            Button comuX10 = Boto("BotoComuX10", c, 0.27f, 0.68f, 0.48f, 0.79f, BotoVerd, "x10", 40);
            Button premX1 = Boto("BotoPremiumX1", c, 0.52f, 0.68f, 0.73f, 0.79f, new Color(0.7f, 0.35f, 0.85f), "x1", 40);
            Button premX10 = Boto("BotoPremiumX10", c, 0.75f, 0.68f, 0.95f, 0.79f, new Color(0.7f, 0.35f, 0.85f), "x10", 40);

            RectTransform resultats = Llista("Resultats", c, 0.03f, 0.1f, 0.97f, 0.66f, new Vector2(200, 260));
            TargetaDeLluitador plantilla = Targeta(resultats, false);
            TMP_Text missatge = Text("TextMissatge", c, 0.1f, 0.02f, 0.9f, 0.09f, "", 34, Vermell);

            var p = ombra.gameObject.AddComponent<PanellDelGacha>();
            Assignar(p, "arrel", ombra.gameObject);
            Assignar(p, "textTicketsComuns", comuns);
            Assignar(p, "textTicketsPremium", premium);
            Assignar(p, "botoComuX1", comuX1);
            Assignar(p, "botoComuX10", comuX10);
            Assignar(p, "botoPremiumX1", premX1);
            Assignar(p, "botoPremiumX10", premX10);
            Assignar(p, "resultats", resultats);
            Assignar(p, "plantilla", plantilla);
            Assignar(p, "textMissatge", missatge);
            Assignar(p, "botoTancar", tancar);
            return p;
        }

        // ---------------------------------------------------------------
        // La config i el catàleg de mostra
        // ---------------------------------------------------------------

        [MenuItem("IdleFrogs/Arena/Crear la config i els lluitadors de mostra")]
        public static void CrearLaConfig()
        {
            ArenaConfigSO config = Asset<ArenaConfigSO>(RutaDeLaConfig, c => c.rareses = RaresesPerDefecte());
            if (config.rareses == null || config.rareses.Length == 0) config.rareses = RaresesPerDefecte();

            if (config.lluitadors.Count == 0)
            {
                string[][] noms =
                {
                    new[] { "Granoteta", "Capgròs" }, new[] { "Saltaire", "Llengua Llarga" },
                    new[] { "Gripau Guerrer", "Reineta Ninja" }, new[] { "Granota Samurai", "Bou Gegant" },
                    new[] { "Rei del Toll", "Drac del Pantà" }, new[] { "Granota Llegendària", "Esperit del Llac" },
                };
                for (int r = 0; r < noms.Length; r++)
                    for (int i = 0; i < noms[r].Length; i++)
                    {
                        string id = $"lluitador_{(RaresaDeLluita)r}_{i + 1}".ToLowerInvariant();
                        string nom = noms[r][i];
                        config.lluitadors.Add(Asset<LluitadorSO>($"{CarpetaDeMostra}/Lluitadors/{id}.asset", l =>
                        {
                            l.id = id;
                            l.raresa = (RaresaDeLluita)r;
                            l.nom = new TextTraduit { angles = nom, castella = nom, catala = nom };
                        }));
                    }
            }

            if (config.bosses.Count == 0)
            {
                string[] noms = { "Mosquit", "Libèl·lula", "Serp d'Aigua", "Tortuga Mossegadora", "Cocodril", "Hidra del Pantà" };
                for (int r = 0; r < noms.Length; r++)
                {
                    string id = $"boss_{(RaresaDeLluita)r}".ToLowerInvariant();
                    string nom = noms[r];
                    config.bosses.Add(Asset<BossSO>($"{CarpetaDeMostra}/Bosses/{id}.asset", b =>
                    {
                        b.id = id;
                        b.raresa = (RaresaDeLluita)r;
                        b.nom = new TextTraduit { angles = nom, castella = nom, catala = nom };
                    }));
                }
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        // Els valors de partida, de simulador_arena.py (~/idleFrogs):
        // un D al màxim mata un A en ~24 h i no pot amb un S; un D a nivell 1
        // mata un D en ~5 h. Millorar: el nivell 2 costa ~1 h d'ingressos del
        // pantà 15; el 30, ~1 dia dels del final del país; el 100, ~7 dies a 5 estrelles.
        private static DadesDeRaresa[] RaresesPerDefecte()
        {
            DadesDeRaresa R(RaresaDeLluita r, Color color, double vida, double regen, int punts, float pBoss,
                            double dany, double costBase, float pComu, float pPremium) => new DadesDeRaresa
            {
                raresa = r, color = color, vidaDelBoss = vida, regeneracio = regen, punts = punts, probabilitatDelBoss = pBoss,
                danyBase = dany, creixementDelDany = 1.06, nivellMaxim = 100,
                costBase = costBase, creixementDelCost = 3.067, nivellDeCanvi = 30, creixementDespres = 1.0423,
                probabilitatComu = pComu, probabilitatPremium = pPremium,
            };
            return new[]
            {
                R(RaresaDeLluita.D, new Color(0.55f, 0.55f, 0.55f), 14400, 0.2, 1, 35, 1, 3.6e24, 45, 20),
                R(RaresaDeLluita.C, new Color(0.3f, 0.7f, 0.3f), 158000, 1.6, 3, 28, 1.6, 5.4e24, 30, 30),
                R(RaresaDeLluita.B, new Color(0.25f, 0.5f, 0.9f), 1.74e6, 12.6, 6, 20, 2.6, 8.1e24, 17, 25),
                R(RaresaDeLluita.A, new Color(0.6f, 0.3f, 0.85f), 1.91e7, 100, 10, 11, 4.2, 1.22e25, 8, 15),
                R(RaresaDeLluita.S, new Color(0.95f, 0.55f, 0.1f), 1.33e8, 400, 15, 5, 7, 1.8e25, 0, 8),
                R(RaresaDeLluita.SSS, new Color(0.9f, 0.15f, 0.2f), 1.69e8, 1500, 25, 1, 12, 2.7e25, 0, 2),
            };
        }
    }
}
