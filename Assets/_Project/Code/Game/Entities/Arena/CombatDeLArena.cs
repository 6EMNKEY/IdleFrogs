using System;
using System.Collections.Generic;

namespace IdleFrogs.Game
{
    // Les rareses de la config, sense ScriptableObject: es pot provar sense Unity.
    public class TaulaDeLArena
    {
        private readonly DadesDeRaresa[] _rareses;
        public double HoresDelCicle { get; }

        public TaulaDeLArena(DadesDeRaresa[] rareses, double horesDelCicle)
        {
            _rareses = rareses ?? new DadesDeRaresa[0];
            HoresDelCicle = horesDelCicle > 0 ? horesDelCicle : 24;
        }

        public DadesDeRaresa Dades(RaresaDeLluita r)
        {
            foreach (DadesDeRaresa d in _rareses) if (d != null && d.raresa == r) return d;
            return null;
        }

        public DadesDeRaresa Dades(int r) => Dades((RaresaDeLluita)r);
        public IReadOnlyList<DadesDeRaresa> Totes => _rareses;
    }

    // Un boss contra un lluitador: el lluitador li treu 'dany' per segon i el
    // boss en recupera 'regen'. Si el dany no supera la regeneració, no mor mai.
    public static class CombatDeLArena
    {
        public static double VidaDespres(double vida, double vidaMaxima, double dany, double regen, double segons)
        {
            double v = vida - (dany - regen) * Math.Max(0, segons);
            return Math.Max(0, Math.Min(vidaMaxima, v));
        }

        // Infinit = no el pot matar.
        public static double SegonsPerMatar(double vida, double dany, double regen)
        {
            if (vida <= 0) return 0;
            double net = dany - regen;
            return net > 0 ? vida / net : double.PositiveInfinity;
        }
    }

    // L'arena d'un país amb el temps: els combats avancen, els bosses moren
    // i cada cicle (24 h) en surten de nous. Tot es calcula a partir de les
    // hores guardades: amb el joc tancat també es lluita.
    public static class ModelDeLArena
    {
        public const int Pantans = 3;

        // Fa avançar l'arena fins a 'ara'. danyDe: el dany per segon d'un
        // exemplar (amb la recerca). enMorir: cada boss que mor (per donar-ne
        // els punts). bossesNous: omple els tres pantans d'un cicle nou.
        public static void Avancar(ArenaGuardada a, DateTime ara, TaulaDeLArena t, Func<int, double> danyDe,
                                   Action<BossALArena> enMorir, Action<ArenaGuardada, DateTime> bossesNous)
        {
            if (a == null || !a.desbloquejada) return;
            DateTime des = a.actualitzadaUtc != 0 ? DateTime.FromBinary(a.actualitzadaUtc) : ara;
            DateTime inici = a.iniciDelCicleUtc != 0 ? DateTime.FromBinary(a.iniciDelCicleUtc) : ara;
            TimeSpan cicle = TimeSpan.FromHours(t.HoresDelCicle);

            int voltes = 0;
            while (des < ara)
            {
                DateTime fi = inici + cicle;
                DateTime fins = ara < fi ? ara : fi;
                double segons = (fins - des).TotalSeconds;
                if (segons > 0)
                    foreach (BossALArena b in a.bosses) Lluitar(b, segons, t, danyDe, enMorir);
                des = fins;
                if (fins < fi) break;

                // S'acaba el cicle: el que quedava a mitges es perd.
                inici = fi;
                if (++voltes > 1000)
                {
                    // Molts anys fora: no cal lluitar-los un per un.
                    long salts = (ara - inici).Ticks / cicle.Ticks;
                    inici = inici.AddTicks(salts * cicle.Ticks);
                    des = inici;
                }
                a.iniciDelCicleUtc = inici.ToBinary();
                bossesNous?.Invoke(a, inici);
            }

            a.actualitzadaUtc = ara.ToBinary();
            a.iniciDelCicleUtc = inici.ToBinary();
        }

