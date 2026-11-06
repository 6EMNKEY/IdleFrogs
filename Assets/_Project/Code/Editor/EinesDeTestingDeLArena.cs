using UnityEditor;
using IdleFrogs.Game;

namespace IdleFrogs.EditorTools
{
    // Menú: IdleFrogs > Testing > Arena / Recerca. Només amb el joc en marxa.
    public static class EinesDeTestingDeLArena
    {
        private const string Menu = "IdleFrogs/Testing/";
        private const int Prioritat = 1300;

        private static int Zona => GameManager.Instancia != null ? GameManager.Instancia.Zona : 0;

        [MenuItem(Menu + "Arena: obrir la del país", priority = Prioritat)]
        private static void Obrir() => GestorDeLArena.Instancia.Desbloquejar(Zona);

        [MenuItem(Menu + "Arena: +10 tickets comuns", priority = Prioritat + 1)]
        private static void Comuns() => GestorDeLArena.Instancia.AfegirTickets(false, 10);

        [MenuItem(Menu + "Arena: +10 tickets prèmium", priority = Prioritat + 2)]
        private static void Premium() => GestorDeLArena.Instancia.AfegirTickets(true, 10);

        [MenuItem(Menu + "Recerca: +100 punts de cada color", priority = Prioritat + 3)]
        private static void Punts()
        {
            for (int z = 0; z < 6; z++) GestorDeRecerca.Instancia.AfegirPunts(z, 100);
        }

        [MenuItem(Menu + "Arena: obrir la del país", true)]
        [MenuItem(Menu + "Arena: +10 tickets comuns", true)]
        [MenuItem(Menu + "Arena: +10 tickets prèmium", true)]
        private static bool HiHaArena() => EditorApplication.isPlaying && GestorDeLArena.Instancia != null;

        [MenuItem(Menu + "Recerca: +100 punts de cada color", true)]
        private static bool HiHaRecerca() => EditorApplication.isPlaying && GestorDeRecerca.Instancia != null;
    }
}
