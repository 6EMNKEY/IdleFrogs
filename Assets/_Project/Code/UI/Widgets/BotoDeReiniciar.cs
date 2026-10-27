using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Botó de reiniciar la partida. Esborra-ho tot, així que primer ho
    // pregunta amb la finestra de confirmació.
    //
    // Va en un Button de la UI (Canvas), no al món: no necessita collider.
    // El seu text, amb TextLocalitzat i la clau ui.reiniciar.
    [RequireComponent(typeof(Button))]
    public class BotoDeReiniciar : MonoBehaviour
    {
        private void Awake() => GetComponent<Button>().onClick.AddListener(EnPremer);

        private void EnPremer()
        {
            FinestraDeConfirmacio.Preguntar(
                Textos.T("conf.reiniciar.titol"),
                Textos.T("conf.reiniciar.text"),
                Reiniciar,
                textAcceptar: Textos.T("conf.reiniciar.boto"),
                perillosa: true);
        }

        private static void Reiniciar()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.ReiniciarPartida();
        }
    }
}
