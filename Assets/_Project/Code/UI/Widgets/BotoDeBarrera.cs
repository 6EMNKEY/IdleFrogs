using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La roca. Tocar-la engega el temporitzador; a partir d'aquí corre sola
    // i el text va comptant enrere. No es paga amb doblers: es paga amb temps.
    public class BotoDeBarrera : MonoBehaviour
    {
        private Barrera _barrera;
        private TMP_Text _text;

        private void Awake()
        {
            _barrera = GetComponentInParent<Barrera>();
            _text = GetComponentInChildren<TMP_Text>();
        }

        private void Start()
        {
            if (_barrera == null)
            {
                Debug.LogError("BotoDeBarrera: ha de penjar d'una Barrera", this);
                enabled = false;
                return;
            }

            _barrera.EnCanviDeTemps += Refrescar;
            // El tick de segon és el que fa avançar el rellotge visible.
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += Refrescar;

            Refrescar();
        }

        private void OnDestroy()
        {
            if (_barrera != null) _barrera.EnCanviDeTemps -= Refrescar;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Refrescar;
        }

        // Un toc engega el temporitzador; si ja corre i tens els doblers,
        // el segon toc la trenca pagant.
        private void OnMouseDown()
        {
            if (!enabled || _barrera == null) return;

            if (_barrera.EsPotIniciar) { _barrera.Iniciar(); return; }
            if (_barrera.EstaIniciada && _barrera.PucPagarLaTrencada) _barrera.TrencarPagant();
        }

        private void Refrescar()
        {
            if (_text == null || _barrera == null) return;

            if (_barrera.EstaOberta) { _text.text = string.Empty; return; }

            if (_barrera.EstaIniciada)
            {
                _text.text = _barrera.Preu > 0
                    ? $"{FormatadorDeNombres.Temps(_barrera.SegonsRestants)}\no {FormatadorDeNombres.Doblers(_barrera.Preu)}"
                    : FormatadorDeNombres.Temps(_barrera.SegonsRestants);
                return;
            }

            _text.text = _barrera.EsPotIniciar
                ? $"Trencar\n{FormatadorDeNombres.Temps(_barrera.SegonsTotals)}"
                : "Bloquejat";
        }
    }
}
