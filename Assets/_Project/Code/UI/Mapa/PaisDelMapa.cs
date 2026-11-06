using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un país dibuixat al mapa. Necessita un collider (millor un
    // PolygonCollider2D: Unity en treu la forma del dibuix i només es toca
    // sobre el país). Totes les peces són opcionals.
    public class PaisDelMapa : MonoBehaviour, IEsPotTocar
    {
        [SerializeField] private PaisSO pais;

        [Header("Dibuix")]
        [Tooltip("Buit: el SpriteRenderer d'aquest objecte")]
        [SerializeField] private SpriteRenderer dibuix;
        [SerializeField] private Color colorBloquejat = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Peces (opcionals; textos del món: TextMeshPro, no UGUI)")]
        [SerializeField] private TMP_Text textNom;
        [SerializeField] private GameObject cadenat;
        [Tooltip("El preu del següent país, a sobre del cadenat")]
        [SerializeField] private TMP_Text textPreu;
        [Tooltip("S'encén al país on ets")]
        [SerializeField] private GameObject marcDeLActual;

        [Header("El que ha guanyat mentre no hi eres")]
        [Tooltip("El cartellet amb els guanys i el botó de recollir (BotoRecollirDelMapa). S'amaga si no hi ha res")]
        [SerializeField] private GameObject arrelDeGuanys;
        [SerializeField] private TMP_Text textGuanys;

        private GestorDelMapa _mapa;
        private Color _colorNormal = Color.white;

        public PaisSO Pais => pais;

        private void Awake()
        {
            if (dibuix == null) dibuix = GetComponent<SpriteRenderer>();
            if (dibuix != null) _colorNormal = dibuix.color;
        }

        public void Inicialitzar(GestorDelMapa mapa) => _mapa = mapa;

        public void EnTocar()
        {
            if (_mapa != null) _mapa.EnTocarPais(this);
        }

        public void Refrescar()
        {
            GestorDePaisos g = GestorDePaisos.Instancia;
            GameManager gm = GameManager.Instancia;
            if (pais == null || g == null) return;

            bool ciutat = pais.esCiutatComercial;
            bool obert = g.EstaDesbloquejat(pais);
            bool seguent = g.EsElSeguent(pais);
            int zona = g.ZonaDe(pais);
            bool actual = !ciutat && gm != null && zona == gm.Zona;

            if (dibuix != null) dibuix.color = obert ? _colorNormal : colorBloquejat;
            if (textNom != null) textNom.text = pais.NomVisible;
            Activar(cadenat, !obert);
            Activar(marcDeLActual, actual);

            if (textPreu != null)
            {
                Activar(textPreu.gameObject, seguent);
                // Es paga amb la moneda del país on ets.
                if (seguent) textPreu.text = FormatadorDeNombres.Doblers(pais.preuDeDesbloqueig) + Icones.MonedaDe(g.PaisActual);
            }

            double guanys = !ciutat && obert && gm != null ? gm.GuanysPendents(zona) : 0;
            Activar(arrelDeGuanys, guanys > 0);
            if (guanys > 0 && textGuanys != null)
                textGuanys.text = $"+{FormatadorDeNombres.Doblers(guanys)}{Icones.MonedaDe(pais)}";
        }

        private static void Activar(GameObject go, bool si)
        {
            if (go != null && go.activeSelf != si) go.SetActive(si);
        }
    }
}
