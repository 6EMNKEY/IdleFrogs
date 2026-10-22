using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un país del mapa: un botó posat damunt de la imatge. Bloquejat es veu
    // més gris; el següent que es pot comprar ensenya el cadenat i el preu.
    [RequireComponent(typeof(Button))]
    public class BotoDePais : MonoBehaviour
    {
        [SerializeField] private PaisSO pais;

        [Header("Peces (totes opcionals)")]
        [Tooltip("El dibuix del país: es posa més gris si està bloquejat")]
        [SerializeField] private Image dibuix;
        [SerializeField] private GameObject cadenat;
        [Tooltip("El preu, a sobre del cadenat")]
        [SerializeField] private TMP_Text textPreu;
        [SerializeField] private TMP_Text textNom;
        [Tooltip("S'encén al país on ets ara")]
        [SerializeField] private GameObject marcDeLActual;

        [Header("Aspecte bloquejat")]
        [SerializeField] private Color colorBloquejat = new Color(0.55f, 0.55f, 0.55f, 1f);

        private Button _boto;
        private PanellDelMapa _mapa;

        public PaisSO Pais => pais;

        private void Awake()
        {
            _boto = GetComponent<Button>();
            if (dibuix == null) dibuix = GetComponent<Image>();
            _boto.onClick.AddListener(() => { if (_mapa != null) _mapa.EnTocarPais(this); });
        }

        public void Inicialitzar(PanellDelMapa mapa) => _mapa = mapa;

        public void Refrescar(GestorDePaisos gestor)
        {
            if (pais == null || gestor == null) return;

            bool obert = gestor.EstaDesbloquejat(pais);
            bool seguent = gestor.EsElSeguent(pais);

            if (dibuix != null) dibuix.color = obert ? Color.white : colorBloquejat;
            if (cadenat != null) cadenat.SetActive(seguent);
            if (textPreu != null)
            {
                textPreu.gameObject.SetActive(seguent);
                if (seguent) textPreu.text = FormatadorDeNombres.Doblers(pais.preuDeDesbloqueig) + Icones.Moneda;
            }
            if (textNom != null) textNom.text = pais.NomVisible;
            if (marcDeLActual != null) marcDeLActual.SetActive(!pais.esCiutatComercial && gestor.PaisActual == pais);
        }
    }
}
