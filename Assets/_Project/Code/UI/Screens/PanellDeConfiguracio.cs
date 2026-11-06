using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El panell de configuració. De moment, l'idioma (un desplegable amb els
    // idiomes escrits en el seu propi idioma; es nota a l'acte i es recorda)
    // i restaurar les compres, que Apple exigeix.
    public class PanellDeConfiguracio : MonoBehaviour
    {
        public static PanellDeConfiguracio Instancia { get; private set; }

        [Header("Obrir i tancar")]
        [SerializeField] private GameObject arrel;
        [Tooltip("El botó de configuració del HUD")]
        [SerializeField] private Button botoObrir;
        [SerializeField] private Button botoTancar;

        [Header("Idioma")]
        [SerializeField] private TMP_Dropdown desplegableDIdioma;

        [Header("So")]
        [Tooltip("Opcional: de 0 a 1")]
        [SerializeField] private Slider volumMusica;
        [Tooltip("Opcional: de 0 a 1")]
        [SerializeField] private Slider volumEfectes;

        [Header("Compres")]
        [Tooltip("Restaurar compres: torna els permanents (x2, sense anuncis...) d'aquest compte")]
        [SerializeField] private Button botoRestaurar;
        [Tooltip("Opcional: \"Compres restaurades: 1\"")]
        [SerializeField] private TMP_Text textRestaurar;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (botoObrir != null) botoObrir.onClick.AddListener(Obrir);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);

            if (desplegableDIdioma != null)
            {
                desplegableDIdioma.ClearOptions();
                desplegableDIdioma.AddOptions(new List<string>(Textos.NomsDelsIdiomes));
                desplegableDIdioma.SetValueWithoutNotify((int)Textos.Actual);
                desplegableDIdioma.onValueChanged.AddListener(i => Textos.Canviar((Idioma)i));
            }

            if (botoRestaurar != null) botoRestaurar.onClick.AddListener(Restaurar);
            if (volumMusica != null)
                volumMusica.onValueChanged.AddListener(v => { if (GestorDAudio.Instancia != null) GestorDAudio.Instancia.CanviarVolumDeLaMusica(v); });
            if (volumEfectes != null)
                volumEfectes.onValueChanged.AddListener(v => { if (GestorDAudio.Instancia != null) GestorDAudio.Instancia.CanviarVolumDelsEfectes(v); });

            if (arrel != null) arrel.SetActive(false);
        }

        private void Restaurar()
        {
            if (GestorDeBotiga.Instancia == null) return;
            if (botoRestaurar != null) botoRestaurar.interactable = false;
            GestorDeBotiga.Instancia.Restaurar(n =>
            {
                if (botoRestaurar != null) botoRestaurar.interactable = true;
                if (textRestaurar != null) textRestaurar.text = Textos.F("config.restaurats", n);
            });
        }

        public void Obrir()
        {
            // Un panell alhora.
            if (PanellDeMillora.Instancia != null) PanellDeMillora.Instancia.Tancar();
            if (PanellDEncarregats.Instancia != null) PanellDEncarregats.Instancia.Tancar();
            if (PanellDeBotiga.Instancia != null) PanellDeBotiga.Instancia.Tancar();
            if (textRestaurar != null) textRestaurar.text = string.Empty;
            if (GestorDAudio.Instancia != null)
            {
                if (volumMusica != null) volumMusica.SetValueWithoutNotify(GestorDAudio.Instancia.VolumMusica);
                if (volumEfectes != null) volumEfectes.SetValueWithoutNotify(GestorDAudio.Instancia.VolumEfectes);
            }

            if (desplegableDIdioma != null) desplegableDIdioma.SetValueWithoutNotify((int)Textos.Actual);
            if (arrel != null && !arrel.activeSelf) GestorDAudio.Sonar(So.Obrir);
            if (arrel != null) arrel.SetActive(true);
        }

        public void Tancar()
        {
            if (arrel != null && arrel.activeSelf) GestorDAudio.Sonar(So.Tancar);
            if (arrel != null) arrel.SetActive(false);
        }
    }
}
