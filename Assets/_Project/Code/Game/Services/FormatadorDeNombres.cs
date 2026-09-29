using System;
using System.Globalization;

namespace IdleFrogs.Game
{
    // Un sol lloc per formatar números. Amb el multiplicador de profunditat
    // els doblers se surten de ToString("N0") de seguida, i si cada pantalla
    // se'l formata pel seu compte acabem amb quatre formats diferents.
    public static class FormatadorDeNombres
    {
        private static readonly string[] SufixosCurts = { "", "K", "M", "B", "T" };

        // Cultura invariant a propòsit: si deixem la del dispositiu, el
        // mateix número surt "1.50K" en un mòbil i "1,50K" en un altre.
        private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

        public static string Doblers(double valor)
        {
            if (double.IsNaN(valor) || double.IsInfinity(valor)) return "0";
            if (valor < 0) return "-" + Doblers(-valor);
            if (valor < 1000)
                return valor < 10 ? valor.ToString("0.##", Cultura) : Math.Floor(valor).ToString("0", Cultura);

            int index = 0;
            while (valor >= 1000.0 && index < 120)
            {
                valor /= 1000.0;
                index++;
            }

            string format = valor < 10 ? "0.00" : valor < 100 ? "0.0" : "0";
            return valor.ToString(format, Cultura) + Sufix(index);
        }

        // Per segon
        public static string Ritme(double valor) => Doblers(valor) + "/s";

        // Números petits (velocitats): també amb cultura invariant.
        public static string Decimals(double valor, int xifres) =>
            valor.ToString("F" + xifres, Cultura);

        // El delta que ensenya el panell de millora: sempre amb signe.
        public static string Delta(double valor)
        {
            if (valor <= 0) return "+0";
            return "+" + Doblers(valor);
        }

        // K, M, B, T i després aa, ab, ac... com fan els incrementals.
        private static string Sufix(int index)
        {
            if (index < SufixosCurts.Length) return SufixosCurts[index];

            int n = index - SufixosCurts.Length;
            if (n >= 26 * 26) return "e" + (index * 3);

            char primera = (char)('a' + n / 26);
            char segona = (char)('a' + n % 26);
            return string.Concat(primera, segona);
        }

        // Per als temporitzadors de barrera.
        public static string Temps(double segons)
        {
            if (segons <= 0) return "0s";
            TimeSpan t = TimeSpan.FromSeconds(Math.Ceiling(segons));
            if (t.TotalDays >= 1) return $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }
    }
}
