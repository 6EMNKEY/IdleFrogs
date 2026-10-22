using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una carta petita de la llista d'encarregats no assignats. Prefab que
    // el panell instancia tantes vegades com en tinguis d'aquell tipus.
    // Tocar-la la selecciona; el botó vermell la ven.
    public class FitxaDEncarregat : MonoBehaviour
    {
        [Tooltip("El botó de tota la carta: tocar-la la selecciona")]
        [SerializeField] private Button botoSeleccionar;
        [Tooltip("El marc groc. S'encén quan la carta està seleccionada")]
        [SerializeField] private GameObject marcDeSeleccio;

        [Header("Contingut")]
        [Tooltip("El quadrat de darrere del retrat: es pinta del color de la raresa")]
        [SerializeField] private Image fonsDelRetrat;
        [SerializeField] private Image retrat;
        [Tooltip("\"3m\"")]
        [SerializeField] private TMP_Text duracio;
        [Tooltip("L'habilitat activa, curta: \"5x Vel\"")]
        [SerializeField] private TMP_Text efecte;
        [Tooltip("Opcional: l'efecte passiu, el que fa sempre mentre treballa: \"+5% Velocitat\"")]
        [SerializeField] private TMP_Text passiu;

        [Header("Vendre")]
        [SerializeField] private Button botoVendre;

        private EncarregatPosseit _encarregat;
        private System.Action<EncarregatPosseit> _enSeleccionar;
        private System.Action<EncarregatPosseit> _enVendre;

        public EncarregatPosseit Encarregat => _encarregat;

        private void Awake()
        {
            if (botoSeleccionar != null)
                botoSeleccionar.onClick.AddListener(() => _enSeleccionar?.Invoke(_encarregat));
            if (botoVendre != null)
                botoVendre.onClick.AddListener(() => _enVendre?.Invoke(_encarregat));
        }

        public void Omplir(EncarregatPosseit encarregat, bool seleccionada,
                           System.Action<EncarregatPosseit> enSeleccionar,
                           System.Action<EncarregatPosseit> enVendre)
        {
            _encarregat = encarregat;
            _enSeleccionar = enSeleccionar;
            _enVendre = enVendre;

            if (marcDeSeleccio != null) marcDeSeleccio.SetActive(seleccionada);
            if (encarregat == null || encarregat.definicio == null) return;

            EncarregatSO d = encarregat.definicio;
            if (fonsDelRetrat != null) fonsDelRetrat.color = ColorsDeRaresa.Per(d.raresa);
            if (retrat != null)
            {
                Sprite s = GestorDEncarregats.Instancia != null ? GestorDEncarregats.Instancia.RetratDe(d) : d.retrat;
                retrat.sprite = s;
                retrat.enabled = s != null;
            }
            if (duracio != null) duracio.text = d.DuracioCurta;
            if (efecte != null) efecte.text = d.DescripcioCurtaDeLHabilitat;
            if (passiu != null) passiu.text = d.DescripcioDeLEfecte;
        }
    }
}
