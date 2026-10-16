using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // El que el planificador necessita saber d'una variant, sense Sprite:
    // així es pot provar sense assets.
    public struct VariantPlana
    {
        public float pes;
        public float escalaMinima;
        public float escalaMaxima;
        public bool girarALAtzar;
        // Els límits del dibuix a escala 1, respecte al pivot (Sprite.bounds).
        public Rect limits;
    }

    public struct Colocacio
    {
        public int variant;
        public Vector2 posicio;
        public float escala;
        public bool girada;
        public Rect requadre;   // on queda el dibuix, en coordenades del món
    }

    // On va cada herba i cada flor. Tot surt d'un System.Random amb la
    // llavor: la mateixa llavor amb els mateixos paràmetres dona sempre el
    // mateix camp, i per això es pot desar la que t'agradi.
    public static class PlanificadorDeDecoracio
    {
        // Intents per element abans de rendir-se: amb molta zona a evitar o
        // una distància mínima gran no hi caben tots, i no s'ha de penjar.
        public const int IntentsPerElement = 30;

        public static List<Colocacio> Planificar(
            int llavor, Rect zona, int quantitat, VariantPlana[] variants,
            IList<Rect> aEvitar, float marge, float distanciaMinima)
        {
            var resultat = new List<Colocacio>();
            if (variants == null || variants.Length == 0 || quantitat <= 0) return resultat;

            float pesTotal = 0;
            foreach (VariantPlana v in variants) pesTotal += Mathf.Max(0, v.pes);
            if (pesTotal <= 0) return resultat;

            // Les zones a evitar ja engreixades amb el marge, un sol cop.
            var evitar = new List<Rect>();
            if (aEvitar != null)
                foreach (Rect r in aEvitar)
                    evitar.Add(new Rect(r.xMin - marge, r.yMin - marge, r.width + 2 * marge, r.height + 2 * marge));

            var atzar = new System.Random(llavor);
            var graella = new Dictionary<Vector2Int, List<Vector2>>();
            float cel = distanciaMinima > 0 ? distanciaMinima : 1f;

            int intents = quantitat * IntentsPerElement;
            for (int i = 0; i < intents && resultat.Count < quantitat; i++)
            {
                // Sempre es tiren els quatre nombres, encaixi o no: si no, què
                // surt després dependria de quins intents han fallat abans.
                int variant = Triar(variants, pesTotal, atzar.NextDouble());
                Vector2 posicio = new Vector2(
                    zona.xMin + (float)atzar.NextDouble() * zona.width,
                    zona.yMin + (float)atzar.NextDouble() * zona.height);
                double tirada = atzar.NextDouble();
                bool girada = atzar.NextDouble() < 0.5;

                VariantPlana v = variants[variant];
                float escala = Mathf.Lerp(v.escalaMinima, Mathf.Max(v.escalaMinima, v.escalaMaxima), (float)tirada);
                girada &= v.girarALAtzar;

                Rect requadre = Requadre(v.limits, posicio, escala, girada);
                if (Toca(requadre, evitar)) continue;
                if (distanciaMinima > 0 && MassaAProp(posicio, graella, cel, distanciaMinima)) continue;

                resultat.Add(new Colocacio { variant = variant, posicio = posicio, escala = escala, girada = girada, requadre = requadre });
                if (distanciaMinima > 0) Anotar(posicio, graella, cel);
            }

            return resultat;
        }

        // Els de més avall van davant: l'ordre de dibuix de cadascun, de 0
        // (el de més amunt, al fons) a n-1.
        public static int[] OrdresDeDibuix(List<Colocacio> colocacions)
        {
            int n = colocacions.Count;
            var index = new int[n];
            for (int i = 0; i < n; i++) index[i] = i;
            System.Array.Sort(index, (a, b) => colocacions[b].posicio.y.CompareTo(colocacions[a].posicio.y));

            var ordres = new int[n];
            for (int pos = 0; pos < n; pos++) ordres[index[pos]] = pos;
            return ordres;
        }

        public static Rect Requadre(Rect limits, Vector2 posicio, float escala, bool girada)
        {
            float xMin = girada ? -limits.xMax : limits.xMin;
            return new Rect(posicio.x + xMin * escala, posicio.y + limits.yMin * escala,
                            limits.width * escala, limits.height * escala);
        }

        private static int Triar(VariantPlana[] variants, float pesTotal, double tirada)
        {
            double acumulat = 0, objectiu = tirada * pesTotal;
            for (int i = 0; i < variants.Length; i++)
            {
                float pes = Mathf.Max(0, variants[i].pes);
                if (pes <= 0) continue;
                acumulat += pes;
                if (objectiu < acumulat) return i;
            }
            // Arrodoniment a l'últim decimal: l'última amb pes.
            for (int i = variants.Length - 1; i >= 0; i--)
                if (variants[i].pes > 0) return i;
            return 0;
        }

        private static bool Toca(Rect requadre, List<Rect> evitar)
        {
            foreach (Rect r in evitar)
                if (requadre.Overlaps(r)) return true;
            return false;
        }

        private static Vector2Int Cela(Vector2 p, float cel) =>
            new Vector2Int(Mathf.FloorToInt(p.x / cel), Mathf.FloorToInt(p.y / cel));

        private static bool MassaAProp(Vector2 p, Dictionary<Vector2Int, List<Vector2>> graella, float cel, float distancia)
        {
            Vector2Int c = Cela(p, cel);
            float d2 = distancia * distancia;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (graella.TryGetValue(new Vector2Int(c.x + dx, c.y + dy), out List<Vector2> punts))
                        foreach (Vector2 q in punts)
                            if ((q - p).sqrMagnitude < d2) return true;
            return false;
        }

        private static void Anotar(Vector2 p, Dictionary<Vector2Int, List<Vector2>> graella, float cel)
        {
            Vector2Int c = Cela(p, cel);
            if (!graella.TryGetValue(c, out List<Vector2> punts))
                graella[c] = punts = new List<Vector2>();
            punts.Add(p);
        }
    }
}
