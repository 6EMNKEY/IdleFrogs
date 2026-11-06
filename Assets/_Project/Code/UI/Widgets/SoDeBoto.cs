using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Posa'l a qualsevol botó que hagi de fer soroll en tocar-lo (pestanyes,
    // el (i), botons sense un so propi). Els que ja en tenen (comprar,
    // millorar, acceptar...) el fan des del codi i no el necessiten.
    [RequireComponent(typeof(Button))]
    public class SoDeBoto : MonoBehaviour
    {
        [SerializeField] private So so = So.Toc;

        private void Awake() => GetComponent<Button>().onClick.AddListener(() => GestorDAudio.Sonar(so));
    }
}
