using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una pestanya de la botiga: icona i nom (amb TextLocalitzat) es posen a
    // l'editor. Tocar-la llisca fins a la seva secció.
    [RequireComponent(typeof(Button))]
    public class PestanyaDeBotiga : MonoBehaviour
    {
        [SerializeField] private SeccioDeBotiga seccio;
        [Tooltip("S'encén a la pestanya de la secció que es veu")]
        [SerializeField] private GameObject marcSeleccionada;
        [Tooltip("Opcional: el punt vermell (a Gratis, quan hi ha alguna cosa per recollir)")]
        [SerializeField] private GameObject puntDAvis;

        private PanellDeBotiga _panell;

        public SeccioDeBotiga Seccio => seccio;

        public void Inicialitzar(PanellDeBotiga panell)
        {
            _panell = panell;
            GetComponent<Button>().onClick.AddListener(() => _panell.AnarA(seccio));
            Seleccionar(false);
            PosarAvis(false);
        }

        public void Seleccionar(bool si)
        {
            if (marcSeleccionada != null && marcSeleccionada.activeSelf != si) marcSeleccionada.SetActive(si);
        }

        public void PosarAvis(bool si)
        {
            if (puntDAvis != null && puntDAvis.activeSelf != si) puntDAvis.SetActive(si);
        }
    }
}
