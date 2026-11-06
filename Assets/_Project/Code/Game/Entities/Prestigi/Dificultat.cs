namespace IdleFrogs.Game
{
    // El que multipliquen els preus del país on ets pel seu prestigi.
    public static class Dificultat
    {
        public static double Actual => GameManager.Instancia != null ? GameManager.Instancia.FactorDeDificultat : 1;
    }
}
