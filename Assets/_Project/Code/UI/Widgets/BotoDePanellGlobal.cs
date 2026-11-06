using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    public enum AccioDeBoto { Botiga, Configuracio, Mapa, TornarAlPais }

    // Un botó del HUD que obre una cosa que no és de l'escena: la botiga i
    // els ajustos viuen a UIGlobal (fora de les escenes), així que el panell
    // no pot tenir una referència al botó. El botó els crida a ells.
    [RequireComponent(typeof(Button))]
    public class BotoDePanellGlobal : MonoBehaviour
    {
        [SerializeField] private AccioDeBoto accio;

        private void Awake() => GetComponent<Button>().onClick.AddListener(Fer);

        private void Fer()
        {
            switch (accio)
            {
                case AccioDeBoto.Botiga:
                    if (PanellDeBotiga.Instancia != null) PanellDeBotiga.Instancia.Obrir();
                    break;
                case AccioDeBoto.Configuracio:
                    if (PanellDeConfiguracio.Instancia != null) PanellDeConfiguracio.Instancia.Obrir();
                    break;
                case AccioDeBoto.Mapa:
                    GestorDAudio.Sonar(So.Obrir);
                    if (GameManager.Instancia != null) GameManager.Instancia.AnarAlMapa();
                    break;
                case AccioDeBoto.TornarAlPais:
                    GestorDAudio.Sonar(So.Tancar);
                    if (GameManager.Instancia != null) GameManager.Instancia.TornarAlPais();
                    break;
            }
        }
    }
}
