using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La finestra de "N'estàs segur?" de tot el joc. N'hi ha una sola, per
    // sobre de tots els panells; qui la necessita la crida amb el títol,
    // l'explicació i el que s'ha de fer si s'accepta:
    //
    //     FinestraDeConfirmacio.Preguntar(titol, explicacio, () => Vendre(e),
    //         textAcceptar: Textos.T("conf.vendre.boto"), perillosa: true);
    //
    // Cancel·lar, la X i tocar el fons no fan mai l'acció. Només hi ha una
    // pregunta alhora: si n'arriba una altra amb aquesta oberta, la primera
    // compta com a cancel·lada.
    public class FinestraDeConfirmacio : MonoBehaviour
    {
        public static FinestraDeConfirmacio Instancia { get; private set; }

        public bool EstaOberta => arrel != null && arrel.activeSelf;

        [Header("Arrel (el que s'amaga i es mostra)")]
        [Tooltip("Amb un fons a pantalla completa amb Raycast Target, perquè no passi cap toc a sota")]
        [SerializeField] private GameObject arrel;

        [Header("Textos")]
        [SerializeField] private TMP_Text textTitol;
        [SerializeField] private TMP_Text textExplicacio;

        [Header("Botons")]
        [SerializeField] private Button botoAcceptar;
        [Tooltip("Opcional: el text del botó d'acceptar. Buit: el primer TMP de dins del botó")]
        [SerializeField] private TMP_Text textAcceptar;
        [SerializeField] private Button botoCancellar;
        [Tooltip("Opcional: el text del botó de cancel·lar. Buit: el primer TMP de dins del botó")]
        [SerializeField] private TMP_Text textCancellar;
        [Tooltip("Opcional: la X de la cantonada. Cancel·la")]
        [SerializeField] private Button botoTancar;
        [Tooltip("Opcional: un Button al fons fosc. Tocar fora de la finestra cancel·la")]
        [SerializeField] private Button botoDelFons;

        [Header("Acció perillosa (vendre, reiniciar...)")]
        [Tooltip("El color del botó d'acceptar quan el que fa no té volta enrere")]
        [SerializeField] private Color colorPerillos = new Color(0.85f, 0.25f, 0.2f, 1f);

        private Action _siAccepta;
        private Action _siCancella;
        private Color _colorNormal = Color.white;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;

            if (textAcceptar == null && botoAcceptar != null) textAcceptar = botoAcceptar.GetComponentInChildren<TMP_Text>(true);
            if (textCancellar == null && botoCancellar != null) textCancellar = botoCancellar.GetComponentInChildren<TMP_Text>(true);
            if (botoAcceptar != null && botoAcceptar.image != null) _colorNormal = botoAcceptar.image.color;

            if (botoAcceptar != null) botoAcceptar.onClick.AddListener(Acceptar);
            if (botoCancellar != null) botoCancellar.onClick.AddListener(Cancellar);
            if (botoTancar != null) botoTancar.onClick.AddListener(Cancellar);
            if (botoDelFons != null) botoDelFons.onClick.AddListener(Cancellar);

            if (arrel != null) arrel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // textAcceptar / textCancellar buits: "Accept" i "Cancel" de l'idioma.
        public static void Preguntar(string titol, string explicacio, Action siAccepta,
                                     Action siCancella = null, string textAcceptar = null,
                                     string textCancellar = null, bool perillosa = false)
        {
            if (Instancia == null)
            {
                // Sense finestra a l'escena el joc no s'ha d'encallar: el que
                // es pot desfer es fa directament. El que no (vendre,
                // reiniciar) no es fa mai sense preguntar.
                if (perillosa)
                {
                    Debug.LogError($"FinestraDeConfirmacio: no n'hi ha cap a l'escena; no es fa \"{titol}\"");
                    siCancella?.Invoke();
                    return;
                }
                Debug.LogWarning($"FinestraDeConfirmacio: no n'hi ha cap a l'escena; s'accepta sense preguntar ({titol})");
                siAccepta?.Invoke();
                return;
            }
            Instancia.Obrir(titol, explicacio, siAccepta, siCancella, textAcceptar, textCancellar, perillosa);
        }

        private void Obrir(string titol, string explicacio, Action siAccepta, Action siCancella,
                           string acceptar, string cancellar, bool perillosa)
        {
            // La que hi havia queda cancel·lada.
            if (EstaOberta) Cancellar();

            _siAccepta = siAccepta;
            _siCancella = siCancella;

            if (textTitol != null) textTitol.text = titol ?? string.Empty;
            if (textExplicacio != null) textExplicacio.text = explicacio ?? string.Empty;
            if (textAcceptar != null) textAcceptar.text = string.IsNullOrEmpty(acceptar) ? Textos.T("ui.acceptar") : acceptar;
            if (textCancellar != null) textCancellar.text = string.IsNullOrEmpty(cancellar) ? Textos.T("ui.cancellar") : cancellar;
            if (botoAcceptar != null && botoAcceptar.image != null)
                botoAcceptar.image.color = perillosa ? colorPerillos : _colorNormal;

            if (arrel != null)
            {
                arrel.SetActive(true);
                // Damunt de tot el que hi hagi al mateix Canvas.
                arrel.transform.SetAsLastSibling();
            }
        }

        // Es buida abans de cridar: si l'acció obre una altra pregunta, no
        // l'esborrem en tancar aquesta.
        private void Acceptar()
        {
            Action accio = _siAccepta;
            Tancar();
            accio?.Invoke();
        }

        private void Cancellar()
        {
            Action accio = _siCancella;
            Tancar();
            accio?.Invoke();
        }

        private void Tancar()
        {
            _siAccepta = null;
            _siCancella = null;
            if (arrel != null) arrel.SetActive(false);
        }
    }
}
