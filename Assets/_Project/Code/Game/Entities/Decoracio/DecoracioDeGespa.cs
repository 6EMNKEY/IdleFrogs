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

    // Herbes, flors, arbres... escampats a l'atzar pel món. El que s'escampa
    // ho diu un PerfilDeDecoracio (un per país, amb capes); el lloc on
    // s'escampa i el que s'ha d'evitar són d'aquesta escena i viuen aquí.
    //
    // En joc, en carregar el país es posa el seu perfil: el camp fornejat si
    // en té, si no es genera capa per capa amb les llavors. A l'editor el que
    // es genera és una previsualització que no es desa amb l'escena: l'escena
    // no carrega milers d'herbes i cada país té el seu camp.
    //
    // Tot surt de la llavor: la mateixa llavor amb els mateixos paràmetres
    // torna el mateix camp.
    public class DecoracioDeGespa : MonoBehaviour
    {
        public const string NomDelContenidor = "Generades";
        private const int MaximDElementsPerCapa = 4000;  // el Sorting Group no en suporta més de 4095

        [Header("Perfil")]
        [Tooltip("El que es previsualitza a l'editor. En joc, el del país; aquest només si el país no en té")]
        public PerfilDeDecoracio perfil;

        [Header("Zona — rectangle verd en seleccionar, respecte a aquest objecte")]
        [SerializeField] private Vector2 centreDeLaZona = Vector2.zero;
        [SerializeField] private Vector2 midaDeLaZona = new Vector2(20f, 12f);

        [Header("Què evitar — rectangles vermells en seleccionar")]
        [Tooltip("Pantans, restaurant, estació... Cada capa diu quins dibuixos de dins compten (Noms A Evitar)")]
        public GameObject[] aEvitar;

        public Rect Zona => new Rect(
            (Vector2)transform.position + centreDeLaZona - midaDeLaZona / 2f, midaDeLaZona);

        public PerfilDeDecoracio PerfilPer(PaisSO pais) =>
            pais != null && pais.decoracio != null ? pais.decoracio : perfil;

        // Després de GestorDePaisos i GameManager: ja se sap on som.
        private void Awake()
        {
            if (!Application.isPlaying) return;
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            Posar(PerfilPer(pais));
        }

        // El del joc: el camp fornejat si n'hi ha, si no generat. Torna
        // quants elements hi ha posat.
        public int Posar(PerfilDeDecoracio p)
        {
            if (p != null && p.campFornejat != null)
            {
                Esborrar();
                GameObject camp = Instantiate(p.campFornejat, transform, false);
                camp.name = NomDelContenidor;
                if (!Application.isPlaying) MarcarComAPrevisualitzacio(camp);
                return camp.GetComponentsInChildren<SpriteRenderer>(true).Length;
            }
            return Generar(p);
        }

        // Sempre a partir de les llavors, encara que el perfil tingui camp
        // fornejat: és el que fa servir l'editor per tornar a començar.
        public int Generar(PerfilDeDecoracio p)
        {
            Esborrar();
            if (p == null || p.capes == null) return 0;

            var contenidor = new GameObject(NomDelContenidor).transform;
            contenidor.SetParent(transform, false);

            int total = 0;
            foreach (CapaDeDecoracio capa in p.capes)
                if (capa != null) total += GenerarCapa(capa, contenidor);

            if (!Application.isPlaying) MarcarComAPrevisualitzacio(contenidor.gameObject);
            return total;
        }

        // Cada capa en el seu Sorting Group: es dibuixa com un bloc amb el seu
        // ordre, i a dins el de més avall va davant.
        private int GenerarCapa(CapaDeDecoracio capa, Transform pare)
        {
            var arrel = new GameObject($"Capa: {capa.nom}");
            arrel.transform.SetParent(pare, false);
            SortingGroup grup = arrel.AddComponent<SortingGroup>();
            grup.sortingLayerName = string.IsNullOrEmpty(capa.capaDeDibuix) ? "Default" : capa.capaDeDibuix;
            grup.sortingOrder = capa.ordreDeDibuix;

            VariantDeDecoracio[] variants = capa.variants;
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
            int quantitat = Mathf.Min(MaximDElementsPerCapa, Mathf.RoundToInt(capa.densitat * zona.width * zona.height));

            List<Colocacio> colocacions = PlanificadorDeDecoracio.Planificar(
                capa.llavor, zona, quantitat, planes, RectanglesAEvitar(capa.nomsAEvitar), capa.marge, capa.distanciaMinima);
            int[] ordres = PlanificadorDeDecoracio.OrdresDeDibuix(colocacions);

            for (int i = 0; i < colocacions.Count; i++)
            {
                Colocacio c = colocacions[i];
                VariantDeDecoracio v = variants[c.variant];

                var go = new GameObject(v.sprite.name);
                go.transform.SetParent(arrel.transform, false);
                go.transform.position = new Vector3(c.posicio.x, c.posicio.y, transform.position.z);
                go.transform.localScale = new Vector3(c.escala, c.escala, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = v.sprite;
                sr.flipX = c.girada;
                sr.sortingOrder = ordres[i];
            }
            return colocacions.Count;
        }

        public int Quantitat
        {
            get
            {
                Transform t = transform.Find(NomDelContenidor);
                return t != null ? t.GetComponentsInChildren<SpriteRenderer>(true).Length : 0;
            }
        }

        public Transform Contenidor => transform.Find(NomDelContenidor);

        // Treu el camp que hi hagi (també el d'abans d'aquest sistema, que es
        // desava amb l'escena) i el Sorting Group que l'objecte duia abans:
        // ara cada capa té el seu i un de comú les ajuntaria totes.
        public void Esborrar()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject fill = transform.GetChild(i).gameObject;
                if (fill.name != NomDelContenidor) continue;
                if (Application.isPlaying)
                {
                    fill.SetActive(false);
                    fill.transform.SetParent(null, false);
                    Destroy(fill);
                }
                else DestroyImmediate(fill);
            }

            if (TryGetComponent(out SortingGroup grupAntic))
            {
                if (Application.isPlaying) Destroy(grupAntic);
                else DestroyImmediate(grupAntic);
            }
        }

        // A l'editor el camp és només per mirar: no es desa amb l'escena.
        private static void MarcarComAPrevisualitzacio(GameObject arrel)
        {
            foreach (Transform t in arrel.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
        }

        // Un rectangle per cada dibuix de dins de cada objecte, no un de sol
        // que els englobi: si arrossegues la llista de pantans sencera,
        // l'espai entre pantà i pantà ha de quedar lliure per a herbes.
        // Amb noms, només els dibuixos que es diguin així.
        public List<Rect> RectanglesAEvitar(string[] noms)
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
                    if (noms != null && noms.Length > 0 && System.Array.IndexOf(noms, r.gameObject.name) < 0) continue;
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
        // Els rectangles vermells són els de la primera capa del perfil que es
        // previsualitza (normalment la gespa, la que més evita).
        private void OnDrawGizmosSelected()
        {
            Rect z = Zona;
            Gizmos.color = new Color(0.3f, 1f, 0.4f);
            Gizmos.DrawWireCube(z.center, z.size);

            CapaDeDecoracio capa = perfil != null && perfil.capes != null && perfil.capes.Count > 0 ? perfil.capes[0] : null;
            float marge = capa != null ? capa.marge : 0f;
            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            foreach (Rect r in RectanglesAEvitar(capa != null ? capa.nomsAEvitar : null))
                Gizmos.DrawWireCube(r.center, new Vector3(r.width + 2 * marge, r.height + 2 * marge));
        }
#endif
    }
}
