using System.Text;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Com es diuen els valors i els costos dels nodes.
    public static class TextosDeRecerca
    {
        // "+10 %", "−3 %", "+10 min", "+1".
        public static string Valor(TipusDEfecteDeRecerca tipus, double valor)
        {
            string signe = valor >= 0 ? "+" : "−";
            double v = System.Math.Abs(valor);
            switch (tipus)
            {
                case TipusDEfecteDeRecerca.AnunciBarrera:
                case TipusDEfecteDeRecerca.AnunciEtFalten:
                case TipusDEfecteDeRecerca.AnunciPotenciador:
                    return $"{signe}{FormatadorDeNombres.Factor(v)} min";
                case TipusDEfecteDeRecerca.RefrescosGratis:
                    return $"{signe}{FormatadorDeNombres.Factor(v)}";
                case TipusDEfecteDeRecerca.AnunciOffline:
                case TipusDEfecteDeRecerca.AnunciRegal:
                    return $"x{FormatadorDeNombres.Factor(2 + valor)}";
                default:
                    return $"{signe}{FormatadorDeNombres.Factor(v * 100)}%";
            }
        }

        public static string Nom(NodeDeRecerca n)
        {
            string t = n != null ? n.nom.Text : null;
            return string.IsNullOrEmpty(t) ? n?.id ?? string.Empty : t;
        }

        // L'explicació amb el total d'ara i el del nivell següent.
        public static string Descripcio(NodeDeRecerca n, int nivell)
        {
            if (n == null) return string.Empty;
            string ara = Valor(n.efecte, n.valorPerNivell * nivell);
            string seguent = Valor(n.efecte, n.valorPerNivell * System.Math.Min(nivell + 1, n.nivellMaxim));
            string plantilla = n.descripcio.Text;
            if (string.IsNullOrEmpty(plantilla)) return $"{ara} → {seguent}";
            try { return string.Format(plantilla, ara, seguent); }
            catch (System.FormatException) { return plantilla; }
        }

        // "12🔍 + 5🔍", cada un del color del seu país.
        public static string Cost(NodeDeRecerca n, int nivell, ArbreDeRecercaSO arbre)
        {
            var sb = new StringBuilder();
            foreach (CostDeRecerca c in n.cost)
            {
                if (c == null) continue;
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(Icones.Punts(c.AlNivell(nivell), arbre != null ? arbre.ColorDe(c.pais) : UnityEngine.Color.white));
            }
            return sb.ToString();
        }
    }
}
