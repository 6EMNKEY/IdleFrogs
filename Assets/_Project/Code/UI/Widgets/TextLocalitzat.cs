using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un text fix de l'escena (UPGRADE, Hire Boss...) en l'idioma triat. Va al
    // mateix objecte que el TMP_Text; la clau és la de la taula de Textos.
    [RequireComponent(typeof(TMP_Text))]
    public class TextLocalitzat : MonoBehaviour
    {
        [Tooltip("La clau de Textos, per exemple ui.upgrade")]
        [SerializeField] private string clau;

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            Textos.EnCanviDIdioma += Refrescar;
            Refrescar();
        }

        private void OnDisable() => Textos.EnCanviDIdioma -= Refrescar;

        private void Refrescar()
        {
            if (_text != null && !string.IsNullOrEmpty(clau)) _text.text = Textos.T(clau);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(clau) && !Textos.Existeix(clau))
                Debug.LogWarning($"{name}: la clau '{clau}' no és a la taula de Textos", this);
        }
#endif
    }
}
