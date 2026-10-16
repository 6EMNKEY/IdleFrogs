using UnityEditor;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // Menú: IdleFrogs > Reiniciar partida
    //
    // Al costat de "Crear encarregats de prova", amb un separador entremig
    // (prioritat diferent) perquè no es cliqui l'un per l'altre.
    public static class EinesDePartida
    {
        [MenuItem("IdleFrogs/Reiniciar partida", priority = 1100)]
        public static void ReiniciarPartida()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reiniciar partida",
                    "S'esborrarà la partida guardada: doblers, nivells, encarregats i progrés de la guia.\n\n" +
                    (Application.isPlaying
                        ? "Com que el joc està en marxa, es tornarà a carregar l'escena i començaràs de zero."
                        : "El proper cop que li donis al Play començaràs de zero."),
                    "Esborra-la", "Cancel·la"))
                return;

            // En Play mode cal passar pel GameManager: si només esborréssim el
            // fitxer, el següent autoguardat el tornaria a escriure amb la
            // partida que hi ha en memòria.
            if (Application.isPlaying && GameManager.Instancia != null)
            {
                GameManager.Instancia.ReiniciarPartida();
                return;
            }

            ServeiDeGuardat.Esborrar();
            Debug.Log("IdleFrogs: partida esborrada. Al proper Play començaràs de zero.");
        }
    }
}
