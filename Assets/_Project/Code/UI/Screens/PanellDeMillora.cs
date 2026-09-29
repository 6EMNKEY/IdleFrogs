using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un sol panell per a totes les unitats millorables: pantà, tren i,
    // quan hi sigui, estació. No sap què està ensenyant — li demana les
    // files a la unitat i les pinta.
    //
    // Els botons dels prefabs hi arriben per la instància: no poden guardar
    // una referència perquè som un objecte d'escena i ells són prefabs.
    public class PanellDeMillora : MonoBehaviour
    {
        public static PanellDeMillora Instancia { get; private set; }

        [Header("Arrel del panell (el que s'amaga i es mostra)")]
        [SerializeField] private GameObject arrel;

        [Header("Capçalera")]
        [SerializeField] private TMP_Text titol;
        [SerializeField] private Button botoTancar;

        [Header("Files")]
        [SerializeField] private RectTransform contenidorDeFiles;
        [SerializeField] private FilaDEstadistica prefabDeFila;

        [Header("Peu")]
        [SerializeField] private TMP_Text textDoblers;
        [SerializeField] private TMP_Text textBotoMillorar;
        [SerializeField] private Button botoMillorar;
        [SerializeField] private Button botoX1, botoX10, botoX50, botoMax;

        private UnitatMillorable _unitat;
        private int _quantitat = 1;
        private bool _maxim;

        private readonly List<FilaEstadistica> _dades = new List<FilaEstadistica>();
        private readonly List<FilaDEstadistica> _files = new List<FilaDEstadistica>();

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoMillorar != null) botoMillorar.onClick.AddListener(Millorar);
            if (botoX1 != null) botoX1.onClick.AddListener(() => Triar(1, false));
            if (botoX10 != null) botoX10.onClick.AddListener(() => Triar(10, false));
            if (botoX50 != null) botoX50.onClick.AddListener(() => Triar(50, false));
            if (botoMax != null) botoMax.onClick.AddListener(() => Triar(1, true));

            if (arrel != null) arrel.SetActive(false);
        }

        public void Obrir(UnitatMillorable unitat)
        {
            if (unitat == null || !unitat.EstaComprat) return;

            // Dos panells oberts alhora es trepitgen; el d'encarregats surt.
            if (PanellDEncarregats.Instancia != null) PanellDEncarregats.Instancia.Tancar();

            Desubscriure();
            _unitat = unitat;
            _unitat.EnCanviDeNivell += Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers += RefrescarPerDoblers;

            if (arrel != null) arrel.SetActive(true);
            Refrescar();
        }

        public void Tancar()
        {
            Desubscriure();
            _unitat = null;
            if (arrel != null) arrel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            Desubscriure();
        }

        private void Desubscriure()
        {
            if (_unitat != null) _unitat.EnCanviDeNivell -= Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= RefrescarPerDoblers;
        }

        private void Triar(int quantitat, bool maxim)
        {
            _quantitat = quantitat;
            _maxim = maxim;
            Refrescar();
        }

        private int QuantitatActual()
        {
            if (_unitat == null) return 0;
            return _maxim ? Mathf.Max(1, _unitat.MillorsQuePucPagar()) : _quantitat;
        }

        private void RefrescarPerDoblers(double _) => Refrescar();

        private void Refrescar()
        {
            if (_unitat == null) return;

            int ara = _unitat.Nivell;
            int k = QuantitatActual();
            int desti = Mathf.Min(ara + k, _unitat.NivellMaxim);

            if (titol != null) titol.text = $"{_unitat.NomVisible} — Nivell {ara}";

            _dades.Clear();
            _unitat.OmplirFiles(_dades, desti);
            PintarFiles();

            double cost = _unitat.CostDeMillora(k);
            double doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;

            if (textDoblers != null) textDoblers.text = FormatadorDeNombres.Doblers(doblers);
            if (textBotoMillorar != null)
                textBotoMillorar.text = $"Millorar x{k}\n{FormatadorDeNombres.Doblers(cost)}";
            if (botoMillorar != null) botoMillorar.interactable = k > 0 && doblers >= cost;
        }

        // Reaprofitem les files que ja hi ha i només en creem quan en falten:
        // el panell s'obre i es refresca constantment.
        private void PintarFiles()
        {
            if (contenidorDeFiles == null || prefabDeFila == null) return;

            while (_files.Count < _dades.Count)
                _files.Add(Instantiate(prefabDeFila, contenidorDeFiles));

            for (int i = 0; i < _files.Count; i++)
            {
                bool cal = i < _dades.Count;
                _files[i].gameObject.SetActive(cal);
                if (cal) _files[i].Omplir(_dades[i]);
            }
        }

        private void Millorar()
        {
            if (_unitat == null) return;
            _unitat.Millorar(QuantitatActual());
        }
    }
}
