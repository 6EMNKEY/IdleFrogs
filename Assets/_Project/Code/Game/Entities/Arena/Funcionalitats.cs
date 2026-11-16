namespace IdleFrogs.Game
{
    // Interruptors de les parts del joc que es poden apagar per publicar sense
    // elles. L'arena i la recerca van juntes: els punts de recerca només surten
    // de l'arena. Es canvia a Resources/Arena (Activa).
    public static class Funcionalitats
    {
        public static bool ArenaIRecerca
        {
            get
            {
                ArenaConfigSO c = ArenaConfigSO.Actual;
                return c != null && c.activa;
            }
        }
    }
}
