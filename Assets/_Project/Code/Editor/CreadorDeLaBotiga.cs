using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // Crea el catàleg de la botiga i tots els productes amb els valors de
    // ~/idleFrogs/tienda_valores.xlsx. Si ja existeixen, els actualitza
    // (manté les icones que hi hagis posat). Després es retoquen a mà.
    public static class CreadorDeLaBotiga
    {
        private const string Carpeta = "Assets/_Project/Data/Botiga";

        // ---- Recompenses ----
        private static Recompensa Gemes(double n, float pes = 1) => new Recompensa { tipus = TipusDeRecompensa.Gemes, quantitat = n, pes = pes };
        private static Recompensa Ingressos(double minuts, float pes = 1) => new Recompensa { tipus = TipusDeRecompensa.TempsDIngressos, quantitat = minuts, pes = pes };
        private static Recompensa Mult(double x, double minuts, float pes = 1, double maxim = 0) =>
            new Recompensa { tipus = TipusDeRecompensa.Multiplicador, multiplicador = x, minuts = minuts, minutsMaxims = maxim, pes = pes };
        private static Recompensa Encarregat(Raresa r) => new Recompensa { tipus = TipusDeRecompensa.Encarregat, raresa = r };
        private static Recompensa Cofre(float comu, float rar, float epic, float llegendari) =>
            new Recompensa { tipus = TipusDeRecompensa.Cofre, probabilitats = new[] { comu, rar, epic, llegendari } };
        private static Recompensa Permanent(EfectePermanent e) => new Recompensa { tipus = TipusDeRecompensa.Permanent, efectePermanent = e };
        private static Recompensa Pase(double gemesAlDia, int dies) => new Recompensa { tipus = TipusDeRecompensa.PaseMensual, quantitat = gemesAlDia, dies = dies };

        private static TextTraduit T(string en, string es, string ca) => new TextTraduit { angles = en, castella = es, catala = ca };

        [MenuItem("IdleFrogs/Crear la botiga")]
        public static void Crear()
        {
            System.IO.Directory.CreateDirectory(Carpeta);
            var productes = new List<ProducteDeBotiga>();

            ProducteDeBotiga P(string id, TextTraduit nom, SeccioDeBotiga s, MidaDeTargeta m, TipusDePagament pag, params Recompensa[] rs)
            {
                string ruta = $"{Carpeta}/{id}.asset";
                var p = AssetDatabase.LoadAssetAtPath<ProducteDeBotiga>(ruta);
                bool nou = p == null;
                if (nou) p = ScriptableObject.CreateInstance<ProducteDeBotiga>();
                p.id = id; p.nom = nom; p.seccio = s; p.mida = m; p.pagament = pag;
                p.recompenses = new List<Recompensa>(rs);
                p.etiqueta = default; p.preuEnGemes = 0; p.idDeCompra = string.Empty; p.preuDeMostra = string.Empty;
                p.unaAlAtzar = false; p.doblePrimeraCompra = false;
                p.limit = 0; p.limitAlDia = 0; p.horesEntreUsos = 0; p.horesDOferta = 0;
                if (nou) AssetDatabase.CreateAsset(p, ruta);
                productes.Add(p);
                return p;
            }

            ProducteDeBotiga Real(ProducteDeBotiga p, string preu) { p.idDeCompra = $"idlefrogs.{p.id}"; p.preuDeMostra = preu; return p; }

            // ---- Ofertes ----
            var inici = Real(P("pack_inici", T("Starter pack", "Pack de inicio", "Pack d'inici"), SeccioDeBotiga.Ofertes, MidaDeTargeta.Gran, TipusDePagament.DinerReal,
                Gemes(500), Encarregat(Raresa.Epic), Mult(2, 240), Ingressos(240)), "2,99 €");
            inici.limit = 1; inici.horesDOferta = 72;
            Real(P("x2_permanent", T("x2 income forever", "x2 ingresos para siempre", "x2 ingressos per sempre"), SeccioDeBotiga.Ofertes, MidaDeTargeta.Gran, TipusDePagament.DinerReal,
                Permanent(EfectePermanent.X2Ingressos)), "9,99 €");
            Real(P("sense_anuncis", T("No ads", "Sin anuncios", "Sense anuncis"), SeccioDeBotiga.Ofertes, MidaDeTargeta.Petita, TipusDePagament.DinerReal,
                Permanent(EfectePermanent.SenseAnuncis)), "4,99 €");
            Real(P("offline_x2", T("Offline x2", "Offline x2", "Offline x2"), SeccioDeBotiga.Ofertes, MidaDeTargeta.Petita, TipusDePagament.DinerReal,
                Permanent(EfectePermanent.OfflineX2)), "4,99 €");

            // ---- Pase ----
            Real(P("pase_mensual", T("Monthly pass", "Pase mensual", "Passi mensual"), SeccioDeBotiga.Pase, MidaDeTargeta.GranDoble, TipusDePagament.DinerReal,
                Gemes(300), Pase(100, 30)), "4,99 €");

            // ---- Gratis ----
            var regal = P("regal_gratis", T("Free gift", "Regalo gratis", "Regal gratis"), SeccioDeBotiga.Gratis, MidaDeTargeta.Gran, TipusDePagament.Gratis,
                Mult(2, 240, 25), Mult(5, 20, 20), Ingressos(30, 20), Gemes(1, 15), Ingressos(120, 8), Gemes(5, 5), Mult(1000, 1, 5), Gemes(20, 2));
            regal.unaAlAtzar = true; regal.horesEntreUsos = 4;
            P("anunci_x2", T("x2 for 30 min", "x2 durante 30 min", "x2 durant 30 min"), SeccioDeBotiga.Gratis, MidaDeTargeta.Petita, TipusDePagament.Anunci,
                Mult(2, 30, 1, 240));
            var anunciGemes = P("anunci_gemes", T("Free gems", "Gemas gratis", "Gemes gratis"), SeccioDeBotiga.Gratis, MidaDeTargeta.Petita, TipusDePagament.Anunci,
                Gemes(2));
            anunciGemes.limitAlDia = 5;

            // ---- Gemes ----
            (string id, string preu, int gemes, string mida, TextTraduit etiqueta)[] packs =
            {
                ("gemes_80", "0,99 €", 80, "p", default),
                ("gemes_500", "4,99 €", 500, "p", default),
                ("gemes_1100", "9,99 €", 1100, "p", default),
                ("gemes_2400", "19,99 €", 2400, "p", T("Popular", "Popular", "Popular")),
                ("gemes_6500", "49,99 €", 6500, "g", default),
                ("gemes_14000", "99,99 €", 14000, "g", T("Best value", "Mejor valor", "Millor valor")),
            };
            foreach (var k in packs)
            {
                var p = Real(P(k.id, T($"{k.gemes} gems", $"{k.gemes} gemas", $"{k.gemes} gemes"), SeccioDeBotiga.Gemes,
                    k.mida == "g" ? MidaDeTargeta.Gran : MidaDeTargeta.Petita, TipusDePagament.DinerReal, Gemes(k.gemes)), k.preu);
                p.doblePrimeraCompra = true;
                p.etiqueta = k.etiqueta;
            }

            // ---- Millores ----
            ProducteDeBotiga G(ProducteDeBotiga p, int gemes) { p.preuEnGemes = gemes; return p; }
            G(P("temps_1h", T("1 h of income", "1 h de ingresos", "1 h d'ingressos"), SeccioDeBotiga.Millores, MidaDeTargeta.Petita, TipusDePagament.Gemes, Ingressos(60)), 60);
            G(P("temps_4h", T("4 h of income", "4 h de ingresos", "4 h d'ingressos"), SeccioDeBotiga.Millores, MidaDeTargeta.Petita, TipusDePagament.Gemes, Ingressos(240)), 200);
            G(P("temps_12h", T("12 h of income", "12 h de ingresos", "12 h d'ingressos"), SeccioDeBotiga.Millores, MidaDeTargeta.Gran, TipusDePagament.Gemes, Ingressos(720)), 500);
            G(P("x2_4h", T("x2 for 4 h", "x2 durante 4 h", "x2 durant 4 h"), SeccioDeBotiga.Millores, MidaDeTargeta.Petita, TipusDePagament.Gemes, Mult(2, 240)), 150);
            G(P("x3_1h", T("x3 for 1 h", "x3 durante 1 h", "x3 durant 1 h"), SeccioDeBotiga.Millores, MidaDeTargeta.Petita, TipusDePagament.Gemes, Mult(3, 60)), 120);
            G(P("cofre", T("Manager chest", "Cofre de encargado", "Cofre d'encarregat"), SeccioDeBotiga.Millores, MidaDeTargeta.Petita, TipusDePagament.Gemes, Cofre(70, 25, 4.5f, 0.5f)), 150);
            G(P("cofre_gran", T("Big chest", "Cofre grande", "Cofre gran"), SeccioDeBotiga.Millores, MidaDeTargeta.Petita, TipusDePagament.Gemes, Cofre(0, 75, 22, 3)), 500);
            G(P("cofre_epic", T("Epic chest", "Cofre épico", "Cofre èpic"), SeccioDeBotiga.Millores, MidaDeTargeta.Gran, TipusDePagament.Gemes, Cofre(0, 0, 85, 15)), 1500);

            foreach (ProducteDeBotiga p in productes) EditorUtility.SetDirty(p);

            // ---- Catàleg i calendari ----
            string rutaCataleg = $"{Carpeta}/CatalegDeBotiga.asset";
            var cataleg = AssetDatabase.LoadAssetAtPath<CatalegDeBotiga>(rutaCataleg);
            bool catalegNou = cataleg == null;
            if (catalegNou) cataleg = ScriptableObject.CreateInstance<CatalegDeBotiga>();
            cataleg.productes = productes;

            DiaDelCalendari D(params Recompensa[] rs) => new DiaDelCalendari { recompenses = new List<Recompensa>(rs) };
            Recompensa CofreNormal() => Cofre(70, 25, 4.5f, 0.5f);
            Recompensa CofreGran() => Cofre(0, 75, 22, 3);
            cataleg.mesInicial = new List<DiaDelCalendari>
            {
                D(Gemes(2)), D(Ingressos(30)), D(Mult(2, 120)), D(Gemes(5)), D(Ingressos(60)), D(Mult(5, 20)), D(CofreGran()),
                D(Gemes(4)), D(Ingressos(120)), D(Mult(2, 240)), D(Gemes(8)), D(Ingressos(120)), D(Mult(5, 20)), D(Encarregat(Raresa.Epic)),
                D(Gemes(6)), D(Ingressos(240)), D(Mult(2, 240)), D(Gemes(12)), D(Ingressos(240)), D(Mult(1000, 1)), D(Gemes(50)),
                D(Gemes(10)), D(Ingressos(360)), D(Mult(2, 480)), D(Gemes(15)), D(Ingressos(480)), D(CofreGran()), D(Mult(1000, 1)), D(Ingressos(240)),
                D(Encarregat(Raresa.Llegendari)),
            };
            cataleg.cicle = new List<DiaDelCalendari>
            {
                D(Ingressos(30)), D(Gemes(2)), D(Mult(2, 120)), D(Ingressos(60)), D(Gemes(4)), D(Mult(5, 20)), D(Gemes(10), CofreNormal()),
            };

            if (catalegNou) AssetDatabase.CreateAsset(cataleg, rutaCataleg);
            EditorUtility.SetDirty(cataleg);
            AssetDatabase.SaveAssets();
            Selection.activeObject = cataleg;
            Debug.Log($"Botiga: {productes.Count} productes i el calendari a {Carpeta}. Posa el catàleg al GestorDeBotiga.");
        }
    }
}
