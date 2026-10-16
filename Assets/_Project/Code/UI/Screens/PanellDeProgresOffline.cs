using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // "Mentre no hi eres": explica què han fet els encarregats mentre el joc
    // estava tancat o en segon pla.
    //
    // Els doblers ja els ha donat el GameManager quan ha carregat; aquest
    // panell només ho ensenya. Així, si el jugador tanca l'app sense mirar-lo,
    // no perd res.
    public class PanellDeProgresOffline : MonoBehaviour
    {
        [Header("Arrel del panell (el que s'amaga i es mostra)")]
        [SerializeField] private GameObject arrel;

        [SerializeField] private TMP_Text textTemps;
        [SerializeField] private TMP_Text textGuanys;
        [SerializeField] private Button botoTancar;

        private readonly StringBuilder _sb = new StringBuilder();

        private void Awake()
        {
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (arrel != null) arrel.SetActive(false);
        }

        // El GameManager pot haver fet el càlcul abans o després d'aquest
        // Start: si ja l'ha fet, el recollim; si no, ens arribarà per l'event.
        private void Start()
        {
            if (GameManager.Instancia == null) return;

            GameManager.Instancia.EnProgresOffline += EnRebreProgres;

            ResultatOffline? pendent = GameManager.Instancia.ConsumirResultatOffline();
            if (pendent.HasValue) Ensenyar(pendent.Value);
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnProgresOffline -= EnRebreProgres;
        }

        private void EnRebreProgres(ResultatOffline r)
        {
            if (GameManager.Instancia != null) GameManager.Instancia.ConsumirResultatOffline();
            Ensenyar(r);
        }

        private void Ensenyar(ResultatOffline r)
        {
            if (textTemps != null)
            {
                textTemps.text = r.HiHaTope
                    ? $"Has estat fora {FormatadorDeNombres.Temps(r.segonsFora)}\n" +
                      $"<size=70%>(els encarregats treballen com a molt {FormatadorDeNombres.Temps(r.segonsComptats)})</size>"
                    : $"Has estat fora {FormatadorDeNombres.Temps(r.segonsFora)}";
            }

            if (textGuanys != null)
            {
                _sb.Clear();
                if (r.venut > 0)
                    _sb.AppendLine($"Venut a la tenda: <b>+{FormatadorDeNombres.Doblers(r.venut)}</b>");
                if (r.portatALEstacio > 0)
                    _sb.AppendLine($"Portat a l'estació: {FormatadorDeNombres.Doblers(r.portatALEstacio)}");
                if (r.produitAlsPantans > 0)
                    _sb.AppendLine($"Recollit als pantans: {FormatadorDeNombres.Doblers(r.produitAlsPantans)}");

                // Si només hi ha encarregats a mitja cadena, els doblers s'han
                // quedat esperant: val la pena dir-ho, o semblarà que no ha fet res.
                if (r.venut <= 0)
                    _sb.AppendLine("<size=80%>Sense encarregat a la tenda, no s'ha venut res.</size>");

                textGuanys.text = _sb.ToString().TrimEnd();
            }

            if (arrel != null) arrel.SetActive(true);
        }

        public void Tancar()
        {
            if (arrel != null) arrel.SetActive(false);
        }
    }
}
