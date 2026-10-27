using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una targeta de la botiga. La mateixa per a tots els productes: el que
    // canvia és el ProducteDeBotiga que se li dona. Totes les peces són
    // opcionals; el panell la crea, la col·loca i la repinta cada segon.
    public class TargetaDeProducte : MonoBehaviour
    {
        [Header("Contingut")]
        [SerializeField] private Image icona;
        [SerializeField] private TMP_Text nom;
        [Tooltip("Què dona: \"500 gemes\", \"x2 durant 4h\"...")]
        [SerializeField] private TMP_Text contingut;

        [Header("Comprar")]
        [SerializeField] private Button boto;
        [Tooltip("El preu, \"Gratis\", \"Veure anunci\", el compte enrere o \"Comprat\"")]
        [SerializeField] private TMP_Text preu;

        [Header("Extres")]
        [Tooltip("El cartellet de dalt (\"Popular\", \"x2 primera compra\"). S'amaga si no n'hi ha")]
        [SerializeField] private GameObject arrelDeLEtiqueta;
        [SerializeField] private TMP_Text etiqueta;
        [Tooltip("\"En queden 3\" o \"Queden 12 dies\"")]
        [SerializeField] private TMP_Text textLimit;
        [Tooltip("\"S'acaba en 2d 4h\"")]
        [SerializeField] private TMP_Text textTemps;

        private ProducteDeBotiga _producte;
        private PanellDeBotiga _panell;

        public ProducteDeBotiga Producte => _producte;

        private void Awake()
        {
            if (boto != null) boto.onClick.AddListener(Tocar);
        }

        public void Omplir(ProducteDeBotiga producte, PanellDeBotiga panell)
        {
            _producte = producte;
            _panell = panell;
            if (icona != null)
            {
                icona.sprite = producte != null ? producte.icona : null;
                icona.enabled = icona.sprite != null;
            }
            Refrescar();
        }

        private bool EsElPase
        {
            get
            {
                if (_producte == null) return false;
                foreach (Recompensa r in _producte.recompenses)
                    if (r != null && r.tipus == TipusDeRecompensa.PaseMensual) return true;
                return false;
            }
        }

        public void Refrescar()
        {
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (_producte == null || g == null) return;

            if (nom != null) nom.text = TextosDeBotiga.Nom(_producte);
            if (contingut != null) contingut.text = TextosDeBotiga.Contingut(_producte);

            // El pase comprat: la targeta passa a ser la de recollir el del dia.
            if (EsElPase && g.PaseActiu)
            {
                string gemes = FormatadorDeNombres.Doblers(g.GemesDelPasePerDia) + Icones.Gema;
                Posar(preu, g.PotRecollirElPase ? Textos.F("botiga.recollir_gemes", gemes) : Textos.T("botiga.dema"));
                if (boto != null) boto.interactable = g.PotRecollirElPase;
                Posar(textLimit, Textos.F("botiga.dies_pase", g.DiesDelPase));
                Posar(textTemps, null);
                Etiqueta(null);
                return;
            }

            EstatDeProducte estat = g.Estat(_producte);
            Posar(preu, TextDelPreu(g, estat));
            if (boto != null) boto.interactable = estat == EstatDeProducte.Disponible;

            int restants = g.Restants(_producte);
            Posar(textLimit, restants >= 0 && estat != EstatDeProducte.Esgotat ? Textos.F("botiga.queden", restants) : null);

            double oferta = g.SegonsDOferta(_producte);
            Posar(textTemps, oferta > 0 && estat != EstatDeProducte.Esgotat
                ? Textos.F("botiga.acaba", FormatadorDeNombres.TempsCurt(oferta)) : null);

            string text = _producte.etiqueta.Text;
            if (_producte.doblePrimeraCompra && g.Vegades(_producte) == 0) text = Textos.T("botiga.x2_primera");
            Etiqueta(text);
        }

        private string TextDelPreu(GestorDeBotiga g, EstatDeProducte estat)
        {
            switch (estat)
            {
                case EstatDeProducte.Esgotat:
                    return Textos.T(_producte.EsPermanent || _producte.limit == 1 ? "botiga.comprat" : "botiga.esgotat");
                case EstatDeProducte.Caducat:
                    return Textos.T("botiga.caducat");
                case EstatDeProducte.Esperant:
                    return Textos.F("botiga.torna", FormatadorDeNombres.TempsCurt(g.SegonsFinsDisponible(_producte)));
            }

            switch (_producte.pagament)
            {
                case TipusDePagament.Gratis: return Textos.T("botiga.preu_gratis");
                case TipusDePagament.Anunci: return Textos.T(g.SenseAnuncis ? "botiga.preu_gratis" : "botiga.anunci");
                case TipusDePagament.Gemes: return FormatadorDeNombres.Doblers(_producte.preuEnGemes) + Icones.Gema;
                default: return g.PreuReal(_producte);
            }
        }

        private void Etiqueta(string text)
        {
            bool hi = !string.IsNullOrEmpty(text);
            if (arrelDeLEtiqueta != null && arrelDeLEtiqueta.activeSelf != hi) arrelDeLEtiqueta.SetActive(hi);
            if (etiqueta != null) etiqueta.text = hi ? text : string.Empty;
        }

        private static void Posar(TMP_Text t, string text)
        {
            if (t == null) return;
            bool hi = !string.IsNullOrEmpty(text);
            if (t.gameObject.activeSelf != hi) t.gameObject.SetActive(hi);
            if (hi) t.text = text;
        }

        private void Tocar()
        {
            GestorDeBotiga g = GestorDeBotiga.Instancia;
            if (g == null || _producte == null) return;

            if (EsElPase && g.PaseActiu)
            {
                int gemes = g.RecollirElPase();
                if (gemes > 0 && _panell != null)
                    _panell.Rebut(new List<Recompensa> { new Recompensa { tipus = TipusDeRecompensa.Gemes, quantitat = gemes } },
                                  (RectTransform)transform, false);
                return;
            }

            bool primera = _producte.doblePrimeraCompra && g.Vegades(_producte) == 0;
            g.Comprar(_producte, donades =>
            {
                if (_panell != null) _panell.Rebut(donades, (RectTransform)transform, primera);
            });
        }
    }
}
