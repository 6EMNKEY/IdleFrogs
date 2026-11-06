using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Invocar lluitadors del país amb tickets: comú (D–A) o prèmium (D–SSS),
    // d'un en un o de deu en deu. El que surt es veu en cartes a sota.
    public class PanellDelGacha : MonoBehaviour
    {
        [SerializeField] private GameObject arrel;
        [SerializeField] private TMP_Text textTicketsComuns;
        [SerializeField] private TMP_Text textTicketsPremium;
        [SerializeField] private Button botoComuX1;
        [SerializeField] private Button botoComuX10;
        [SerializeField] private Button botoPremiumX1;
        [SerializeField] private Button botoPremiumX10;
        [Tooltip("On surten les cartes del que t'ha tocat")]
        [SerializeField] private RectTransform resultats;
        [Tooltip("La carta de mostra, apagada")]
        [SerializeField] private TargetaDeLluitador plantilla;
        [SerializeField] private TMP_Text textMissatge;
        [SerializeField] private Button botoTancar;

        private readonly List<TargetaDeLluitador> _cartes = new List<TargetaDeLluitador>();
        private int _zona = -1;

        public bool EstaObert => arrel != null && arrel.activeSelf;

        private void Awake()
        {
            if (plantilla != null) plantilla.gameObject.SetActive(false);
            if (botoComuX1 != null) botoComuX1.onClick.AddListener(() => Tirar(false, 1));
            if (botoComuX10 != null) botoComuX10.onClick.AddListener(() => Tirar(false, 10));
            if (botoPremiumX1 != null) botoPremiumX1.onClick.AddListener(() => Tirar(true, 1));
            if (botoPremiumX10 != null) botoPremiumX10.onClick.AddListener(() => Tirar(true, 10));
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (arrel != null) arrel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GestorDeLArena.Instancia != null) GestorDeLArena.Instancia.EnCanvi += RefrescarTickets;
        }

        private void OnDisable()
        {
            if (GestorDeLArena.Instancia != null) GestorDeLArena.Instancia.EnCanvi -= RefrescarTickets;
        }

        public void Obrir(int zona)
        {
            _zona = zona;
            GestorDAudio.Sonar(So.Obrir);
            if (arrel != null)
            {
                arrel.SetActive(true);
                arrel.transform.SetAsLastSibling();
            }
            Mostrar(null);
            if (textMissatge != null) textMissatge.text = string.Empty;
            RefrescarTickets();
        }

        public void Tancar()
        {
            if (arrel != null && arrel.activeSelf) GestorDAudio.Sonar(So.Tancar);
            if (arrel != null) arrel.SetActive(false);
        }

        private void RefrescarTickets()
        {
            GestorDeLArena g = GestorDeLArena.Instancia;
            if (g == null) return;
            if (textTicketsComuns != null) textTicketsComuns.text = Textos.F("arena.tickets_comuns", g.TicketsComuns);
            if (textTicketsPremium != null) textTicketsPremium.text = Textos.F("arena.tickets_premium", g.TicketsPremium);
            CompraAmbGemes.Pintar(botoComuX1, g.TicketsComuns >= 1);
            CompraAmbGemes.Pintar(botoComuX10, g.TicketsComuns >= 10);
            CompraAmbGemes.Pintar(botoPremiumX1, g.TicketsPremium >= 1);
            CompraAmbGemes.Pintar(botoPremiumX10, g.TicketsPremium >= 10);
        }

        private void Tirar(bool premium, int quantes)
        {
            GestorDeLArena g = GestorDeLArena.Instancia;
            if (g == null || _zona < 0) return;
            if (g.Tickets(premium) < quantes)
            {
                GestorDAudio.Sonar(So.Error);
                if (textMissatge != null) textMissatge.text = Textos.T("arena.sense_tickets");
                return;
            }
            List<LluitadorGuardat> nous = g.Tirar(_zona, premium, quantes);
            GestorDAudio.Sonar(nous.Count > 0 ? So.Recompensa : So.Error);
            if (textMissatge != null) textMissatge.text = string.Empty;
            Mostrar(nous);
        }

        private void Mostrar(List<LluitadorGuardat> nous)
        {
            int n = nous != null ? nous.Count : 0;
            while (_cartes.Count < n)
            {
                TargetaDeLluitador c = Instantiate(plantilla, resultats);
                _cartes.Add(c);
            }
            for (int i = 0; i < _cartes.Count; i++)
            {
                bool hi = i < n;
                _cartes[i].gameObject.SetActive(hi);
                if (hi) _cartes[i].Omplir(nous[i], _zona);
            }
        }
    }
}
