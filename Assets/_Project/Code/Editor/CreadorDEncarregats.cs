using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // Genera els assets d'encarregats i el catàleg d'una tacada, per no
    // haver-ne de crear setze a mà amb sis camps cadascun.
    //
    // Menú: IdleFrogs > Crear encarregats de prova
    //
    // Quatre esglaons per efecte, un per raresa: Comú, Rar, Èpic i
    // Llegendari. Els percentatges de raresa són al catàleg.
    public static class CreadorDEncarregats
    {
        private const string Carpeta = "Assets/_Project/Data/Encarregats";

        private struct Recepta
        {
            public TipusDeLloc lloc;
            public TipusDEfecte efecte;
            public string nom;
            public double[] perRaresa;   // Comú, Rar, Èpic, Llegendari

            public Recepta(TipusDeLloc l, TipusDEfecte e, string n, params double[] m)
            {
                lloc = l; efecte = e; nom = n; perRaresa = m;
            }
        }

        private static readonly Recepta[] Receptes =
        {
            new Recepta(TipusDeLloc.Panta, TipusDEfecte.VelocitatDeMoviment, "Saltador",   2.0, 3.5, 5.5, 7.5),
            new Recepta(TipusDeLloc.Panta, TipusDEfecte.VelocitatDeTreball,  "Llengut",    2.0, 3.0, 5.0, 7.0),
            new Recepta(TipusDeLloc.Panta, TipusDEfecte.ReduccioDeCost,      "Comptable",  0.2, 0.4, 0.7, 0.8),

            new Recepta(TipusDeLloc.Tren,  TipusDEfecte.VelocitatDeMoviment, "Maquinista", 1.5, 2.0, 4.0, 6.0),
            new Recepta(TipusDeLloc.Tren,  TipusDEfecte.VelocitatDeTreball,  "Carregador", 1.5, 2.0, 4.0, 6.0),
            new Recepta(TipusDeLloc.Tren,  TipusDEfecte.Capacitat,           "Vagoner",    2.0, 3.0, 5.0, 7.0),
            new Recepta(TipusDeLloc.Tren,  TipusDEfecte.ReduccioDeCost,      "Cap d'estació", 0.2, 0.4, 0.7, 0.8),

            new Recepta(TipusDeLloc.Tenda, TipusDEfecte.VelocitatDeMoviment, "Corredor",   2.0, 3.0, 5.0, 7.0),
            new Recepta(TipusDeLloc.Tenda, TipusDEfecte.VelocitatDeTreball,  "Empaquetador", 2.0, 3.0, 5.0, 8.0),
            new Recepta(TipusDeLloc.Tenda, TipusDEfecte.Capacitat,           "Cisteller",  2.0, 3.0, 5.0, 8.0),
            new Recepta(TipusDeLloc.Tenda, TipusDEfecte.ReduccioDeCost,      "Regatejador", 0.2, 0.4, 0.7, 0.8),
        };

        // Com més rar, més dura l'habilitat i més triga a recarregar-se.
        private static readonly float[] Duracio  = { 30f, 60f, 180f, 600f };
        private static readonly float[] Recarrega = { 180f, 300f, 900f, 3000f };
        private static readonly double[] MagnitudHabilitat = { 2, 3, 5, 8 };

        [MenuItem("IdleFrogs/Crear encarregats de prova")]
        public static void Crear()
        {
            System.IO.Directory.CreateDirectory(Carpeta);

            var creats = new List<EncarregatSO>();

            foreach (Recepta r in Receptes)
            {
                for (int i = 0; i < 4; i++)
                {
                    var raresa = (Raresa)i;
                    string id = $"{r.lloc}_{r.efecte}_{raresa}".ToLowerInvariant();
                    string ruta = $"{Carpeta}/{id}.asset";

                    EncarregatSO actiu = AssetDatabase.LoadAssetAtPath<EncarregatSO>(ruta);
                    bool esNou = actiu == null;
                    if (esNou) actiu = ScriptableObject.CreateInstance<EncarregatSO>();

                    actiu.id = id;
                    actiu.nomVisible = $"{r.nom} {raresa}";
                    actiu.lloc = r.lloc;
                    actiu.raresa = raresa;
                    actiu.efecte = r.efecte;
                    actiu.magnitud = r.perRaresa[i];
                    actiu.magnitudDeLHabilitat = MagnitudHabilitat[i];
                    actiu.segonsDeDuracio = Duracio[i];
                    actiu.segonsDeRecarrega = Recarrega[i];

                    if (esNou) AssetDatabase.CreateAsset(actiu, ruta);
                    else EditorUtility.SetDirty(actiu);

                    creats.Add(actiu);
                }
            }

            const string rutaCataleg = Carpeta + "/CatalegDEncarregats.asset";
            CatalegDEncarregats cataleg = AssetDatabase.LoadAssetAtPath<CatalegDEncarregats>(rutaCataleg);
            bool catalegNou = cataleg == null;
            if (catalegNou) cataleg = ScriptableObject.CreateInstance<CatalegDEncarregats>();

            cataleg.encarregats = creats.ToArray();
            if (catalegNou) AssetDatabase.CreateAsset(cataleg, rutaCataleg);
            else EditorUtility.SetDirty(cataleg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"CreadorDEncarregats: {creats.Count} encarregats i el catàleg a {Carpeta}");
            Selection.activeObject = cataleg;
        }
    }
}
