using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Va al sprite del botó, amb un Collider2D. El toc li arriba del
    // DetectorDeTocs, igual que a les granotes.
    //
    // Dins d'un prefab es busca la unitat sol, perquè un prefab no pot
    // referenciar objectes d'escena. Però el botó del tren viu a sobre de
    // l'estació, que no és el tren: en aquest cas s'omple el camp a mà.
    public class BotoDeMillora : MonoBehaviour, IEsPotTocar
    {
        [Tooltip("Buit dins d'un prefab (es busca sol pujant per la jerarquia). " +
                 "Omple'l quan el botó no penja de la unitat que millora.")]
        [SerializeField] private UnitatMillorable unitat;

        private TMP_Text _textNivell;

        private void Awake()
        {
            if (unitat == null) unitat = GetComponentInParent<UnitatMillorable>();
            _textNivell = GetComponentInChildren<TMP_Text>();
        }

        private void Start()
        {
            if (unitat == null || _textNivell == null)
            {
                Debug.LogError("BotoDeMillora: necessita una UnitatMillorable (al pare o al camp) i un TMP_Text a dins", this);
                enabled = false;
                return;
            }

            unitat.EnCanviDeNivell += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (unitat != null) unitat.EnCanviDeNivell -= Refrescar;
        }

        public void EnTocar()
        {
            if (!enabled) return;
            if (PanellDeMillora.Instancia != null)
                PanellDeMillora.Instancia.Obrir(unitat);
        }

        private void Refrescar() => _textNivell.text = $"Nivell {unitat.Nivell}";
    }
}
