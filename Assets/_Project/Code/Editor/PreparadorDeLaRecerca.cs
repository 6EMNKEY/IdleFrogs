using System.Collections.Generic;
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
    // Crea Assets/Scenes/Recerca.unity i Resources/Recerca.asset amb l'arbre
    // proposat (52 nodes): una branca per país (punts del seu color, efecte
    // només al país), la dels anuncis (dos colors) i les generals (diversos
    // colors, efecte a tots). Tot editable després al SO.
    public static class PreparadorDeLaRecerca
    {
        private const string RutaDeLEscena = "Assets/Scenes/Recerca.unity";
        private const string RutaDeLArbre = "Assets/_Project/Resources/Recerca.asset";
        private const int Paisos = 6;

        [MenuItem("IdleFrogs/Recerca/Crear l'escena de la recerca")]
        public static void Crear()
        {
            if (!PotSubstituir(RutaDeLEscena, "Crear l'escena de la recerca")) return;

            CrearLArbre(false);
            AfegirASistemes<GestorDeRecerca>();
            AfegirASistemes<GestorDeLArena>();

            Transform arrel = NovaEscena("CanvasRecerca", out Scene escena);
            Caixa("Fons", arrel, 0, 0, 1, 1, new Color(0.2f, 0.25f, 0.18f));

            // L'arbre: un ScrollRect a tota la pantalla menys la barra de dalt.
            Image vista = Caixa("Arbre", arrel, 0, 0, 1, 0.88f, new Color(0, 0, 0, 0.1f));
            var scroll = vista.gameObject.AddComponent<ScrollRect>();
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            vista.gameObject.AddComponent<RectMask2D>();
            vista.gameObject.AddComponent<ZoomDeScrollRect>();
            RectTransform contingut = Buit("Contingut", vista.transform, 0.5f, 0.5f, 0.5f, 0.5f);
            contingut.sizeDelta = new Vector2(4000, 2500);
            scroll.viewport = (RectTransform)vista.transform;
            scroll.content = contingut;
            NodeDeRecercaUI plantilla = NodeDeMostra(contingut);

            // La barra de dalt: els punts de cada color.
            Caixa("Barra", arrel, 0, 0.88f, 1, 1, EinesDEscenes.Fosc);
            Localitzat(Text("TextTitol", arrel, 0.01f, 0.89f, 0.17f, 0.99f, "RESEARCH", 50, Color.white), "recerca.titol");
            var punts = new TMP_Text[Paisos];
            for (int z = 0; z < Paisos; z++)
            {
                float x0 = 0.18f + z * 0.12f;
                punts[z] = Text($"TextPunts{z + 1}", arrel, x0, 0.89f, x0 + 0.11f, 0.99f, "0", 44, Color.white);
            }
            Button tancar = Creu(arrel, 0.93f, 0.89f, 0.98f, 0.99f);

            PanellDeNode panell = Panell(arrel);

            var pantalla = new GameObject("PantallaDeLaRecerca").AddComponent<PantallaDeLaRecerca>();
            AssignarLlista(pantalla, "textsDePunts", punts);
            Assignar(pantalla, "botoTancar", tancar);
            Assignar(pantalla, "contingut", contingut);
            Assignar(pantalla, "plantilla", plantilla);
            Assignar(pantalla, "panellDeNode", panell);

            Desar(escena, RutaDeLEscena);
            Debug.Log($"PreparadorDeLaRecerca: {RutaDeLEscena} creada");
        }

        private static NodeDeRecercaUI NodeDeMostra(Transform pare)
        {
            RectTransform r = Buit("Node", pare, 0.5f, 0.5f, 0.5f, 0.5f);
            r.sizeDelta = new Vector2(150, 150);
            Image marc = r.gameObject.AddComponent<Image>();
            Button boto = r.gameObject.AddComponent<Button>();
            Image dins = Caixa("Fons", r, 0.07f, 0.07f, 0.93f, 0.93f, new Color(0.25f, 0.2f, 0.15f));
            dins.raycastTarget = false;
            Image icona = Caixa("Icona", r, 0.15f, 0.25f, 0.85f, 0.9f, Color.white);
            icona.preserveAspect = true;
            icona.raycastTarget = false;
            TMP_Text nivell = Text("TextNivell", r, 0.05f, 0.04f, 0.95f, 0.26f, "0/5", 30, Color.white);

            var n = r.gameObject.AddComponent<NodeDeRecercaUI>();
            Assignar(n, "boto", boto);
            Assignar(n, "marc", marc);
            Assignar(n, "icona", icona);
            Assignar(n, "textNivell", nivell);
            return n;
        }

        private static PanellDeNode Panell(Transform arrel)
        {
            Image ombra = Caixa("PanellDeNode", arrel, 0, 0, 1, 1, Ombra);
            Button fons = ombra.gameObject.AddComponent<Button>();
            Image caixa = Caixa("Caixa", ombra.transform, 0.25f, 0.15f, 0.75f, 0.85f, EinesDEscenes.Panell);
            caixa.gameObject.AddComponent<Outline>().effectColor = Vora;
            Transform c = caixa.transform;
            Image icona = Caixa("Icona", c, 0.05f, 0.68f, 0.25f, 0.95f, new Color(0.25f, 0.2f, 0.15f));
            icona.preserveAspect = true;
            TMP_Text nom = Text("TextNom", c, 0.28f, 0.82f, 0.88f, 0.95f, "Node", 48, Color.black, TextAlignmentOptions.Left);
            TMP_Text nivell = Text("TextNivell", c, 0.28f, 0.7f, 0.88f, 0.81f, "0/5", 36, Vora, TextAlignmentOptions.Left);
            Button tancar = Creu(c, 0.9f, 0.88f, 0.99f, 0.99f);
            TMP_Text descripcio = Text("TextDescripcio", c, 0.06f, 0.4f, 0.94f, 0.65f, "", 38, Color.black);
            TMP_Text requisits = Text("TextRequisits", c, 0.06f, 0.32f, 0.94f, 0.4f, "", 30, Vermell);
            TMP_Text cost = Text("TextCost", c, 0.06f, 0.2f, 0.94f, 0.31f, "", 44, Color.black);
            Button comprar = Boto("BotoComprar", c, 0.25f, 0.04f, 0.75f, 0.17f, BotoVerd, "Research", 44);

            var p = ombra.gameObject.AddComponent<PanellDeNode>();
            Assignar(p, "arrel", ombra.gameObject);
            Assignar(p, "icona", icona);
            Assignar(p, "textNom", nom);
            Assignar(p, "textNivell", nivell);
            Assignar(p, "textDescripcio", descripcio);
            Assignar(p, "textRequisits", requisits);
            Assignar(p, "textCost", cost);
            Assignar(p, "botoComprar", comprar);
            Assignar(p, "textComprar", TextDe(comprar));
            Assignar(p, "botoTancar", tancar);
            // Tocar el fons fosc també tanca.
            UnityEditor.Events.UnityEventTools.AddPersistentListener(fons.onClick, p.Tancar);
            return p;
        }

        // ---------------------------------------------------------------
        // L'arbre proposat
        // ---------------------------------------------------------------

        [MenuItem("IdleFrogs/Recerca/Tornar a crear l'arbre de mostra")]
        public static void TornarACrearLArbre() => CrearLArbre(true);

        private static void CrearLArbre(bool substituir)
        {
            ArbreDeRecercaSO arbre = Asset<ArbreDeRecercaSO>(RutaDeLArbre);
            if (arbre.nodes.Count > 0 && !substituir) return;
            if (substituir && arbre.nodes.Count > 0 &&
                !EditorUtility.DisplayDialog("Arbre de recerca", "Se substituiran tots els nodes de Resources/Recerca. Endavant?", "Sí", "No"))
                return;

            var nodes = new List<NodeDeRecerca>();
            string[] paisos = NomsDelsPaisos();

            // Branques de país: una columna cadascuna, de baix a dalt.
            for (int k = 0; k < Paisos; k++)
            {
                float x = -1500 + k * 600;
                string p = paisos[k];
                string Id(string s) => $"p{k}_{s}";
                nodes.Add(N(Id("produccio"), T($"Hungry frogs ({p})", $"Ranas glotonas ({p})", $"Granotes golafres ({p})"),
                    T("Swamp production: {0} → {1}", "Producción de los pantanos: {0} → {1}", "Producció dels pantans: {0} → {1}"),
                    new Vector2(x, -1000), 5, TipusDEfecteDeRecerca.ProduccioDePantans, k, 0.1, C(k, 2)));
                nodes.Add(N(Id("tren"), T($"Fast track ({p})", $"Vía rápida ({p})", $"Via ràpida ({p})"),
                    T("Train speed: {0} → {1}", "Velocidad del tren: {0} → {1}", "Velocitat del tren: {0} → {1}"),
                    new Vector2(x - 130, -820), 5, TipusDEfecteDeRecerca.VelocitatDelTren, k, 0.05, C(k, 3), Id("produccio")));
                nodes.Add(N(Id("restaurant"), T($"Big kitchen ({p})", $"Cocina grande ({p})", $"Cuina gran ({p})"),
                    T("Restaurant capacity: {0} → {1}", "Capacidad del restaurante: {0} → {1}", "Capacitat del restaurant: {0} → {1}"),
                    new Vector2(x + 130, -820), 5, TipusDEfecteDeRecerca.CapacitatDelRestaurant, k, 0.1, C(k, 4), Id("produccio")));
                nodes.Add(N(Id("regateig"), T($"Haggling ({p})", $"Regateo ({p})", $"Regateig ({p})"),
                    T("Upgrade costs: {0} → {1}", "Coste de las mejoras: {0} → {1}", "Cost de les millores: {0} → {1}"),
                    new Vector2(x - 130, -640), 5, TipusDEfecteDeRecerca.CostDeMillores, k, -0.03, C(k, 6), Id("tren")));
                nodes.Add(N(Id("entrenament"), T($"Training ({p})", $"Entrenamiento ({p})", $"Entrenament ({p})"),
                    T("Fighter damage: {0} → {1}", "Daño de los luchadores: {0} → {1}", "Dany dels lluitadors: {0} → {1}"),
                    new Vector2(x + 130, -640), 5, TipusDEfecteDeRecerca.DanyDeLluitadors, k, 0.1, C(k, 8), Id("restaurant")));
                nodes.Add(N(Id("boti"), T($"Loot ({p})", $"Botín ({p})", $"Botí ({p})"),
                    T("Boss research points: {0} → {1}", "Puntos de los bosses: {0} → {1}", "Punts dels bosses: {0} → {1}"),
                    new Vector2(x, -460), 3, TipusDEfecteDeRecerca.PuntsDeBosses, k, 0.1, C(k, 12), Id("regateig"), Id("entrenament")));
            }

            // Anuncis: dos colors cadascun; surten del Regateig d'un país.
            (string id, TextTraduit nom, TextTraduit desc, TipusDEfecteDeRecerca efecte, double valor, int nivells)[] anuncis =
            {
                ("anunci_barrera", T("Barrier videos", "Vídeos de barrera", "Vídeos de barrera"),
                    T("Extra time per video: {0} → {1}", "Tiempo extra por vídeo: {0} → {1}", "Temps de més per vídeo: {0} → {1}"),
                    TipusDEfecteDeRecerca.AnunciBarrera, 10, 3),
                ("anunci_mosca", T("Golden fly", "Mosca dorada", "Mosca daurada"),
                    T("Golden fly reward: {0} → {1}", "Recompensa de la mosca: {0} → {1}", "Recompensa de la mosca: {0} → {1}"),
                    TipusDEfecteDeRecerca.AnunciMosca, 0.2, 3),
                ("anunci_offline", T("Offline video", "Vídeo offline", "Vídeo offline"),
                    T("Offline video multiplier: {0} → {1}", "Multiplicador del vídeo offline: {0} → {1}", "Multiplicador del vídeo offline: {0} → {1}"),
                    TipusDEfecteDeRecerca.AnunciOffline, 0.5, 2),
                ("anunci_etfalten", T("Quick cash", "Dinero rápido", "Diners ràpids"),
                    T("\"You need X\" by video, up to: {0} → {1}", "«Te faltan X» con vídeo, hasta: {0} → {1}", "«Et falten X» amb vídeo, fins a: {0} → {1}"),
                    TipusDEfecteDeRecerca.AnunciEtFalten, 15, 3),
                ("anunci_regal", T("Gift videos", "Vídeos de regalo", "Vídeos de regal"),
                    T("Calendar and gift video: {0} → {1}", "Vídeo del calendario y del regalo: {0} → {1}", "Vídeo del calendari i del regal: {0} → {1}"),
                    TipusDEfecteDeRecerca.AnunciRegal, 0.5, 2),
                ("anunci_potenciador", T("Longer boosts", "Potenciadores largos", "Potenciadors llargs"),
                    T("Video boost duration: {0} → {1}", "Duración del potenciador por vídeo: {0} → {1}", "Durada del potenciador per vídeo: {0} → {1}"),
                    TipusDEfecteDeRecerca.AnunciPotenciador, 10, 3),
            };
            for (int i = 0; i < anuncis.Length; i++)
            {
                var a = anuncis[i];
                int c1 = i, c2 = (i + 1) % Paisos;
                nodes.Add(N(a.id, a.nom, a.desc, new Vector2(-1500 + i * 600, -200), a.nivells, a.efecte, -1, a.valor,
                            C(c1, 10, 1.6), C(c2, 10, 1.6), $"p{i}_regateig"));
            }

            // Generals: dalt de tot; els últims demanen tots els colors.
            int[] tots = { 0, 1, 2, 3, 4, 5 };
            nodes.Add(N("g_ingressos", T("Golden age", "Edad de oro", "Edat d'or"),
                T("Income in every country: {0} → {1}", "Ingresos en todos los países: {0} → {1}", "Ingressos a tots els països: {0} → {1}"),
                new Vector2(-900, 150), 10, TipusDEfecteDeRecerca.Ingressos, -1, 0.1, Cs(5, 1.3, 0, 1), "p0_boti", "p1_boti"));
            nodes.Add(N("g_offline", T("Night shift", "Turno de noche", "Torn de nit"),
                T("Offline income: {0} → {1}", "Ingresos offline: {0} → {1}", "Ingressos offline: {0} → {1}"),
                new Vector2(-1200, 450), 5, TipusDEfecteDeRecerca.IngressosOffline, -1, 0.1, Cs(8, 1.5, 0, 1, 2), "g_ingressos"));
            nodes.Add(N("g_habilitats", T("Bossy bosses", "Jefes mandones", "Caps manaires"),
                T("Manager ability duration: {0} → {1}", "Duración de las habilidades: {0} → {1}", "Durada de les habilitats: {0} → {1}"),
                new Vector2(-600, 450), 5, TipusDEfecteDeRecerca.DuracioDHabilitats, -1, 0.1, Cs(8, 1.5, 1, 2, 3), "g_ingressos"));
            nodes.Add(N("g_recarrega", T("Coffee break", "Pausa del café", "Pausa del cafè"),
                T("Ability recharge time: {0} → {1}", "Recarga de las habilidades: {0} → {1}", "Recàrrega de les habilitats: {0} → {1}"),
                new Vector2(-600, 750), 5, TipusDEfecteDeRecerca.RecarregaDHabilitats, -1, -0.05, Cs(13, 1.5, 2, 3, 4), "g_habilitats"));
            nodes.Add(N("g_barreres", T("Demolition", "Demolición", "Demolició"),
                T("Barrier time: {0} → {1}", "Tiempo de las barreras: {0} → {1}", "Temps de les barreres: {0} → {1}"),
                new Vector2(-1200, 750), 5, TipusDEfecteDeRecerca.TempsDeBarreres, -1, -0.05, Cs(13, 1.5, 3, 4, 5), "g_offline"));
            nodes.Add(N("g_fites", T("Milestone hunter", "Cazahitos", "Caçador de fites"),
                T("Milestone gems: {0} → {1}", "Gemas de los hitos: {0} → {1}", "Gemes de les fites: {0} → {1}"),
                new Vector2(-900, 1000), 3, TipusDEfecteDeRecerca.GemesDeFites, -1, 0.1, Cs(20, 1.6, 0, 3, 5), "g_barreres", "g_recarrega"));
            nodes.Add(N("g_dany", T("War drums", "Tambores de guerra", "Tambors de guerra"),
                T("Damage of every fighter: {0} → {1}", "Daño de todos los luchadores: {0} → {1}", "Dany de tots els lluitadors: {0} → {1}"),
                new Vector2(900, 150), 10, TipusDEfecteDeRecerca.DanyDeLluitadors, -1, 0.05, Cs(5, 1.3, 2, 3), "p2_boti", "p3_boti"));
            nodes.Add(N("g_bosses", T("Trophy room", "Sala de trofeos", "Sala de trofeus"),
                T("Research points from bosses: {0} → {1}", "Puntos de los bosses: {0} → {1}", "Punts dels bosses: {0} → {1}"),
                new Vector2(900, 450), 5, TipusDEfecteDeRecerca.PuntsDeBosses, -1, 0.1, Cs(10, 1.5, 2, 3, 4, 5), "g_dany"));
            nodes.Add(N("g_refrescos", T("Fresh water", "Agua fresca", "Aigua fresca"),
                T("Free arena refreshes per day: {0} → {1}", "Refrescos gratis al día: {0} → {1}", "Refrescos gratis al dia: {0} → {1}"),
                new Vector2(600, 750), 2, TipusDEfecteDeRecerca.RefrescosGratis, -1, 1, Cs(27, 2, tots), "g_bosses"));
            nodes.Add(N("g_sort", T("Lucky charm", "Amuleto", "Amulet"),
                T("Luck in summons: {0} → {1}", "Suerte en las invocaciones: {0} → {1}", "Sort a les invocacions: {0} → {1}"),
                new Vector2(1200, 750), 3, TipusDEfecteDeRecerca.SortAlGacha, -1, 0.2, Cs(33, 1.8, tots), "g_bosses"));

            arbre.nodes = nodes;
            EditorUtility.SetDirty(arbre);
            AssetDatabase.SaveAssets();

            double[] total = ModelDeRecerca.CostTotal(nodes, Paisos);
            Debug.Log($"PreparadorDeLaRecerca: {nodes.Count} nodes. Punts per acabar-lo, per color: {string.Join(" · ", total)}");
        }

        private static NodeDeRecerca N(string id, TextTraduit nom, TextTraduit desc, Vector2 posicio, int nivells,
                                       TipusDEfecteDeRecerca efecte, int pais, double valor, params object[] costIRequisits)
        {
            var costos = new List<CostDeRecerca>();
            var requisits = new List<string>();
            foreach (object o in costIRequisits)
            {
                if (o is CostDeRecerca c) costos.Add(c);
                else if (o is CostDeRecerca[] cs) costos.AddRange(cs);
                else if (o is string s) requisits.Add(s);
            }
            return new NodeDeRecerca
            {
                id = id, nom = nom, descripcio = desc, posicio = posicio, nivellMaxim = nivells,
                efecte = efecte, pais = pais, valorPerNivell = valor,
                cost = costos.ToArray(), prerequisits = requisits.ToArray(),
            };
        }

        // Calibrat amb simulador_arena.py: ~1.200-1.600 punts per color per
        // acabar-lo (uns 230 dies a 2-3 estrelles, 130 a 5).
        private static CostDeRecerca C(int pais, double costBase, double creixement = 1.5) =>
            new CostDeRecerca { pais = pais, costBase = costBase, creixement = creixement };

        private static CostDeRecerca[] Cs(double costBase, double creixement, params int[] paisos)
        {
            var r = new CostDeRecerca[paisos.Length];
            for (int i = 0; i < paisos.Length; i++) r[i] = C(paisos[i], costBase, creixement);
            return r;
        }

        private static TextTraduit T(string angles, string castella, string catala) =>
            new TextTraduit { angles = angles, castella = castella, catala = catala };

        // Els noms dels països (per als nodes de cada branca), del GestorDePaisos.
        private static string[] NomsDelsPaisos()
        {
            var noms = new string[Paisos];
            for (int i = 0; i < Paisos; i++) noms[i] = $"{i + 1}";
            var sistemes = Resources.Load<GameObject>("Sistemes");
            var g = sistemes != null ? sistemes.GetComponentInChildren<GestorDePaisos>(true) : null;
            if (g == null || g.Paisos == null) return noms;
            int z = 0;
            foreach (PaisSO p in g.Paisos)
            {
                if (p == null || p.esCiutatComercial) continue;
                if (z >= Paisos) break;
                noms[z++] = p.NomVisible;
            }
            return noms;
        }
    }
}
