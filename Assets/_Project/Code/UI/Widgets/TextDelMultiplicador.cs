using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Al HUD: el multiplicador que corre ara i el temps que li queda,
    // "x2 3:59:12". Amagat quan no n'hi ha cap.
    [RequireComponent(typeof(TMP_Text))]
    public class TextDelMultiplicador : MonoBehaviour
    {
        [Tooltip("Opcional: el fons o la icona, que s'amaga amb el text")]
        [SerializeField] private GameObject arrel;

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void Start()
        {
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi += Refrescar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.EnCanvi -= Refrescar;
        }

        private void Refrescar()
        {
            MultiplicadorActiu m = GestorDeBotiga.Instancia != null ? GestorDeBotiga.Instancia.MultiplicadorActiu : null;
            bool hi = m != null;
            if (arrel != null && arrel != gameObject && arrel.activeSelf != hi) arrel.SetActive(hi);
            _text.enabled = hi;
            if (hi) _text.text = $"x{FormatadorDeNombres.Factor(m.factor)} {FormatadorDeNombres.Temps(m.segonsRestants)}";
        }
    }
}
