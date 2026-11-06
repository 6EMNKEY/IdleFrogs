using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Les arenes de tots els països: lluitadors, bosses, cicles, refrescos,
    // tickets i gacha. Global (Resources/Sistemes): els combats avancen
    // estiguis on estiguis, i amb el joc tancat es posen al dia en tornar.
    [DefaultExecutionOrder(-180)]
    public class GestorDeLArena : MonoBehaviour
    {
        public static GestorDeLArena Instancia { get; private set; }

        private DadesDArena _dades = new DadesDArena();
        private readonly List<BossSO> _bosses = new List<BossSO>();
        private readonly List<LluitadorSO> _lluitadors = new List<LluitadorSO>();
        private float _rellotge;

        public ArenaConfigSO Config => ArenaConfigSO.Actual;

        // Qualsevol canvi que la UI hagi de repintar (també el compte enrere, cada segon).
        public event Action EnCanvi;
        // (zona, boss, punts): un boss ha mort.
        public event Action<int, BossALArena, double> EnBossMort;
        // S'ha obert l'arena d'un país.
        public event Action<int> EnDesbloquejar;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        private void Update()
        {
            _rellotge += Time.unscaledDeltaTime;
            if (_rellotge < 1f) return;
            _rellotge = 0f;
            for (int z = 0; z < _dades.arenes.Count; z++)
                if (_dades.arenes[z] != null && _dades.arenes[z].desbloquejada) Avancar(z);
            EnCanvi?.Invoke();
        }

        // ---------------------------------------------------------------
        // Consultes
        // ---------------------------------------------------------------

        public int TicketsComuns => _dades.ticketsComuns;
        public int TicketsPremium => _dades.ticketsPremium;
        public int Tickets(bool premium) => premium ? _dades.ticketsPremium : _dades.ticketsComuns;

        public bool Desbloquejada(int z) => z >= 0 && z < _dades.arenes.Count && _dades.arenes[z] != null && _dades.arenes[z].desbloquejada;
        public ArenaGuardada Arena(int z) => _dades.Arena(z);

        public BossALArena Boss(int z, int panta)
        {
            ArenaGuardada a = Arena(z);
            return panta >= 0 && panta < a.bosses.Count ? a.bosses[panta] : null;
        }

        public LluitadorGuardat Exemplar(int z, int id) => ModelDeLArena.Exemplar(Arena(z), id);
        public int PantaDe(int z, int id) => ModelDeLArena.PantaDe(Arena(z), id);

        public double Dany(int z, LluitadorGuardat l)
        {
            DadesDeRaresa d = l != null && Config != null ? Config.Taula.Dades(l.raresa) : null;
            return d != null ? d.Dany(l.nivell) * Recerca.Factor(TipusDEfecteDeRecerca.DanyDeLluitadors, z) : 0;
        }

        public double Regeneracio(BossALArena b)
        {
            DadesDeRaresa d = b != null && Config != null ? Config.Taula.Dades(b.raresa) : null;
            return d != null ? d.regeneracio : 0;
        }

        // Infinit: no el pot matar (o no hi ha ningú al nenúfar).
        public double SegonsPerMatar(int z, int panta)
        {
            BossALArena b = Boss(z, panta);
            if (b == null || b.mort) return 0;
            return CombatDeLArena.SegonsPerMatar(b.vida, Dany(z, Exemplar(z, b.lluitador)), Regeneracio(b));
        }

        public double SegonsDelCicle(int z)
        {
            ArenaGuardada a = Arena(z);
            if (!a.desbloquejada || Config == null) return 0;
            DateTime fi = DateTime.FromBinary(a.iniciDelCicleUtc).AddHours(Config.horesDelCicle);
            return Math.Max(0, (fi - DateTime.UtcNow).TotalSeconds);
        }

        public double PuntsDe(int z, BossALArena b)
        {
            DadesDeRaresa d = b != null && Config != null ? Config.Taula.Dades(b.raresa) : null;
            return d != null ? Math.Round(d.punts * Recerca.Factor(TipusDEfecteDeRecerca.PuntsDeBosses, z)) : 0;
        }

        // ---------------------------------------------------------------
        // Desbloquejar i fer avançar
        // ---------------------------------------------------------------

        // Comprar el pantà que toca l'obre (una vegada; el prestigi no la tanca).
        public void Desbloquejar(int z)
        {
            if (Config == null || z < 0) return;
            ArenaGuardada a = Arena(z);
            if (a.desbloquejada) return;

            DateTime ara = DateTime.UtcNow;
            a.desbloquejada = true;
            a.iniciDelCicleUtc = ara.ToBinary();
            a.actualitzadaUtc = ara.ToBinary();
            BossesNous(z, a, new System.Random(Llavor(z, ara)));
            if (!a.regalDonat)
            {
                a.regalDonat = true;
                _dades.ticketsComuns += Config.ticketsDeRegal;
            }
            Desar();
            EnDesbloquejar?.Invoke(z);
            EnCanvi?.Invoke();
        }

        public void Avancar(int z)
        {
            if (Config == null || !Desbloquejada(z)) return;
            TaulaDeLArena t = Config.Taula;
            ArenaGuardada a = Arena(z);
            ModelDeLArena.Avancar(a, DateTime.UtcNow, t,
                id => Dany(z, ModelDeLArena.Exemplar(a, id)),
                b => Morir(z, b),
                (ar, inici) => BossesNous(z, ar, new System.Random(Llavor(z, inici))));
        }

        private void Morir(int z, BossALArena b)
        {
            double punts = PuntsDe(z, b);
            if (GestorDeRecerca.Instancia != null) GestorDeRecerca.Instancia.AfegirPunts(z, punts);
            EnBossMort?.Invoke(z, b, punts);
        }

        // Cada cicle d'un país té sempre els mateixos bosses, encara que
        // l'arena es calculi més d'un cop.
        private static int Llavor(int z, DateTime inici) =>
            unchecked((int)(inici.Ticks / TimeSpan.TicksPerMinute) * 7919 + z * 104729);

        private void BossesNous(int z, ArenaGuardada a, System.Random atzar)
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisDeLaZona(z) : null;
            ModelDeLArena.BossesNous(a, Config.Taula, atzar, (r, rr) =>
            {
                Config.BossesDe(r, pais, _bosses);
                // Sense catàleg també es pot jugar: el boss és només la seva raresa.
                return _bosses.Count > 0 ? _bosses[rr.Next(_bosses.Count)].id : r.ToString();
            });
        }

        // ---------------------------------------------------------------
        // Refrescar
        // ---------------------------------------------------------------

        public int RefrescosGratisQueQueden(int z)
        {
            if (Config == null) return 0;
            ArenaGuardada a = Arena(z);
            int fets = a.diaDelsRefrescos == Calendari.Dia(DateTime.Now) ? a.refrescosGratisFets : 0;
            int total = Config.refrescosGratisAlDia + (int)Math.Round(Recerca.Suma(TipusDEfecteDeRecerca.RefrescosGratis, z));
            return Math.Max(0, total - fets);
        }

        public int GemesPerRefrescar => Config != null ? Config.gemesPerRefresc : 0;

        // Tres bosses nous ara mateix i el cicle torna a començar. Gratis si en
        // queden avui; si no, amb gemes.
        public bool Refrescar(int z)
        {
            if (Config == null || !Desbloquejada(z)) return false;
            Avancar(z);
            ArenaGuardada a = Arena(z);
            int avui = Calendari.Dia(DateTime.Now);
            if (RefrescosGratisQueQueden(z) > 0)
            {
                if (a.diaDelsRefrescos != avui) { a.diaDelsRefrescos = avui; a.refrescosGratisFets = 0; }
                a.refrescosGratisFets++;
            }
            else if (CurrencyManager.Instancia == null || !CurrencyManager.Instancia.GastarGemes(Config.gemesPerRefresc))
                return false;

            DateTime ara = DateTime.UtcNow;
            a.iniciDelCicleUtc = ara.ToBinary();
            a.actualitzadaUtc = ara.ToBinary();
            BossesNous(z, a, new System.Random(Llavor(z, ara) ^ a.refrescosGratisFets));
            Desar();
            EnCanvi?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------
        // Lluitadors
        // ---------------------------------------------------------------

        // Posa un exemplar al nenúfar d'un pantà (0 = treure'l). Si era a un
        // altre, en surt.
        public bool Assignar(int z, int panta, int id)
        {
            ArenaGuardada a = Arena(z);
            if (!a.desbloquejada || panta < 0 || panta >= a.bosses.Count) return false;
            if (id != 0 && ModelDeLArena.Exemplar(a, id) == null) return false;
            // El temps d'abans es lluita amb qui hi havia.
            Avancar(z);
            foreach (BossALArena b in a.bosses) if (b != null && id != 0 && b.lluitador == id) b.lluitador = 0;
            a.bosses[panta].lluitador = id;
            Desar();
            EnCanvi?.Invoke();
            return true;
        }

        public double CostDeMillora(LluitadorGuardat l)
        {
            DadesDeRaresa d = l != null && Config != null ? Config.Taula.Dades(l.raresa) : null;
            return d != null ? d.CostDeMillora(l.nivell) : 0;
        }

        public bool AlMaxim(LluitadorGuardat l)
        {
            DadesDeRaresa d = l != null && Config != null ? Config.Taula.Dades(l.raresa) : null;
            return d == null || l.nivell >= d.nivellMaxim;
        }

        // Es paga amb la moneda del país del lluitador.
        public bool Millorar(int z, int id)
        {
            LluitadorGuardat l = Exemplar(z, id);
            if (l == null || AlMaxim(l) || GameManager.Instancia == null) return false;
            Avancar(z);
            if (!GameManager.Instancia.GastarDoblersDeLaZona(z, CostDeMillora(l))) return false;
            l.nivell++;
            Desar();
            EnCanvi?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------
        // Tickets i gacha
        // ---------------------------------------------------------------

        public void AfegirTickets(bool premium, int quantitat)
        {
            if (premium) _dades.ticketsPremium = Math.Max(0, _dades.ticketsPremium + quantitat);
            else _dades.ticketsComuns = Math.Max(0, _dades.ticketsComuns + quantitat);
            EnCanvi?.Invoke();
        }

        // Gasta tickets i dona lluitadors del país de l'arena. Buida si no n'hi ha prou.
        public List<LluitadorGuardat> Tirar(int z, bool premium, int quantes)
        {
            var nous = new List<LluitadorGuardat>();
            if (Config == null || !Desbloquejada(z) || quantes <= 0 || Tickets(premium) < quantes) return nous;
            AfegirTickets(premium, -quantes);

            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisDeLaZona(z) : null;
            double sort = Recerca.Suma(TipusDEfecteDeRecerca.SortAlGacha, z);
            ArenaGuardada a = Arena(z);
            for (int i = 0; i < quantes; i++)
            {
                RaresaDeLluita r = ModelDeLArena.TirarRaresa(Config.Taula, premium, sort, UnityEngine.Random.value);
                Config.LluitadorsDe(r, pais, _lluitadors);
                var l = new LluitadorGuardat
                {
                    id = _dades.seguentId++,
                    idDefinicio = _lluitadors.Count > 0 ? _lluitadors[UnityEngine.Random.Range(0, _lluitadors.Count)].id : r.ToString(),
                    raresa = (int)r,
                    nivell = 1,
                };
                a.lluitadors.Add(l);
                nous.Add(l);
            }
            Desar();
            EnCanvi?.Invoke();
            return nous;
        }

        // ---------------------------------------------------------------
        // Guardat
        // ---------------------------------------------------------------

        private static void Desar()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.SaveGameData();
        }

        public DadesDArena Guardar() => _dades;

        // Es posa al dia: el temps que el joc ha estat tancat també s'ha lluitat.
        public void Carregar(DadesDArena dades)
        {
            _dades = dades ?? new DadesDArena();
            if (_dades.arenes == null) _dades.arenes = new List<ArenaGuardada>();
            foreach (ArenaGuardada a in _dades.arenes)
            {
                if (a == null) continue;
                if (a.lluitadors == null) a.lluitadors = new List<LluitadorGuardat>();
                if (a.bosses == null) a.bosses = new List<BossALArena>();
            }
            if (_dades.seguentId <= 0) _dades.seguentId = 1;
            for (int z = 0; z < _dades.arenes.Count; z++) Avancar(z);
            EnCanvi?.Invoke();
        }
    }
}
