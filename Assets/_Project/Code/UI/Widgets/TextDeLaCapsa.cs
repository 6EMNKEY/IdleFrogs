using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Va al mateix GameObject que el text TMP, dins del prefab del pantà.
    // No té res per arrossegar: busca el pantà pujant per la jerarquia.
    public class TextDeLaCapsa : MonoBehaviour
    {
        private Panta _panta;
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _panta = GetComponentInParent<Panta>();
        }

        private void Start()
        {
            if (_panta == null || _text == null)
            {
                Debug.LogError("TextDeLaCapsa: ha d'anar en un TMP_Text penjant d'un Panta", this);
                enabled = false;
                return;
            }

            _panta.EnCanviDeCapsa += Refrescar;
            Refrescar(_panta.DoblersALaCapsa);
        }

        private void OnDestroy()
        {
            if (_panta != null) _panta.EnCanviDeCapsa -= Refrescar;
        }

        private void Refrescar(double quantitat)
        {
            _text.text = FormatadorDeNombres.Doblers(quantitat);
        }
    }
}
