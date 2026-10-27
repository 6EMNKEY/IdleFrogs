using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La recompensa diària, la primera targeta de la secció Gratis.
    public class TargetaDelCalendari : MonoBehaviour
    {
        [Tooltip("\"Mes de benvinguda\" o \"Recompensa diària\"")]
        [SerializeField] private TMP_Text titol;
        [Tooltip("\"Dia 3/30\"")]
        [SerializeField] private TMP_Text dia;
        [Tooltip("El que dona avui")]
        [SerializeField] private TMP_Text premi;
        [SerializeField] private Button boto;
        [Tooltip("\"Recollir\" o \"Torna demà\"")]
        [SerializeField] private TMP_Text textBoto;

        private PanellDeBotiga _panell;

        private void Awake()
        {
            if (boto != null) boto.onClick.AddListener(Recollir);
        }

        public void Inicialitzar(PanellDeBotiga panell)
        {
            _panell = panell;
            Refrescar();
        }

        public void Refrescar()
        {
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (g == null) return;

            if (titol != null) titol.text = Textos.T(g.EsElMesInicial ? "botiga.benvinguda" : "botiga.calendari");
            (int n, int de) = g.NumeroDelCalendari;
            if (dia != null) dia.text = de > 0 ? Textos.F("botiga.dia", n, de) : string.Empty;
            DiaDelCalendari d = g.DiaDelCalendari;
            if (premi != null) premi.text = d != null ? TextosDeBotiga.Descripcio(d.recompenses, " + ") : string.Empty;

            bool pot = g.PotRecollirElCalendari;
            if (boto != null) boto.interactable = pot;
            if (textBoto != null) textBoto.text = Textos.T(pot ? "botiga.recollir" : "botiga.dema");
        }

        private void Recollir()
        {
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (g == null) return;
            var donades = g.RecollirElCalendari();
            if (_panell != null) _panell.Rebut(donades, (RectTransform)transform, false);
        }
    }
}
