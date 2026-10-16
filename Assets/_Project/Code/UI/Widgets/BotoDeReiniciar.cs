using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Botó de reiniciar la partida. Esborra-ho tot, així que demana un segon
    // toc per confirmar: el primer canvia el text i, si no tornes a tocar en
    // uns segons, es desarma sol.
    //
    // Va en un Button de la UI (Canvas), no al món: no necessita collider.
    [RequireComponent(typeof(Button))]
    public class BotoDeReiniciar : MonoBehaviour
    {
        [Tooltip("Buit = el primer TMP_Text que hi hagi a dins del botó")]
        [SerializeField] private TMP_Text text;
        [SerializeField] private string textNormal = "Reiniciar partida";
        [SerializeField] private string textDeConfirmacio = "Segur? Es perdrà tot.\nToca un altre cop";
        [SerializeField] private float segonsPerConfirmar = 3f;

        private Button _boto;
        private float _armatFins = -1f;

        private void Awake()
        {
            _boto = GetComponent<Button>();
            _boto.onClick.AddListener(EnPremer);
            if (text == null) text = GetComponentInChildren<TMP_Text>();
            Posar(textNormal);
        }

        private void OnDisable()
        {
            // Si s'amaga el panell on viu, en tornar no ha d'estar armat.
            _armatFins = -1f;
            Posar(textNormal);
        }

        private void Update()
        {
            if (_armatFins > 0f && Time.unscaledTime > _armatFins)
            {
                _armatFins = -1f;
                Posar(textNormal);
            }
        }

        private void EnPremer()
        {
            if (_armatFins < 0f)
            {
                _armatFins = Time.unscaledTime + segonsPerConfirmar;
                Posar(textDeConfirmacio);
                return;
            }

            _armatFins = -1f;
            if (GameManager.Instancia != null) GameManager.Instancia.ReiniciarPartida();
        }

        private void Posar(string t)
        {
            if (text != null) text.text = t;
        }
    }
}
