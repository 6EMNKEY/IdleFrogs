using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El panell d'encarregats d'una ranura. Mateixa mida i mateixa estructura
    // que PanellDeMillora: a dalt la llista del que tens d'aquell tipus, a
    // baix el botó de comprar.
    //
    // Les siluetes dels prefabs hi arriben per la instància, perquè un prefab
    // no pot referenciar un objecte d'escena.
    public class PanellDEncarregats : MonoBehaviour
    {
        public static PanellDEncarregats Instancia { get; private set; }

        [Header("Arrel del panell (el que s'amaga i es mostra)")]
        [SerializeField] private GameObject arrel;

        [Header("Capçalera")]
        [SerializeField] private TMP_Text titol;
        [SerializeField] private Button botoTancar;

        [Header("Llista")]
        [SerializeField] private RectTransform contenidorDeFitxes;
        [SerializeField] private FitxaDEncarregat prefabDeFitxa;
        [Tooltip("Es mostra quan encara no en tens cap d'aquest tipus")]
        [SerializeField] private GameObject avisDeLlistaBuida;

        [Header("Peu")]
        [SerializeField] private TMP_Text textDoblers;
        [SerializeField] private Button botoComprar;
        [SerializeField] private TMP_Text textBotoComprar;
        [Tooltip("Hi surt què t'ha tocat en comprar")]
        [SerializeField] private TMP_Text textDeResultat;

        private UnitatMillorable _unitat;
        private readonly List<EncarregatPosseit> _dades = new List<EncarregatPosseit>();
        private readonly List<FitxaDEncarregat> _fitxes = new List<FitxaDEncarregat>();

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoComprar != null) botoComprar.onClick.AddListener(Comprar);

            if (arrel != null) arrel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            Desubscriure();
        }

        public void Obrir(UnitatMillorable unitat)
        {
            if (unitat == null) return;

            // Dos panells oberts alhora es trepitgen; el de millores surt.
            if (PanellDeMillora.Instancia != null) PanellDeMillora.Instancia.Tancar();

            Desubscriure();
            _unitat = unitat;

            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.EnCanviDEncarregats += Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers += RefrescarPerDoblers;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += Refrescar;   // els cooldowns

            if (textDeResultat != null) textDeResultat.text = string.Empty;
            if (arrel != null) arrel.SetActive(true);
            Refrescar();
        }

        public void Tancar()
        {
            Desubscriure();
            _unitat = null;
            if (arrel != null) arrel.SetActive(false);
        }

        private void Desubscriure()
        {
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.EnCanviDEncarregats -= Refrescar;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EnCanviDeDoblers -= RefrescarPerDoblers;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Refrescar;
        }

        private void RefrescarPerDoblers(double _) => Refrescar();

        private void Refrescar()
        {
            if (_unitat == null || GestorDEncarregats.Instancia == null) return;

            Ranura ranura = _unitat.Ranura;
            if (titol != null) titol.text = $"Encarregat · {ranura}";

            GestorDEncarregats.Instancia.PerALloc(ranura.tipus, _dades);
            PintarFitxes(ranura);

            if (avisDeLlistaBuida != null) avisDeLlistaBuida.SetActive(_dades.Count == 0);

            double preu = GestorDEncarregats.Instancia.PreuDeLaSeguentCompra;
            double doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;

            if (textDoblers != null) textDoblers.text = FormatadorDeNombres.Doblers(doblers);
            if (textBotoComprar != null)
                textBotoComprar.text = $"Contractar\n{FormatadorDeNombres.Doblers(preu)}";
            if (botoComprar != null) botoComprar.interactable = doblers >= preu;
        }

        // Es reaprofiten les fitxes i només se'n creen quan en falten, com fa
        // PanellDeMillora amb les files.
        private void PintarFitxes(Ranura ranura)
        {
            if (contenidorDeFitxes == null || prefabDeFitxa == null) return;

            while (_fitxes.Count < _dades.Count)
                _fitxes.Add(Instantiate(prefabDeFitxa, contenidorDeFitxes, false));

            for (int i = 0; i < _fitxes.Count; i++)
            {
                bool cal = i < _dades.Count;
                _fitxes[i].gameObject.SetActive(cal);
                if (cal) _fitxes[i].Omplir(_dades[i], ranura, Refrescar);
            }
        }

        private void Comprar()
        {
            if (_unitat == null || GestorDEncarregats.Instancia == null) return;

            EncarregatSO rebut = GestorDEncarregats.Instancia.Comprar(_unitat.Ranura.tipus);
            if (rebut == null) return;

            if (textDeResultat != null)
                textDeResultat.text = $"Has contractat {rebut.nomVisible} · {rebut.DescripcioDeLEfecte}";

            Refrescar();
        }
    }
}
