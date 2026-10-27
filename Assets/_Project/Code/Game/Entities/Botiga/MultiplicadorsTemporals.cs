using System;
using System.Collections.Generic;

namespace IdleFrogs.Game
{
    [Serializable]
    public class MultiplicadorActiu
    {
        public double factor;
        public double segonsRestants;
    }

    // Els x2, x5, x1000 que donen la botiga, el regal i el calendari.
    //
    // Només en corre un: el més fort. Els altres esperen amb el seu temps
    // intacte i continuen quan el fort s'acaba. Dos del mateix factor no es
    // multipliquen: se sumen els temps (amb un tope opcional, el dels anuncis).
    // El temps passa també amb el joc tancat.
    public class MultiplicadorsTemporals
    {
        private readonly List<MultiplicadorActiu> _actius = new List<MultiplicadorActiu>();

        public IReadOnlyList<MultiplicadorActiu> Actius => _actius;

        public void Afegir(double factor, double segons, double segonsMaxims = 0)
        {
            if (factor <= 1 || segons <= 0) return;
            MultiplicadorActiu mateix = _actius.Find(m => Math.Abs(m.factor - factor) < 1e-9);
            if (mateix == null)
            {
                mateix = new MultiplicadorActiu { factor = factor, segonsRestants = 0 };
                _actius.Add(mateix);
            }
            mateix.segonsRestants += segons;
            if (segonsMaxims > 0 && mateix.segonsRestants > segonsMaxims) mateix.segonsRestants = segonsMaxims;
        }

        // El que corre ara (el factor més alt), o null.
        public MultiplicadorActiu Actiu
        {
            get
            {
                MultiplicadorActiu millor = null;
                foreach (MultiplicadorActiu m in _actius)
                    if (m.segonsRestants > 0 && (millor == null || m.factor > millor.factor)) millor = m;
                return millor;
            }
        }

        public double Factor => Actiu?.factor ?? 1;

        // Fa córrer el temps: primer s'acaba el més fort, i el que sobra
        // passa al següent.
        public void Avancar(double segons)
        {
            while (segons > 0)
            {
                MultiplicadorActiu m = Actiu;
                if (m == null) break;
                double gastat = Math.Min(segons, m.segonsRestants);
                m.segonsRestants -= gastat;
                segons -= gastat;
            }
            _actius.RemoveAll(m => m.segonsRestants <= 0);
        }

        public MultiplicadorActiu[] Guardar() =>
            _actius.ConvertAll(m => new MultiplicadorActiu { factor = m.factor, segonsRestants = m.segonsRestants }).ToArray();

        public void Carregar(MultiplicadorActiu[] dades)
        {
            _actius.Clear();
            if (dades == null) return;
            foreach (MultiplicadorActiu m in dades)
                if (m != null && m.factor > 1 && m.segonsRestants > 0) Afegir(m.factor, m.segonsRestants);
        }
    }

    // Quin dia del calendari toca i si ja s'ha recollit avui. Els dies són
    // del rellotge del dispositiu (mitjanit local).
    public static class Calendari
    {
        public static int Dia(DateTime local) => local.Year * 10000 + local.Month * 100 + local.Day;

        public static bool PotRecollir(int ultimDiaRecollit, int avui) => avui > ultimDiaRecollit;

        // Primer el mes inicial; després el cicle, que es repeteix.
        public static DiaDelCalendari DelDia(IList<DiaDelCalendari> mesInicial, IList<DiaDelCalendari> cicle, int recollits)
        {
            int n = Math.Max(0, recollits);
            int mes = mesInicial?.Count ?? 0;
            if (n < mes) return mesInicial[n];
            if (cicle == null || cicle.Count == 0) return null;
            return cicle[(n - mes) % cicle.Count];
        }

        // El número que veu el jugador: "Dia 3/30" al mes inicial, "Dia 3/7" al cicle.
        public static (int dia, int de) Numero(int mesInicial, int cicle, int recollits)
        {
            int n = Math.Max(0, recollits);
            if (n < mesInicial) return (n + 1, mesInicial);
            if (cicle <= 0) return (0, 0);
            return ((n - mesInicial) % cicle + 1, cicle);
        }
    }
}
