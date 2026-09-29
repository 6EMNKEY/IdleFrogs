namespace IdleFrogs.Game
{
    // Les fites d'IMT: en arribar a cert nivell, un salt que trenca l'escalat
    // constant. Els tres trams en tenen, així que el tipus és comú.
    //
    // A la font sempre venen aparellades — "×2.1 a la Velocitat i la Capacitat",
    // "×2.66 a la Capacitat i la Velocitat de Càrrega" — i això és important:
    // com que el temps es deriva de capacitat/velocitat, aplicar-les a tots dos
    // stats deixa el temps igual i multiplica la producció net.
    [System.Serializable]
    public class FitaDeMultiplicador
    {
        public int nivell;
        public double multiplicador = 2;
    }

    public static class Fites
    {
        // Producte de totes les fites assolides al nivell n.
        public static double Acumulat(FitaDeMultiplicador[] fites, int n)
        {
            if (fites == null) return 1;
            double total = 1;
            foreach (FitaDeMultiplicador f in fites)
                if (f != null && n >= f.nivell) total *= f.multiplicador;
            return total;
        }

        // A partir de quin nivell base × creixement^(n-1) es passa de rang i
        // dona Infinity. Amb creixement 1.33 passa cap al nivell 2470, molt
        // abans del que un es pensaria: posar un nivellMaxim per sobre
        // converteix les estadístiques en NaN sense avisar.
        public static int NivellMaximSegur(double valorBase, double creixement, double multiplicadorDeFites = 1)
        {
            if (creixement <= 1.0 || valorBase <= 0) return int.MaxValue;

            double arrencada = valorBase * (multiplicadorDeFites <= 0 ? 1 : multiplicadorDeFites);
            // 1e300 i no double.MaxValue: deixem marge per al que vingui després.
            double marge = (System.Math.Log(1e300) - System.Math.Log(arrencada)) / System.Math.Log(creixement);
            if (marge <= 0) return 1;
            return marge > int.MaxValue - 1 ? int.MaxValue : 1 + (int)marge;
        }
    }
}
