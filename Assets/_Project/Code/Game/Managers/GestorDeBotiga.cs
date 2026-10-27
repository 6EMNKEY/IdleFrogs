using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum EstatDeProducte { Disponible, NoArriba, Esperant, Esgotat, Caducat }

    // La botiga: què es pot comprar, què passa en comprar-ho, el calendari
    // diari, el pase mensual, els multiplicadors i els efectes permanents.
    // La UI només pregunta i crida; tot el que es guarda és aquí.
    [DefaultExecutionOrder(-180)]
    public class GestorDeBotiga : MonoBehaviour
    {
        public static GestorDeBotiga Instancia { get; private set; }

        [SerializeField] private CatalegDeBotiga cataleg;
        [Header("Pagar amb gemes el que falta")]
        [Tooltip("Gemes que costa una hora d'ingressos quan falten monedes (igual que a la botiga)")]
        [Min(1)] [SerializeField] private float gemesPerHoraDIngressos = 60f;
        [Tooltip("El mínim: el que falta, per poc que sigui, no costa menys")]
        [Min(1)] [SerializeField] private int minimDeGemes = 5;
        [Tooltip("Per sobre d'això no s'ofereix pagar amb gemes: només \"Et falten X\"")]
        [Min(1)] [SerializeField] private int maximDeGemes = 100000;

        public CatalegDeBotiga Cataleg => cataleg;

        // Ara els de prova; els de debò es posen aquí quan existeixin.
        public IServeiDeCompres Compres { get; set; } = new CompresDeProva();
        public IServeiDAnuncis Anuncis { get; set; } = new AnuncisDeProva();

        private readonly Dictionary<string, CompraGuardada> _compres = new Dictionary<string, CompraGuardada>();
        private readonly MultiplicadorsTemporals _multiplicadors = new MultiplicadorsTemporals();
        private DadesDeBotiga _dades = new DadesDeBotiga();
        private double _factorAbans = 1;
        private float _rellotge;

        // Qualsevol canvi que la UI hagi de repintar: compres, temps, calendari.
        public event Action EnCanvi;

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

        // El temps dels multiplicadors corre en temps real; la UI es repinta
        // un cop per segon (comptes enrere) i quan canvia el multiplicador.
        private void Update()
        {
            _multiplicadors.Avancar(Time.unscaledDeltaTime);
            _rellotge += Time.unscaledDeltaTime;
            double factor = FactorDIngressos;
            if (_rellotge >= 1f || Math.Abs(factor - _factorAbans) > 1e-9)
            {
                _rellotge = 0f;
                _factorAbans = factor;
                EnCanvi?.Invoke();
            }
        }

        // ---------------------------------------------------------------
        // Multiplicadors i permanents
        // ---------------------------------------------------------------

        public double FactorPermanent => _dades.x2Permanent ? 2 : 1;
        // El que multiplica el que cobra el restaurant ara mateix.
        public double FactorDIngressos => _multiplicadors.Factor * FactorPermanent;
        public MultiplicadorActiu MultiplicadorActiu => _multiplicadors.Actiu;
        public bool SenseAnuncis => _dades.senseAnuncis;
        public bool OfflineX2 => _dades.offlineX2;

        // ---------------------------------------------------------------
        // Estat dels productes
        // ---------------------------------------------------------------

        private CompraGuardada Registre(ProducteDeBotiga p)
        {
            if (!_compres.TryGetValue(p.id, out CompraGuardada c))
            {
                c = new CompraGuardada { id = p.id };
                _compres[p.id] = c;
            }
            return c;
        }

        private static int Avui => Calendari.Dia(DateTime.Now);

        public int Vegades(ProducteDeBotiga p) => p != null && _compres.TryGetValue(p.id, out CompraGuardada c) ? c.vegades : 0;

        private int VegadesAvui(ProducteDeBotiga p) =>
            p != null && _compres.TryGetValue(p.id, out CompraGuardada c) && c.diaDeVegadesAvui == Avui ? c.vegadesAvui : 0;

        // Quantes en queden: del límit total o del diari, el més petit. -1 = sense límit.
        public int Restants(ProducteDeBotiga p)
        {
            if (p == null) return 0;
            int restants = -1;
            if (p.limit > 0) restants = Math.Max(0, p.limit - Vegades(p));
            if (p.limitAlDia > 0)
            {
                int avui = Math.Max(0, p.limitAlDia - VegadesAvui(p));
                restants = restants < 0 ? avui : Math.Min(restants, avui);
            }
            return restants;
        }

        public double SegonsFinsDisponible(ProducteDeBotiga p)
        {
            if (p == null || p.horesEntreUsos <= 0) return 0;
            if (!_compres.TryGetValue(p.id, out CompraGuardada c) || c.ultimaUtc == 0) return 0;
            DateTime proper = DateTime.FromBinary(c.ultimaUtc).AddHours(p.horesEntreUsos);
            return Math.Max(0, (proper - DateTime.UtcNow).TotalSeconds);
        }

        // Les ofertes amb temps comencen a comptar la primera vegada que es veuen.
        public void MarcarComVist(ProducteDeBotiga p)
        {
            if (p == null || p.horesDOferta <= 0) return;
            CompraGuardada c = Registre(p);
            if (c.primeraVistaUtc == 0) c.primeraVistaUtc = DateTime.UtcNow.ToBinary();
        }

        // -1 = sense temps.
        public double SegonsDOferta(ProducteDeBotiga p)
        {
            if (p == null || p.horesDOferta <= 0) return -1;
            if (!_compres.TryGetValue(p.id, out CompraGuardada c) || c.primeraVistaUtc == 0) return p.horesDOferta * 3600.0;
            DateTime fi = DateTime.FromBinary(c.primeraVistaUtc).AddHours(p.horesDOferta);
            return Math.Max(0, (fi - DateTime.UtcNow).TotalSeconds);
        }

        public EstatDeProducte Estat(ProducteDeBotiga p)
        {
            if (p == null) return EstatDeProducte.Esgotat;
            if (p.EsPermanent && TeElPermanent(p)) return EstatDeProducte.Esgotat;
            if (Restants(p) == 0) return EstatDeProducte.Esgotat;
            if (SegonsDOferta(p) == 0) return EstatDeProducte.Caducat;
            if (SegonsFinsDisponible(p) > 0) return EstatDeProducte.Esperant;
            if (p.pagament == TipusDePagament.Gemes &&
                (CurrencyManager.Instancia == null || CurrencyManager.Instancia.gemesActuals < p.preuEnGemes))
                return EstatDeProducte.NoArriba;
            return EstatDeProducte.Disponible;
        }

        private bool TeElPermanent(ProducteDeBotiga p)
        {
            foreach (Recompensa r in p.recompenses)
                if (r != null && r.tipus == TipusDeRecompensa.Permanent && !TePermanent(r.efectePermanent)) return false;
            return true;
        }

        private bool TePermanent(EfectePermanent e) => e switch
        {
            EfectePermanent.X2Ingressos => _dades.x2Permanent,
            EfectePermanent.SenseAnuncis => _dades.senseAnuncis,
            _ => _dades.offlineX2
        };

        // El preu en text: gemes, gratis, anunci o el de la botiga real.
        public string PreuReal(ProducteDeBotiga p) =>
            p == null ? string.Empty : Compres?.PreuLocalitzat(p.idDeCompra) ?? p.preuDeMostra;

        // ---------------------------------------------------------------
        // Comprar
        // ---------------------------------------------------------------

        // enAcabar rep les recompenses donades (buida si no s'ha pogut).
        public void Comprar(ProducteDeBotiga p, Action<List<Recompensa>> enAcabar = null)
        {
            var donades = new List<Recompensa>();
            if (Estat(p) != EstatDeProducte.Disponible)
            {
                enAcabar?.Invoke(donades);
                return;
            }

            switch (p.pagament)
            {
                case TipusDePagament.Gratis:
                    enAcabar?.Invoke(Lliurar(p));
                    break;

                case TipusDePagament.Gemes:
                    if (CurrencyManager.Instancia != null && CurrencyManager.Instancia.GastarGemes(p.preuEnGemes))
                        donades = Lliurar(p);
                    enAcabar?.Invoke(donades);
                    break;

                case TipusDePagament.Anunci:
                    if (SenseAnuncis) { enAcabar?.Invoke(Lliurar(p)); break; }
                    Anuncis.Mostrar(vist => enAcabar?.Invoke(vist ? Lliurar(p) : donades));
                    break;

                case TipusDePagament.DinerReal:
                    Compres.Comprar(p.idDeCompra, fet => enAcabar?.Invoke(fet ? Lliurar(p) : donades));
                    break;
            }
        }

        private List<Recompensa> Lliurar(ProducteDeBotiga p)
        {
            CompraGuardada c = Registre(p);
            bool primera = c.vegades == 0;
            c.vegades++;
            if (c.diaDeVegadesAvui != Avui) { c.diaDeVegadesAvui = Avui; c.vegadesAvui = 0; }
            c.vegadesAvui++;
            c.ultimaUtc = DateTime.UtcNow.ToBinary();

            var donades = new List<Recompensa>();
            if (p.unaAlAtzar)
            {
                Recompensa r = Recompensa.TriarPerPes(p.recompenses, UnityEngine.Random.value);
                if (r != null) donades.Add(r);
            }
            else donades.AddRange(p.recompenses);

            foreach (Recompensa r in donades) Donar(r, primera && p.doblePrimeraCompra);

            Desar();
            return donades;
        }

        // Dona una recompensa, vingui d'on vingui (botiga, calendari, regal).
        public void Donar(Recompensa r, bool doble = false)
        {
            if (r == null) return;
            switch (r.tipus)
            {
                case TipusDeRecompensa.Gemes:
                    if (CurrencyManager.Instancia != null)
                        CurrencyManager.Instancia.AfegirGemes((int)Math.Round(r.quantitat * (doble ? 2 : 1)));
                    break;

                case TipusDeRecompensa.TempsDIngressos:
                    if (CurrencyManager.Instancia != null)
                        CurrencyManager.Instancia.AfegirDoblers(DoblersDeTemps(r.quantitat * 60));
                    break;

                case TipusDeRecompensa.Multiplicador:
                    _multiplicadors.Afegir(r.multiplicador, r.minuts * 60, r.minutsMaxims * 60);
                    break;

                case TipusDeRecompensa.Encarregat:
                    GestorDEncarregats.Instancia?.Regalar(r.raresa);
                    break;

                case TipusDeRecompensa.Cofre:
                    GestorDEncarregats.Instancia?.Regalar(r.TirarRaresa(UnityEngine.Random.value));
                    break;

                case TipusDeRecompensa.Permanent:
                    if (r.efectePermanent == EfectePermanent.X2Ingressos) _dades.x2Permanent = true;
                    else if (r.efectePermanent == EfectePermanent.SenseAnuncis) _dades.senseAnuncis = true;
                    else _dades.offlineX2 = true;
                    break;

                case TipusDeRecompensa.PaseMensual:
                    // Si ja en tens un, s'allarga.
                    DateTime avui = DateTime.Now.Date;
                    DateTime fi = _dades.paseFiTicks > avui.Ticks ? new DateTime(_dades.paseFiTicks) : avui;
                    _dades.paseFiTicks = fi.AddDays(r.dies).Ticks;
                    _dades.paseGemesPerDia = r.quantitat;
                    break;
            }
            EnCanvi?.Invoke();
        }

        // Temps d'ingressos = el ritme de tota la cadena del país on ets (el
        // coll d'ampolla) pel temps, amb el x2 permanent si el tens. Sense les
        // habilitats enceses: si no, encendre-les abans de cobrar o de pagar
        // amb gemes sortiria a compte.
        public double DoblersDeTemps(double segons)
        {
            if (GameManager.Instancia == null) return 0;
            return GameManager.Instancia.Mesures(senseHabilitats: true).CollDAmpolla * segons * FactorPermanent;
        }

        // Les gemes que valen uns doblers que falten: el temps que trigaries a
        // guanyar-los al ritme de la cadena (sense habilitats), al preu de la
        // botiga. 0 = no s'ofereix pagar amb gemes.
        public int GemesPer(double doblers) =>
            CalcularGemes(doblers, DoblersDeTemps(3600), gemesPerHoraDIngressos, minimDeGemes, maximDeGemes);

        // Separat per poder-ho provar. Sense ritme (un país acabat d'obrir) no
        // se sap quant val el temps: no s'ofereix. Massa gemes (o un número
        // que no cap en un int) tampoc.
        public static int CalcularGemes(double doblers, double perHora, double gemesPerHora, int minim, int maxim)
        {
            if (!(doblers > 0)) return 0;
            if (!(perHora > 0) || double.IsInfinity(perHora)) return 0;
            double gemes = Math.Ceiling(doblers / perHora * gemesPerHora);
            if (double.IsNaN(gemes) || gemes > maxim) return 0;
            return Math.Max(minim, (int)gemes);
        }

        // ---------------------------------------------------------------
        // Calendari
        // ---------------------------------------------------------------

        public bool PotRecollirElCalendari =>
            cataleg != null && DiaDelCalendari != null && Calendari.PotRecollir(_dades.ultimDiaDelCalendari, Avui);

        public DiaDelCalendari DiaDelCalendari =>
            cataleg != null ? Calendari.DelDia(cataleg.mesInicial, cataleg.cicle, _dades.diesDelCalendariRecollits) : null;

        // (3, 30) = "Dia 3/30"; al mes inicial i després, al cicle.
        public (int dia, int de) NumeroDelCalendari => cataleg != null
            ? Calendari.Numero(cataleg.mesInicial.Count, cataleg.cicle.Count, _dades.diesDelCalendariRecollits)
            : (0, 0);

        public bool EsElMesInicial => cataleg != null && _dades.diesDelCalendariRecollits < cataleg.mesInicial.Count;

        public List<Recompensa> RecollirElCalendari()
        {
            var donades = new List<Recompensa>();
            if (!PotRecollirElCalendari) return donades;

            DiaDelCalendari dia = DiaDelCalendari;
            foreach (Recompensa r in dia.recompenses)
                if (r != null) { Donar(r); donades.Add(r); }

            _dades.diesDelCalendariRecollits++;
            _dades.ultimDiaDelCalendari = Avui;
            Desar();
            return donades;
        }

        // ---------------------------------------------------------------
        // Pase mensual
        // ---------------------------------------------------------------

        public bool PaseActiu => _dades.paseFiTicks > DateTime.Now.Date.Ticks;

        public int DiesDelPase => PaseActiu ? (int)(new DateTime(_dades.paseFiTicks) - DateTime.Now.Date).TotalDays : 0;

        public double GemesDelPasePerDia => _dades.paseGemesPerDia;

        public bool PotRecollirElPase => PaseActiu && Calendari.PotRecollir(_dades.ultimDiaDelPase, Avui);

        public int RecollirElPase()
        {
            if (!PotRecollirElPase || CurrencyManager.Instancia == null) return 0;
            int gemes = (int)Math.Round(_dades.paseGemesPerDia);
            CurrencyManager.Instancia.AfegirGemes(gemes);
            _dades.ultimDiaDelPase = Avui;
            Desar();
            EnCanvi?.Invoke();
            return gemes;
        }

        // ---------------------------------------------------------------
        // Avisos i restaurar
        // ---------------------------------------------------------------

        // Hi ha alguna cosa gratis per recollir: el punt vermell.
        public bool HiHaGratisPerRecollir
        {
            get
            {
                if (PotRecollirElCalendari || PotRecollirElPase) return true;
                if (cataleg == null) return false;
                foreach (ProducteDeBotiga p in cataleg.productes)
                    if (p != null && p.pagament == TipusDePagament.Gratis && Estat(p) == EstatDeProducte.Disponible)
                        return true;
                return false;
            }
        }

        // Torna a donar els permanents que el compte ja tenia comprats.
        public void Restaurar(Action<int> enAcabar = null)
        {
            Compres.Restaurar(ids =>
            {
                int restaurats = 0;
                if (ids != null && cataleg != null)
                    foreach (ProducteDeBotiga p in cataleg.productes)
                    {
                        if (p == null || !p.EsPermanent || Array.IndexOf(ids, p.idDeCompra) < 0) continue;
                        foreach (Recompensa r in p.recompenses)
                            if (r != null && r.tipus == TipusDeRecompensa.Permanent) Donar(r);
                        restaurats++;
                    }
                if (restaurats > 0) Desar();
                enAcabar?.Invoke(restaurats);
            });
        }

        // ---------------------------------------------------------------
        // Guardat
        // ---------------------------------------------------------------

        private static void Desar()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.SaveGameData();
        }

        public DadesDeBotiga Guardar()
        {
            _dades.compres = new List<CompraGuardada>(_compres.Values).ToArray();
            _dades.multiplicadors = _multiplicadors.Guardar();
            return _dades;
        }

        // segonsFora: el temps des que es va guardar; els multiplicadors hi corren.
        public void Carregar(DadesDeBotiga dades, double segonsFora)
        {
            _dades = dades ?? new DadesDeBotiga();
            _compres.Clear();
            if (_dades.compres != null)
                foreach (CompraGuardada c in _dades.compres)
                    if (c != null && !string.IsNullOrEmpty(c.id)) _compres[c.id] = c;
            _multiplicadors.Carregar(_dades.multiplicadors);
            if (segonsFora > 0) _multiplicadors.Avancar(segonsFora);
            EnCanvi?.Invoke();
        }
    }
}
