using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El botó d'Unlock de la barrera. No es veu fins que has comprat l'últim
    // pantà del tram; llavors ensenya el preu (el del pantà que es salta) i
    // el temps que tardarà. Tocar-lo paga i engega el temporitzador: el botó
    // marxa i queda el compte enrere fins que la barrera desapareix.
    public class BotoDeBarrera : MonoBehaviour, IEsPotTocar
    {
        [Header("Peces")]
        [Tooltip("El botó (fons, preu, \"Unlock\"). Només es veu quan es pot desbloquejar. " +
                 "Buit: només s'apaga el collider (la roca es queda)")]
        [SerializeField] private GameObject arrelDelBoto;
        [Tooltip("El preu i la moneda")]
        [SerializeField] private TMP_Text textPreu;
        [Tooltip("Abans de començar, el que tardarà (\"8h\"); després, el compte enrere. " +
                 "Posa'l fora de l'Arrel Del Boto perquè es vegi mentre compta. " +
                 "Buit: el primer text dels fills")]
        [SerializeField] private TMP_Text textTemps;

        [Header("Sense doblers")]
        [Tooltip("Opcional: el fons del botó, que es posa d'aquest color si no ho pots pagar")]
        [SerializeField] private SpriteRenderer fonsDelBoto;
        [SerializeField] private Color colorApagat = new Color(0.5f, 0.5f, 0.5f, 1f);

        private Barrera _barrera;
        private SpriteRenderer _sprite;
        private Collider2D _collider;
        private Color _colorNormal = Color.white;

        private void Awake()
        {
            _barrera = GetComponentInParent<Barrera>();
            _sprite = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            if (textTemps == null) textTemps = GetComponentInChildren<TMP_Text>(true);
            if (fonsDelBoto != null) _colorNormal = fonsDelBoto.color;
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
            _barrera.EnCanviDeDisponibilitat += Refrescar;
            // El tick de segon fa avançar el rellotge visible, i també és el que
            // s'adona que has comprat l'últim pantà del tram.
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers += RefrescarPerDoblers;

            Refrescar();
        }

        private void OnDestroy()
        {
            if (_barrera != null)
            {
                _barrera.EnCanviDeTemps -= Refrescar;
                _barrera.EnCanviDeDisponibilitat -= Refrescar;
            }
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= RefrescarPerDoblers;
        }

        public void EnTocar()
        {
            if (!enabled || _barrera == null) return;
            if (_barrera.Iniciar()) Refrescar();
        }

        private void RefrescarPerDoblers(double _) => Refrescar();

        private void Refrescar()
        {
            if (_barrera == null) return;

            bool boto = _barrera.EsPotIniciar;
            bool comptant = _barrera.EstaIniciada && !_barrera.EstaOberta;

            MostrarBoto(boto);

            if (textPreu != null)
                textPreu.text = boto ? FormatadorDeNombres.Doblers(_barrera.Preu) + Icones.Moneda : string.Empty;

            if (textTemps != null)
            {
                textTemps.enabled = boto || comptant;
                textTemps.text = comptant ? FormatadorDeNombres.Temps(_barrera.SegonsRestants)
                               : boto ? FormatadorDeNombres.TempsCurt(_barrera.SegonsTotals)
                               : string.Empty;
            }

            if (boto && fonsDelBoto != null)
                fonsDelBoto.color = _barrera.PucPagar ? _colorNormal : colorApagat;
        }

        // L'arrel s'apaga sencera; si l'arrel és aquest mateix objecte, el seu
        // dibuix (no el GameObject: aquest script ha de seguir corrent). El
        // collider d'aquí només toca quan hi ha botó.
        private void MostrarBoto(bool visible)
        {
            if (arrelDelBoto == gameObject)
            {
                if (_sprite != null) _sprite.enabled = visible;
            }
            else if (arrelDelBoto != null && arrelDelBoto.activeSelf != visible)
            {
                arrelDelBoto.SetActive(visible);
            }
            if (_collider != null) _collider.enabled = visible;
        }
    }
}
