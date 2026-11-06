using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una mica de la gespa del país al fons del panell del prestigi: les
    // capes de la seva decoració que van per sota de l'aigua (gespa, flors),
    // amb la mateixa llavor, escampades per tota la pantalla menys on hi ha
    // el pantà. Els arbres i les canyes (per sobre de l'aigua) no hi surten.
    //
    // Va en un objecte que ocupi tota la pantalla, just després del Fons:
    // tot el que hi ha després (panells, botons) queda per sobre.
    [RequireComponent(typeof(RectTransform))]
    public class GespaDelPrestigi : MonoBehaviour
    {
        [Tooltip("Els països sense decoració pròpia (la del país 1)")]
        [SerializeField] private PerfilDeDecoracio perfilPerDefecte;
        [Tooltip("El que no ha de quedar tapat: el pantà")]
        [SerializeField] private RectTransform[] aEvitar = new RectTransform[0];
        [Tooltip("Espai de més al voltant del que s'evita, en píxels de referència")]
        [Min(0)] [SerializeField] private float marge = 20f;
        [Tooltip("Píxels per unitat del món: com de grans surten les herbes respecte al joc")]
        [Min(1)] [SerializeField] private float pixelsPerUnitat = 100f;
        [Tooltip("1 = la mateixa densitat que al joc")]
        [Min(0)] [SerializeField] private float quantitat = 1f;

        private readonly List<Image> _imatges = new List<Image>();
        private PaisSO _mostrat;
        private bool _fet;

        // Es torna a escampar només si canvia el país.
        public void Mostrar(PaisSO pais)
        {
            if (_fet && pais == _mostrat) return;
            _fet = true;
            _mostrat = pais;
            Escampar(pais != null && pais.decoracio != null ? pais.decoracio : perfilPerDefecte);
        }

        private void Escampar(PerfilDeDecoracio perfil)
        {
            int usades = 0;
            var rt = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            Rect zona = rt.rect;

            var evitar = new List<Rect>();
            foreach (RectTransform r in aEvitar)
                if (r != null) evitar.Add(RectLocal(r, rt));

            if (perfil != null && perfil.capes != null)
            {
                // De la de més avall a la de més amunt, com al joc.
                var capes = new List<CapaDeDecoracio>(perfil.capes);
                capes.Sort((a, b) => a.ordreDeDibuix.CompareTo(b.ordreDeDibuix));
                foreach (CapaDeDecoracio capa in capes)
                {
                    // Només el que va per sota de l'aigua (el fons dels pantans és -50).
                    if (capa == null || capa.ordreDeDibuix >= -50 || capa.variants == null || capa.variants.Length == 0) continue;
                    usades = EscamparCapa(capa, zona, evitar, usades);
                }
            }

            for (int i = usades; i < _imatges.Count; i++) _imatges[i].gameObject.SetActive(false);
        }

        private int EscamparCapa(CapaDeDecoracio capa, Rect zona, List<Rect> evitar, int usades)
        {
            var planes = new VariantPlana[capa.variants.Length];
            for (int i = 0; i < planes.Length; i++)
            {
                VariantDeDecoracio v = capa.variants[i];
                Sprite s = v != null ? v.sprite : null;
                Bounds b = s != null ? s.bounds : new Bounds();
                planes[i] = new VariantPlana
                {
                    // Sense sprite no surt mai.
                    pes = s != null ? v.pes : 0,
                    escalaMinima = v != null ? v.escalaMinima : 1,
                    escalaMaxima = v != null ? v.escalaMaxima : 1,
                    girarALAtzar = v != null && v.girarALAtzar,
                    limits = new Rect(b.min.x * pixelsPerUnitat, b.min.y * pixelsPerUnitat,
                                      b.size.x * pixelsPerUnitat, b.size.y * pixelsPerUnitat),
                };
            }

            float area = zona.width * zona.height / (pixelsPerUnitat * pixelsPerUnitat);
            int n = Mathf.RoundToInt(capa.densitat * area * quantitat);
            List<Colocacio> colocacions = PlanificadorDeDecoracio.Planificar(
                capa.llavor, zona, n, planes, evitar, marge, capa.distanciaMinima * pixelsPerUnitat);

            // Les de més avall, davant.
            int[] ordres = PlanificadorDeDecoracio.OrdresDeDibuix(colocacions);
            var perOrdre = new int[colocacions.Count];
            for (int i = 0; i < ordres.Length; i++) perOrdre[ordres[i]] = i;

            foreach (int i in perOrdre)
            {
                Colocacio c = colocacions[i];
                Sprite s = capa.variants[c.variant].sprite;
                Image img = Imatge(usades++);
                img.sprite = s;
                RectTransform r = img.rectTransform;
                r.SetAsLastSibling();
                r.pivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
                r.sizeDelta = (Vector2)s.bounds.size * pixelsPerUnitat;
                r.anchoredPosition = c.posicio;
                r.localScale = new Vector3(c.girada ? -c.escala : c.escala, c.escala, 1f);
            }
            return usades;
        }

        private Image Imatge(int i)
        {
            if (i < _imatges.Count)
            {
                _imatges[i].gameObject.SetActive(true);
                return _imatges[i];
            }
            var go = new GameObject("Herba", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var r = (RectTransform)go.transform;
            // Posicions des del centre de la pantalla, com el Rect de la zona.
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            _imatges.Add(img);
            return img;
        }

        // El rectangle d'un element, en les coordenades d'aquest objecte.
        private static Rect RectLocal(RectTransform r, RectTransform en)
        {
            var c = new Vector3[4];
            r.GetWorldCorners(c);
            Vector2 a = en.InverseTransformPoint(c[0]);
            Vector2 b = en.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }
}
