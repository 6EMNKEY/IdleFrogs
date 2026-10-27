using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El cartell "Comprar pantà". Viu dins del prefab del pantà, penjant
    // de l'arrel que Panta encén només quan l'estat és EnVenda, així que
    // no ha de decidir si es veu o no: només si es pot pagar.
    public class BotoDeCompra : MonoBehaviour, IEsPotTocar
    {
        [Header("Textos")]
        [Tooltip("El text del preu: s'hi escriu el preu i la moneda. El \"Buy\" és un text fix a part. " +
                 "Buit: s'escriu \"Comprar\" i el preu al primer text que hi hagi")]
        [SerializeField] private TMP_Text textPreu;

        [Header("Color quan no hi ha doblers")]
        [SerializeField] private Color colorApagat = new Color(0.5f, 0.5f, 0.5f, 1f);

        private Panta _panta;
        private TMP_Text _textPreu;
        private bool _nomesPreu;
        private SpriteRenderer _sprite;
        private Color _colorNormal = Color.white;

        private void Awake()
        {
            _panta = GetComponentInParent<Panta>();
            _nomesPreu = textPreu != null;
            _textPreu = _nomesPreu ? textPreu : GetComponentInChildren<TMP_Text>();
            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite != null) _colorNormal = _sprite.color;
        }

        private void Start()
        {
            if (_panta == null)
            {
                Debug.LogError("BotoDeCompra: ha de penjar d'un Panta", this);
                enabled = false;
                return;
            }

            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers += RefrescarPerDoblers;

            Refrescar();
        }

        private void OnEnable() => Refrescar();

        private void OnDestroy()
        {
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= RefrescarPerDoblers;
        }

        public void EnTocar()
        {
            if (!enabled || _panta == null) return;
            CompraAmbGemes.Intentar(_panta.PreuDeCompra, Textos.T("conf.comprar.titol"),
                preu => Textos.F("conf.comprar.text", _panta.NomVisible, preu), () => _panta.Comprar());
        }

        private void RefrescarPerDoblers(double _) => Refrescar();

        private void Refrescar()
        {
            if (_panta == null) return;

            if (_textPreu != null)
            {
                string preu = FormatadorDeNombres.Doblers(_panta.PreuDeCompra) + Icones.Moneda;
                _textPreu.text = _nomesPreu ? preu : Textos.F("mon.comprar", preu);
            }

            if (_sprite != null)
                _sprite.color = _panta.PucPagarLaCompra ? _colorNormal : colorApagat;
        }
    }
}
