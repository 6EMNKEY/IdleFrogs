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
        [Tooltip("Opcional: recollir-ho x2 veient un anunci. Només es veu quan es pot recollir")]
        [SerializeField] private Button botoX2Anunci;
        [Tooltip("Opcional: el text del botó x2. Buit: el primer TMP de dins del botó")]
        [SerializeField] private TMP_Text textX2Anunci;

        private PanellDeBotiga _panell;

        private void Awake()
        {
            if (boto != null) boto.onClick.AddListener(Recollir);
            if (botoX2Anunci != null)
            {
                botoX2Anunci.onClick.AddListener(RecollirDoble);
                if (textX2Anunci == null) textX2Anunci = botoX2Anunci.GetComponentInChildren<TMP_Text>(true);
            }
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
            if (botoX2Anunci != null && botoX2Anunci.gameObject.activeSelf != pot) botoX2Anunci.gameObject.SetActive(pot);
            if (textX2Anunci != null) textX2Anunci.text = Textos.F("anunci.xn",
                FormatadorDeNombres.Factor(GestorDeBotiga.Instancia != null ? GestorDeBotiga.Instancia.FactorDelVideoDeRegal : 2));
        }

        private void RecollirDoble()
        {
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (g == null) return;
            g.RecollirElCalendariAmbAnunci(donades =>
            {
                if (_panell != null) _panell.Rebut(donades, (RectTransform)transform, true);
            });
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
