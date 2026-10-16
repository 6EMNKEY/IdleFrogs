using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleFrogs.Game
{
    [System.Serializable]
    public class VariantDeDecoracio
    {
        public Sprite sprite;
        [Tooltip("Proporció respecte a les altres: amb 3 i 1, tres de cada quatre són la primera")]
        [Min(0)] public float pes = 1;
        [Min(0.01f)] public float escalaMinima = 0.8f;
        [Min(0.01f)] public float escalaMaxima = 1.2f;
        [Tooltip("La meitat surten girades en horitzontal")]
        public bool girarALAtzar = true;
    }

    [System.Serializable]
    public class LlavorDesada
    {
        public int llavor;
        public string nota;
    }

    // Herbes i flors escampades a l'atzar per sobre del verd del fons i per
    // sota de tot el que hi ha al món. Es generen a l'editor (botons de
    // l'inspector) i queden guardades a l'escena com a objectes normals:
    // el joc no les genera en arrencar.
    //
    // Tot surt de la llavor: la mateixa llavor amb els mateixos paràmetres
    // torna el mateix camp. Canviar les variants o la zona el canvia.
    [RequireComponent(typeof(SortingGroup))]
    public class DecoracioDeGespa : MonoBehaviour
    {
        private const string NomDelContenidor = "Generades";
        private const int MaximDElements = 5000;

        [Header("Herbes i flors")]
        public VariantDeDecoracio[] variants;

        [Header("Zona — rectangle verd en seleccionar, respecte a aquest objecte")]
        [SerializeField] private Vector2 centreDeLaZona = Vector2.zero;
        [SerializeField] private Vector2 midaDeLaZona = new Vector2(20f, 12f);

        [Header("Quantitat")]
        [Tooltip("Elements per unitat quadrada de zona")]
        [Min(0)] public float densitat = 1f;
        [Tooltip("Distància mínima entre dos elements, perquè no s'amunteguin. 0 = sense límit")]
        [Min(0)] public float distanciaMinima = 0.3f;

        [Header("Què evitar — rectangles vermells en seleccionar")]
        [Tooltip("Pantans, tenda, estació... Es mira cada dibuix que tinguin a dins, també els amagats")]
        public GameObject[] aEvitar;
        [Tooltip("Espai de més al voltant del que s'evita, en unitats del món")]
        [Min(0)] public float marge = 0.2f;

        [Header("Ordre de dibuix — per sota de tot (el fons dels pantans és -50)")]
        [SerializeField] private string capaDeDibuix = "Default";
        [SerializeField] private int ordreDeDibuix = -100;

        [Header("Llavor")]
        public int llavor;
        [Tooltip("Les que t'han agradat. A l'inspector, 'Fer servir' la torna a generar")]
        public List<LlavorDesada> llavorsDesades = new List<LlavorDesada>();

        public Rect Zona => new Rect(
            (Vector2)transform.position + centreDeLaZona - midaDeLaZona / 2f, midaDeLaZona);

        // Torna quants n'ha pogut posar: amb molt a evitar poden ser menys
        // dels que demana la densitat.
        public int Generar()
        {
            AplicarOrdre();
            Transform contenidor = Contenidor();
            Buidar(contenidor);

            if (variants == null || variants.Length == 0) return 0;

            var planes = new VariantPlana[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                VariantDeDecoracio v = variants[i];
                bool valida = v != null && v.sprite != null;
                planes[i] = new VariantPlana
                {
                    pes = valida ? v.pes : 0,
                    escalaMinima = valida ? v.escalaMinima : 1,
                    escalaMaxima = valida ? v.escalaMaxima : 1,
                    girarALAtzar = valida && v.girarALAtzar,
                    limits = valida
                        ? new Rect(v.sprite.bounds.min.x, v.sprite.bounds.min.y, v.sprite.bounds.size.x, v.sprite.bounds.size.y)
                        : new Rect()
                };
            }

            Rect zona = Zona;
            int quantitat = Mathf.Min(MaximDElements, Mathf.RoundToInt(densitat * zona.width * zona.height));

            List<Colocacio> colocacions = PlanificadorDeDecoracio.Planificar(
                llavor, zona, quantitat, planes, RectanglesAEvitar(), marge, distanciaMinima);
            int[] ordres = PlanificadorDeDecoracio.OrdresDeDibuix(colocacions);

            for (int i = 0; i < colocacions.Count; i++)
            {
                Colocacio c = colocacions[i];
                VariantDeDecoracio v = variants[c.variant];

                var go = new GameObject(v.sprite.name);
                go.transform.SetParent(contenidor, false);
                go.transform.position = new Vector3(c.posicio.x, c.posicio.y, transform.position.z);
                go.transform.localScale = new Vector3(c.escala, c.escala, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = v.sprite;
                sr.flipX = c.girada;
                // Dins del Sorting Group: el de més avall va davant.
                sr.sortingOrder = ordres[i];
            }

            return colocacions.Count;
        }

        public void Esborrar() => Buidar(Contenidor());

        public int Quantitat
        {
            get
            {
                Transform t = transform.Find(NomDelContenidor);
                return t != null ? t.childCount : 0;
            }
        }

        // Tot el grup es dibuixa com un sol bloc amb aquest ordre, i a dins
        // cada element s'ordena pel seu. Així cap herba es fica entre els
        // objectes del món per molt alt que sigui el seu número.
        private void AplicarOrdre()
        {
            SortingGroup grup = GetComponent<SortingGroup>();
            if (grup == null) return;
            grup.sortingLayerName = capaDeDibuix;
            grup.sortingOrder = ordreDeDibuix;
        }

        private void OnValidate() => AplicarOrdre();

        private Transform Contenidor()
        {
            Transform t = transform.Find(NomDelContenidor);
            if (t != null) return t;
            var go = new GameObject(NomDelContenidor);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        private static void Buidar(Transform contenidor)
        {
            for (int i = contenidor.childCount - 1; i >= 0; i--)
                DestroyImmediate(contenidor.GetChild(i).gameObject);
        }

        // Un rectangle per cada dibuix de dins de cada objecte, no un de sol
        // que els englobi: si arrossegues la llista de pantans sencera,
        // l'espai entre pantà i pantà ha de quedar lliure per a herbes.
        public List<Rect> RectanglesAEvitar()
        {
            var rects = new List<Rect>();
            if (aEvitar == null) return rects;

            foreach (GameObject go in aEvitar)
            {
                if (go == null) continue;
                // true: també els amagats. Un pantà sense comprar té el
                // contingut apagat però el lloc ja és seu.
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.transform.IsChildOf(transform)) continue;
                    if (TryLimits(r, out Rect rect)) rects.Add(rect);
                }
            }
            return rects;
        }

        // Els SpriteRenderer es calculen a mà: Renderer.bounds d'un objecte
        // apagat no és fiable.
        private static bool TryLimits(Renderer r, out Rect rect)
        {
            rect = default;
            if (r is SpriteRenderer sr)
            {
                if (sr.sprite == null) return false;
                Bounds b = sr.sprite.bounds;
                Matrix4x4 m = sr.transform.localToWorldMatrix;
                Vector3 a = m.MultiplyPoint3x4(new Vector3(b.min.x, b.min.y));
                Vector3 c = m.MultiplyPoint3x4(new Vector3(b.max.x, b.max.y));
                Vector3 d = m.MultiplyPoint3x4(new Vector3(b.min.x, b.max.y));
                Vector3 e = m.MultiplyPoint3x4(new Vector3(b.max.x, b.min.y));
                float xMin = Mathf.Min(Mathf.Min(a.x, c.x), Mathf.Min(d.x, e.x));
                float xMax = Mathf.Max(Mathf.Max(a.x, c.x), Mathf.Max(d.x, e.x));
                float yMin = Mathf.Min(Mathf.Min(a.y, c.y), Mathf.Min(d.y, e.y));
                float yMax = Mathf.Max(Mathf.Max(a.y, c.y), Mathf.Max(d.y, e.y));
                rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                return rect.width > 0 && rect.height > 0;
            }

            Bounds limits = r.bounds;
            if (limits.size.x <= 0 || limits.size.y <= 0) return false;
            rect = Rect.MinMaxRect(limits.min.x, limits.min.y, limits.max.x, limits.max.y);
            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Rect z = Zona;
            Gizmos.color = new Color(0.3f, 1f, 0.4f);
            Gizmos.DrawWireCube(z.center, z.size);

            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            foreach (Rect r in RectanglesAEvitar())
                Gizmos.DrawWireCube(r.center, new Vector3(r.width + 2 * marge, r.height + 2 * marge));
        }
#endif
    }
}