        private static void Lluitar(BossALArena b, double segons, TaulaDeLArena t, Func<int, double> danyDe,
                                    Action<BossALArena> enMorir)
        {
            if (b == null || b.mort || string.IsNullOrEmpty(b.idBoss)) return;
            DadesDeRaresa d = t.Dades(b.raresa);
            double regen = d != null ? d.regeneracio : 0;
            double dany = b.lluitador != 0 && danyDe != null ? danyDe(b.lluitador) : 0;
            if (CombatDeLArena.SegonsPerMatar(b.vida, dany, regen) <= segons)
            {
                b.vida = 0;
                b.mort = true;
                enMorir?.Invoke(b);
                return;
            }
            b.vida = CombatDeLArena.VidaDespres(b.vida, b.vidaMaxima, dany, regen, segons);
        }

        // Tres bosses nous; els lluitadors es queden al seu nenúfar.
        // triarBoss: l'id d'un boss d'aquesta raresa (o null si no n'hi ha cap).
        public static void BossesNous(ArenaGuardada a, TaulaDeLArena t, Random atzar,
                                      Func<RaresaDeLluita, Random, string> triarBoss)
        {
            var pesos = new float[6];
            for (int r = 0; r < 6; r++)
            {
                DadesDeRaresa d = t.Dades(r);
                pesos[r] = d != null ? d.probabilitatDelBoss : 0;
            }

            for (int i = 0; i < Pantans; i++)
            {
                while (a.bosses.Count <= i) a.bosses.Add(new BossALArena());
                BossALArena vell = a.bosses[i];
                var r = (RaresaDeLluita)TriarPerPes(pesos, atzar.NextDouble());
                DadesDeRaresa d = t.Dades(r);
                double vida = d != null ? d.vidaDelBoss : 1;
                a.bosses[i] = new BossALArena
                {
                    raresa = (int)r,
                    idBoss = triarBoss != null ? triarBoss(r, atzar) : r.ToString(),
                    vida = vida,
                    vidaMaxima = vida,
                    lluitador = vell != null ? vell.lluitador : 0,
                };
            }
        }

        // La raresa d'una tirada del gacha. sort: la de la recerca (0 = cap):
        // fa més probables les rareses altes.
        public static RaresaDeLluita TirarRaresa(TaulaDeLArena t, bool premium, double sort, double tirada)
        {
            var pesos = new float[6];
            for (int r = 0; r < 6; r++)
            {
                DadesDeRaresa d = t.Dades(r);
                float p = d != null ? (premium ? d.probabilitatPremium : d.probabilitatComu) : 0;
                pesos[r] = (float)(p * (1 + Math.Max(0, sort) * r / 5.0));
            }
            return (RaresaDeLluita)TriarPerPes(pesos, tirada);
        }

        // L'índex segons els pesos; tirada de 0 a 1. Sense pesos, el 0.
        public static int TriarPerPes(IList<float> pesos, double tirada)
        {
            double total = 0;
            foreach (float p in pesos) total += Math.Max(0, p);
            if (total <= 0) return 0;
            double objectiu = tirada * total, acumulat = 0;
            for (int i = 0; i < pesos.Count; i++)
            {
                if (pesos[i] <= 0) continue;
                acumulat += pesos[i];
                if (objectiu < acumulat) return i;
            }
            for (int i = pesos.Count - 1; i >= 0; i--) if (pesos[i] > 0) return i;
            return 0;
        }

        public static LluitadorGuardat Exemplar(ArenaGuardada a, int id)
        {
            if (a == null || id == 0) return null;
            foreach (LluitadorGuardat l in a.lluitadors) if (l != null && l.id == id) return l;
            return null;
        }

        // A quin pantà és un exemplar (-1 = a cap).
        public static int PantaDe(ArenaGuardada a, int id)
        {
            if (a == null || id == 0) return -1;
            for (int i = 0; i < a.bosses.Count; i++) if (a.bosses[i] != null && a.bosses[i].lluitador == id) return i;
            return -1;
        }
    }
}
