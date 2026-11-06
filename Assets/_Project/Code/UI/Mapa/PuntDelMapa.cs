using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    public enum AccioDelMapa { Botiga, Missions, Esdeveniments, Prestigi, Recerca, Arena }

    // Un lloc del mapa que obre alguna cosa: la ciutat comercial (la botiga),
    // el cartell de les missions diàries, els esdeveniments, el prestigi...
    // Els que encara no existeixen diuen "Aviat". Necessita un collider.
    public class PuntDelMapa : MonoBehaviour, IEsPotTocar
    {
        [SerializeField] private AccioDelMapa accio;
        [Tooltip("Opcional: el nom (s'omple sol en l'idioma del jugador)")]
        [SerializeField] private TMP_Text textNom;
        [Tooltip("Opcional: el punt vermell (a la botiga, quan hi ha alguna cosa gratis per recollir)")]
        [SerializeField] private GameObject puntDAvis;

        private GestorDelMapa _mapa;

        public AccioDelMapa Accio => accio;

        public void Inicialitzar(GestorDelMapa mapa) => _mapa = mapa;

        public void EnTocar()
        {
            if (_mapa != null) _mapa.Obrir(accio);
        }

        public void Refrescar()
        {
            if (textNom != null) textNom.text = Textos.T(accio switch
            {
                AccioDelMapa.Botiga => "mapa.ciutat",
                AccioDelMapa.Missions => "mapa.missions",
                AccioDelMapa.Esdeveniments => "mapa.esdeveniments",
                AccioDelMapa.Recerca => "mapa.recerca",
                AccioDelMapa.Arena => "mapa.arena",
                _ => "mapa.prestigi"
            });

            bool avis = accio == AccioDelMapa.Botiga && GestorDeBotiga.Instancia != null && GestorDeBotiga.Instancia.HiHaGratisPerRecollir;
            if (puntDAvis != null && puntDAvis.activeSelf != avis) puntDAvis.SetActive(avis);
        }
    }
}
