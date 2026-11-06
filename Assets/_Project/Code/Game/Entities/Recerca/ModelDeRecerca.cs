using System;
using System.Collections.Generic;

namespace IdleFrogs.Game
{
    public enum EstatDeNode { Bloquejat, Disponible, Maxim }

    // La recerca sense Unity: què es pot comprar, què costa i quant suma.
    public static class ModelDeRecerca
    {
        public static EstatDeNode Estat(NodeDeRecerca n, DadesDeRecerca d)
        {
            if (n == null) return EstatDeNode.Bloquejat;
            if (d.Nivell(n.id) >= n.nivellMaxim) return EstatDeNode.Maxim;
            if (n.prerequisits != null)
                foreach (string p in n.prerequisits)
                    if (!string.IsNullOrEmpty(p) && d.Nivell(p) <= 0) return EstatDeNode.Bloquejat;
            return EstatDeNode.Disponible;
        }

        public static bool LiArriba(NodeDeRecerca n, DadesDeRecerca d)
        {
            int nivell = d.Nivell(n.id);
            foreach (CostDeRecerca c in n.cost)
                if (c != null && d.Punts(c.pais) < c.AlNivell(nivell)) return false;
            return true;
        }

        public static bool Comprar(NodeDeRecerca n, DadesDeRecerca d)
        {
            if (Estat(n, d) != EstatDeNode.Disponible || !LiArriba(n, d)) return false;
            int nivell = d.Nivell(n.id);
            foreach (CostDeRecerca c in n.cost)
                if (c != null) d.AfegirPunts(c.pais, -c.AlNivell(nivell));
            d.PosarNivell(n.id, nivell + 1);
            return true;
        }

        // La suma d'un tipus d'efecte al país 'zona' (els de tots i els seus).
        public static double Suma(IList<NodeDeRecerca> nodes, DadesDeRecerca d, TipusDEfecteDeRecerca tipus, int zona)
        {
            double suma = 0;
            if (nodes == null || d == null) return 0;
            foreach (NodeDeRecerca n in nodes)
                if (n != null && n.efecte == tipus && n.TocaElPais(zona))
                    suma += n.valorPerNivell * Math.Min(d.Nivell(n.id), n.nivellMaxim);
            return suma;
        }

        // El total de punts que costa l'arbre sencer, per color.
        public static double[] CostTotal(IList<NodeDeRecerca> nodes, int colors)
        {
            var total = new double[colors];
            foreach (NodeDeRecerca n in nodes)
                for (int nivell = 0; nivell < n.nivellMaxim; nivell++)
                    foreach (CostDeRecerca c in n.cost)
                        if (c != null && c.pais < colors) total[c.pais] += c.AlNivell(nivell);
            return total;
        }
    }
}
