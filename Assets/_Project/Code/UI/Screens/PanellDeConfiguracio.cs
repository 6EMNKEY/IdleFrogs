using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El panell de configuració. De moment, l'idioma: un desplegable amb els
    // idiomes escrits en el seu propi idioma. Canviar-lo es nota a l'acte i es
    // recorda la propera vegada.
    public class PanellDeConfiguracio : MonoBehaviour
    {
        [Header("Obrir i tancar")]
        [SerializeField] private GameObject arrel;
        [Tooltip("El botó de configuració del HUD")]
        [SerializeField] private Button botoObrir;
        [SerializeField] private Button botoTancar;

        [Header("Idioma")]
        [SerializeField] private TMP_Dropdown desplegableDIdioma;

        private void Awake()
        {
            if (botoObrir != null) botoObrir.onClick.AddListener(Obrir);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);

            if (desplegableDIdioma != null)
            {
                desplegableDIdioma.ClearOptions();
                desplegableDIdioma.AddOptions(new List<string>(Textos.NomsDelsIdiomes));
                desplegableDIdioma.SetValueWithoutNotify((int)Textos.Actual);
                desplegableDIdioma.onValueChanged.AddListener(i => Textos.Canviar((Idioma)i));
            }

            if (arrel != null) arrel.SetActive(false);
        }

        public void Obrir()
        {
            // Un panell alhora.
            if (PanellDeMillora.Instancia != null) PanellDeMillora.Instancia.Tancar();
            if (PanellDEncarregats.Instancia != null) PanellDEncarregats.Instancia.Tancar();
            if (PanellDelMapa.Instancia != null) PanellDelMapa.Instancia.Tancar();

            if (desplegableDIdioma != null) desplegableDIdioma.SetValueWithoutNotify((int)Textos.Actual);
            if (arrel != null) arrel.SetActive(true);
        }

        public void Tancar()
        {
            if (arrel != null) arrel.SetActive(false);
        }
    }
}
