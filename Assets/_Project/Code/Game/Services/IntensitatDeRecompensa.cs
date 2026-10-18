namespace IdleFrogs.Game
{
    // Com de gros ha de ser l'esclat d'una recompensa, de 0 (el mínim) a 1
    // (el màxim). Es mesura contra el que el jugador ja tenia d'aquella
    // moneda: 1.000 doblers són una festa si en tenies 500 i res si en tens
    // un milió.
    public static class IntensitatDeRecompensa
    {
        // Arrel quadrada de la proporció: un 25% ja arriba a mig camí. Amb la
        // proporció tal qual, tot el que no dobla el que tens quedaria al mínim.
        public static float Calcular(double recompensa, double teniaAbans, bool esCompra = false)
        {
            if (esCompra) return 1f;
            if (recompensa <= 0) return 0f;
            if (teniaAbans <= 0 || recompensa >= teniaAbans) return 1f;
            return (float)System.Math.Sqrt(recompensa / teniaAbans);
        }
    }
}
