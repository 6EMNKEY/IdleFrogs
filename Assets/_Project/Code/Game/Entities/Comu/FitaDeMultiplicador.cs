namespace IdleFrogs.Game
{
    // Les fites: en arribar a cert nivell, un salt que trenca l'escalat
    // constant. Els tres trams en tenen, així que el tipus és comú.
    //
    // Cada fita multiplica dos stats alhora — la velocitat i la capacitat —
    // i això és important: com que el temps es deriva de capacitat/velocitat,
    // aplicar-la a tots dos deixa el temps igual i multiplica la producció net.
    [System.Serializable]
    public class FitaDeMultiplicador
    {
        public int nivell;
        public double multiplicador = 2;

        [UnityEngine.Tooltip("Gemes que dona en arribar-hi. Surten a la barra del panell de millora")]
        [UnityEngine.Min(0)] public int gemes;
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

        // La fita més baixa que encara no tens, o null si ja les tens totes.
        // No es dona per fet que l'array estigui ordenat: s'edita a mà.
        public static FitaDeMultiplicador Seguent(FitaDeMultiplicador[] fites, int n)
        {
            if (fites == null) return null;
            FitaDeMultiplicador millor = null;
            foreach (FitaDeMultiplicador f in fites)
                if (f != null && f.nivell > n && (millor == null || f.nivell < millor.nivell))
                    millor = f;
            return millor;
        }

        // On comença la barra: el nivell de l'última fita assolida, o 1 si
        // encara no n'has passat cap.
        public static int NivellDeLAnterior(FitaDeMultiplicador[] fites, int n)
        {
            int anterior = 1;
            if (fites == null) return anterior;
            foreach (FitaDeMultiplicador f in fites)
                if (f != null && f.nivell <= n && f.nivell > anterior) anterior = f.nivell;
            return anterior;
        }

        // 0..1 de l'última fita a la següent. Plena quan ja no en queden.
        public static float Progres(FitaDeMultiplicador[] fites, int n)
        {
            FitaDeMultiplicador seguent = Seguent(fites, n);
            if (seguent == null) return 1f;

            int anterior = NivellDeLAnterior(fites, n);
            int tram = seguent.nivell - anterior;
            if (tram <= 0) return 0f;
            return UnityEngine.Mathf.Clamp01((float)(n - anterior) / tram);
        }

        // Les gemes de totes les fites que es creuen pujant de 'desde' a
        // 'fins'. Amb ×50 se'n poden passar dues de cop i es donen totes.
        public static int GemesEntre(FitaDeMultiplicador[] fites, int desde, int fins)
        {
            if (fites == null) return 0;
            int total = 0;
            foreach (FitaDeMultiplicador f in fites)
                if (f != null && f.nivell > desde && f.nivell <= fins) total += f.gemes;
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
