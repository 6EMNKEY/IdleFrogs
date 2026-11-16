using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un botó fix del HUD del mapa (inventari, missions, prestigi): fa el
    // mateix que un lloc del mapa, però no es mou amb la càmera.
    [RequireComponent(typeof(Button))]
    public class BotoDelMapa : MonoBehaviour
    {
        [SerializeField] private AccioDelMapa accio;
        [Tooltip("Opcional: el nom a sota (s'omple sol en l'idioma del jugador)")]
        [SerializeField] private TMP_Text textNom;
        [Tooltip("Opcional: el punt vermell (a la botiga, quan hi ha alguna cosa gratis per recollir)")]
        [SerializeField] private GameObject puntDAvis;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Tocar);
            if (!PuntDelMapa.Disponible(accio)) gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Textos.EnCanviDIdioma += Refrescar;
            Refrescar();
        }

        private void OnDisable() => Textos.EnCanviDIdioma -= Refrescar;

        private void Update()
        {
            if (puntDAvis == null) return;
            bool avis = accio == AccioDelMapa.Botiga && GestorDeBotiga.Instancia != null && GestorDeBotiga.Instancia.HiHaGratisPerRecollir;
            if (puntDAvis.activeSelf != avis) puntDAvis.SetActive(avis);
        }

        private void Tocar()
        {
            if (GestorDelMapa.Instancia != null) GestorDelMapa.Instancia.Obrir(accio);
        }

        private void Refrescar()
        {
            if (textNom != null) textNom.text = Textos.T(PuntDelMapa.ClauDelNom(accio));
        }
    }
}
