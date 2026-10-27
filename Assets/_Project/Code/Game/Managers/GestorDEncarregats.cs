using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Un encarregat que el jugador ja té. La definició és compartida (l'asset);
    // això és la còpia concreta, amb la seva ranura i els seus temporitzadors.
    public class EncarregatPosseit
    {
        public EncarregatSO definicio;
        public int id;

        public bool assignat;
        public Ranura ranura;

        // El que va costar. Vendre'l en torna la meitat.
        public double preuPagat;

        // Com a Barrera: venciment en UTC i no segons restants, perquè si no
        // tancar el joc congelaria el cooldown.
        public System.DateTime fiDeLHabilitat;
        public System.DateTime fiDeRecarrega;

        // Perquè el tick sàpiga que ja ha recalculat després de caducar
        public bool caducitatAvisada = true;

        public bool HabilitatActiva => System.DateTime.UtcNow < fiDeLHabilitat;
        public bool HabilitatLlesta => System.DateTime.UtcNow >= fiDeRecarrega;

        public double SegonsDHabilitatRestants
        {
            get
            {
                double queden = (fiDeLHabilitat - System.DateTime.UtcNow).TotalSeconds;
                return queden < 0 ? 0 : queden;
            }
        }

        public double SegonsFinsLlesta
        {
            get
            {
                double queden = (fiDeRecarrega - System.DateTime.UtcNow).TotalSeconds;
                return queden < 0 ? 0 : queden;
            }
        }

        // Encesa mana: si la recàrrega fos més curta que la durada, mentre
        // dura segueix sent "encesa".
        public EstatDHabilitat Estat =>
            HabilitatActiva ? EstatDHabilitat.Activa
            : HabilitatLlesta ? EstatDHabilitat.Llesta
            : EstatDHabilitat.Recarregant;

        // El temps que s'ensenya: la durada si està llesta, el que li queda si
        // està encesa i el que falta per tornar-la a fer servir si recarrega.
        public string TempsDeLHabilitat
        {
            get
            {
                if (definicio == null) return string.Empty;
                switch (Estat)
                {
                    case EstatDHabilitat.Activa: return FormatadorDeNombres.TempsCurt(SegonsDHabilitatRestants);
                    case EstatDHabilitat.Recarregant: return FormatadorDeNombres.TempsCurt(SegonsFinsLlesta);
                    default: return definicio.DuracioCurta;
                }
            }
        }
    }

    public enum EstatDHabilitat { Llesta, Activa, Recarregant }

    // Qui té quins encarregats i on estan posats. Els efectes i l'automatisme
    // encara no hi són: això només compra, guarda i assigna.
    public class GestorDEncarregats : MonoBehaviour
    {
        public static GestorDEncarregats Instancia { get; private set; }

        [SerializeField] private CatalegDEncarregats cataleg;

        private readonly List<EncarregatPosseit> _posseits = new List<EncarregatPosseit>();
        // Un comptador per tipus de lloc: el preu puja per a cada tipus pel seu
        // compte. Comprar quatre encarregats de tenda no encareix els de pantà,
        // que tornen a començar a 100.
        private static readonly int QuantitatDeTipus = System.Enum.GetValues(typeof(TipusDeLloc)).Length;
        private readonly int[] _compratsPerTipus = new int[QuantitatDeTipus];
        private int _seguentId = 1;

        public IReadOnlyList<EncarregatPosseit> Posseits => _posseits;

        public int Comprats(TipusDeLloc lloc) => _compratsPerTipus[(int)lloc];

        // Les dues imatges que toca ensenyar: la seva, la del seu tipus o la de
        // per defecte. El retrat és el del panell; el sprite al món, la silueta.
        // Amb els canvis de sprites del país al damunt.
        public Sprite RetratDe(EncarregatSO e) =>
            SpritesDelPais.Substituir(cataleg != null ? cataleg.RetratDe(e) : (e != null ? e.retrat : null));
        public Sprite SpriteAlMonDe(EncarregatSO e) =>
            SpritesDelPais.Substituir(cataleg != null ? cataleg.SpriteAlMonDe(e) : (e != null ? e.spriteAlMon : null));

        public double PreuDeLaSeguentCompra(TipusDeLloc lloc) =>
            cataleg != null ? cataleg.PreuDeLaCompra(Comprats(lloc)) : 0;

        public event System.Action EnCanviDEncarregats;
        public event System.Action EnDesbloquejar;

        // Es calcula en viu a partir del nivell del primer pantà, no es guarda:
        // si un dia el prestigi reinicia els pantans, es tornen a bloquejar sols.
        private bool _desbloquejats;
        private Panta _primerPanta;

        public bool EstanDesbloquejats => _desbloquejats;
        public int NivellDeDesbloqueig =>
            cataleg != null ? cataleg.nivellDelPrimerPantaPerDesbloquejar : 5;

        public bool HiHaAlgunAssignat
        {
            get
            {
                foreach (EncarregatPosseit e in _posseits)
                    if (e.assignat) return true;
                return false;
            }
        }

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
            if (_primerPanta != null) _primerPanta.EnCanviDeNivell -= ComprovarDesbloqueig;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= ComprovarHabilitats;
            if (Instancia == this) Instancia = null;
        }

        // ---------------------------------------------------------------
        // Compra
        // ---------------------------------------------------------------

        public bool PucPagarLaCompra(TipusDeLloc lloc) =>
            CurrencyManager.Instancia != null &&
            CurrencyManager.Instancia.doblersActuals >= PreuDeLaSeguentCompra(lloc);

        // El tipus de lloc el decideix la silueta des d'on es compra; només
        // la raresa és aleatòria. Torna el que ha sortit, o null si no s'ha
        // pogut comprar, perquè la UI pugui ensenyar "has rebut X".
        public EncarregatSO Comprar(TipusDeLloc lloc)
        {
            if (cataleg == null)
            {
                Debug.LogError("GestorDEncarregats: falta el catàleg", this);
                return null;
            }
            if (CurrencyManager.Instancia == null) return null;
            if (!_desbloquejats) return null;

            double preu = PreuDeLaSeguentCompra(lloc);
            EncarregatSO triat = cataleg.Sortejar(lloc);
            if (triat == null)
            {
                Debug.LogError($"GestorDEncarregats: el catàleg no té cap encarregat de {lloc}", this);
                return null;
            }

            // Es cobra només un cop sabem que hi ha què donar.
            if (!CurrencyManager.Instancia.GastarDoblers(preu)) return null;

            _posseits.Add(new EncarregatPosseit
            {
                definicio = triat,
                id = _seguentId++,
                assignat = false,
                preuPagat = preu,
                // Neix llest: el cooldown comença quan s'activa l'habilitat.
                fiDeLHabilitat = System.DateTime.MinValue,
                fiDeRecarrega = System.DateTime.MinValue
            });

            _compratsPerTipus[(int)lloc]++;
            EnCanviDEncarregats?.Invoke();
            return triat;
        }

        // Un regal de la botiga: no es paga ni encareix els que es compren
        // amb monedes. preuPagat gairebé zero i no zero: amb 0 el guardat el
        // reconstruiria com si s'hagués comprat, i vendre'l donaria diners.
        public EncarregatSO Regalar(Raresa raresa)
        {
            if (cataleg == null) return null;
            EncarregatSO triat = cataleg.TriarDeRaresa(raresa);
            if (triat == null)
            {
                Debug.LogWarning($"GestorDEncarregats: el catàleg no té cap encarregat {raresa}", this);
                return null;
            }

            _posseits.Add(new EncarregatPosseit
            {
                definicio = triat,
                id = _seguentId++,
                assignat = false,
                preuPagat = 1e-6,
                fiDeLHabilitat = System.DateTime.MinValue,
                fiDeRecarrega = System.DateTime.MinValue
            });
            EnCanviDEncarregats?.Invoke();
            return triat;
        }

        // ---------------------------------------------------------------
        // Assignació
        // ---------------------------------------------------------------

        public EncarregatPosseit ALaRanura(Ranura ranura)
        {
            foreach (EncarregatPosseit e in _posseits)
                if (e.assignat && e.ranura.Equals(ranura)) return e;
            return null;
        }

        public bool RanuraOcupada(Ranura ranura) => ALaRanura(ranura) != null;

        public void PerALloc(TipusDeLloc lloc, List<EncarregatPosseit> sortida)
        {
            sortida.Clear();
            foreach (EncarregatPosseit e in _posseits)
                if (e.definicio != null && e.definicio.lloc == lloc) sortida.Add(e);
        }

        // Els d'aquell tipus que no treballen enlloc: la llista del panell.
        public void NoAssignats(TipusDeLloc lloc, List<EncarregatPosseit> sortida)
        {
            sortida.Clear();
            foreach (EncarregatPosseit e in _posseits)
                if (!e.assignat && e.definicio != null && e.definicio.lloc == lloc) sortida.Add(e);
        }

        public bool Assignar(EncarregatPosseit encarregat, Ranura ranura)
        {
            if (encarregat == null || encarregat.definicio == null) return false;
            if (encarregat.definicio.lloc != ranura.tipus) return false;

            // Una ranura només aguanta un encarregat: el que hi hagués surt.
            EncarregatPosseit anterior = ALaRanura(ranura);
            if (anterior == encarregat) return false;
            if (anterior != null) anterior.assignat = false;

            Ranura anteriorRanura = encarregat.assignat ? encarregat.ranura : ranura;

            encarregat.assignat = true;
            encarregat.ranura = ranura;

            // La ranura d'on venia també canvia: es queda sense encarregat.
            Recalcular(anteriorRanura);
            Recalcular(ranura);
            EnCanviDEncarregats?.Invoke();
            return true;
        }

        public bool Desassignar(EncarregatPosseit encarregat)
        {
            if (encarregat == null || !encarregat.assignat) return false;

            Ranura ranura = encarregat.ranura;
            encarregat.assignat = false;
            Recalcular(ranura);
            EnCanviDEncarregats?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------
        // Venda
        // ---------------------------------------------------------------

        public static double PreuDeVenda(EncarregatPosseit encarregat) =>
            encarregat != null ? encarregat.preuPagat / 2 : 0;

        // El comptador del tipus no baixa: si baixés, comprar i vendre
        // abaratiria la tirada següent. Si estava treballant, la ranura
        // es queda buida.
        public bool Vendre(EncarregatPosseit encarregat)
        {
            if (encarregat == null || CurrencyManager.Instancia == null) return false;
            if (!_posseits.Remove(encarregat)) return false;

            if (encarregat.assignat)
            {
                encarregat.assignat = false;
                Recalcular(encarregat.ranura);
            }

            CurrencyManager.Instancia.AfegirDoblers(PreuDeVenda(encarregat));
            EnCanviDEncarregats?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------
        // Efectes — el gestor calcula i empeny; les unitats no el consulten
        // cada frame, es guarden el resultat.
        // ---------------------------------------------------------------

        private readonly Dictionary<Ranura, UnitatMillorable> _unitats =
            new Dictionary<Ranura, UnitatMillorable>();

        // La crida GameManager després de carregar, perquè l'ordre de Start
        // entre objectes no està garantit i això ha de passar abans que res.
        public void RegistrarUnitats(LlistaDePantans pantans, ControladorTren tren, Tenda tenda)
        {
            _unitats.Clear();

            if (pantans != null)
                for (int i = 0; i < pantans.Quantitat; i++)
                {
                    Panta p = pantans.Obtenir(i);
                    if (p != null) _unitats[p.Ranura] = p;
                }

            if (tren != null) _unitats[tren.Ranura] = tren;
            if (tenda != null) _unitats[tenda.Ranura] = tenda;

            // El desbloqueig depèn del nivell del primer pantà: l'escoltem.
            if (_primerPanta != null) _primerPanta.EnCanviDeNivell -= ComprovarDesbloqueig;
            _primerPanta = pantans != null && pantans.Quantitat > 0 ? pantans.Obtenir(0) : null;
            if (_primerPanta != null) _primerPanta.EnCanviDeNivell += ComprovarDesbloqueig;
            ComprovarDesbloqueig();

            RecalcularTot();
        }

        private void ComprovarDesbloqueig()
        {
            // Si ja en tens algun (una partida d'abans d'aquest canvi), no te'ls
            // amaguem: ja els havies aconseguit.
            bool ara = _posseits.Count > 0 ||
                       (_primerPanta != null && _primerPanta.Nivell >= NivellDeDesbloqueig);

            if (ara == _desbloquejats) return;
            _desbloquejats = ara;

            if (ara) EnDesbloquejar?.Invoke();
            EnCanviDEncarregats?.Invoke();
        }

        public void RecalcularTot()
        {
            foreach (KeyValuePair<Ranura, UnitatMillorable> parella in _unitats)
                Recalcular(parella.Key);
        }

        private void Recalcular(Ranura ranura)
        {
            if (!_unitats.TryGetValue(ranura, out UnitatMillorable unitat) || unitat == null) return;

            EncarregatPosseit e = ALaRanura(ranura);
            unitat.AplicarBonificacions(CalcularBonificacions(e, true), CalcularBonificacions(e, false), e != null);
        }

        // ambHabilitat = false: només la passiva, encara que l'habilitat estigui encesa.
        private static Bonificacions CalcularBonificacions(EncarregatPosseit e, bool ambHabilitat)
        {
            Bonificacions b = Bonificacions.Cap;
            if (e == null || e.definicio == null) return b;

            double magnitud = e.definicio.magnitud;
            if (ambHabilitat && e.HabilitatActiva)
                magnitud = e.definicio.EsReduccioDeCost ? e.definicio.RebaixaDeLHabilitat
                                                       : magnitud * e.definicio.magnitudDeLHabilitat;

            switch (e.definicio.efecte)
            {
                case TipusDEfecte.VelocitatDeMoviment: b.velocitat = magnitud; break;
                case TipusDEfecte.VelocitatDeTreball: b.treball = magnitud; break;
                case TipusDEfecte.Capacitat: b.capacitat = magnitud; break;

                case TipusDEfecte.ReduccioDeCost:
                    // Topall al 95%: a 1 o més les millores sortirien
                    // gratis o a preu negatiu.
                    b.reduccioDeCost = System.Math.Min(0.95, magnitud);
                    break;
            }
            return b;
        }

        // ---------------------------------------------------------------
        // Habilitat activa
        // ---------------------------------------------------------------

        private void Start()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += ComprovarHabilitats;
        }

        private void ComprovarHabilitats()
        {
            bool algunaHaCaducat = false;
            foreach (EncarregatPosseit e in _posseits)
            {
                if (!e.assignat || e.definicio == null) continue;
                if (e.caducitatAvisada || e.HabilitatActiva) continue;
                if (e.fiDeLHabilitat == System.DateTime.MinValue) continue;

                e.caducitatAvisada = true;
                Recalcular(e.ranura);
                algunaHaCaducat = true;
            }
            if (algunaHaCaducat) EnCanviDEncarregats?.Invoke();
        }

        public bool ActivarHabilitat(EncarregatPosseit e)
        {
            if (e == null || e.definicio == null) return false;
            if (!e.assignat || !e.HabilitatLlesta) return false;

            System.DateTime ara = System.DateTime.UtcNow;
            e.fiDeLHabilitat = ara.AddSeconds(e.definicio.segonsDeDuracio);
            // La recàrrega compta des de l'activació, no des que s'apaga:
            // és el que fa que 1 min de durada amb 5 de recàrrega sigui un
            // 20% d'ús i no un 1/6.
            e.fiDeRecarrega = ara.AddSeconds(e.definicio.segonsDeRecarrega);
            e.caducitatAvisada = false;

            Recalcular(e.ranura);
            EnCanviDEncarregats?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------
        // Guardat
        // ---------------------------------------------------------------

        public EncarregatGuardat[] Guardar()
        {
            var dades = new EncarregatGuardat[_posseits.Count];
            for (int i = 0; i < _posseits.Count; i++)
            {
                EncarregatPosseit e = _posseits[i];
                dades[i] = new EncarregatGuardat
                {
                    idDefinicio = e.definicio != null ? e.definicio.id : string.Empty,
                    idInstancia = e.id,
                    assignat = e.assignat,
                    llocTipus = (int)e.ranura.tipus,
                    llocIndex = e.ranura.index,
                    fiDeLHabilitatUtc = e.fiDeLHabilitat.ToBinary(),
                    fiDeRecarregaUtc = e.fiDeRecarrega.ToBinary(),
                    preuPagat = e.preuPagat
                };
            }
            return dades;
        }

        // Les partides d'abans de la venda no guardaven el preu. Es dedueix
        // de l'ordre de compra: l'id d'instància puja amb cada compra, així
        // que el k-è de cada tipus per id va costar PreuDeLaCompra(k).
        private void ReconstruirPreusPagats()
        {
            if (cataleg == null) return;

            foreach (EncarregatPosseit e in _posseits)
            {
                if (e.preuPagat > 0 || e.definicio == null) continue;

                int abans = 0;
                foreach (EncarregatPosseit altre in _posseits)
                    if (altre.definicio != null && altre.definicio.lloc == e.definicio.lloc && altre.id < e.id)
                        abans++;
                e.preuPagat = cataleg.PreuDeLaCompra(abans);
            }
        }

        public int[] GuardarComprats() => (int[])_compratsPerTipus.Clone();

        // compratsPerTipus és null en les partides d'abans d'aquest canvi, que
        // només tenien un comptador global. Llavors es reconstrueix comptant
        // els encarregats que ja tens de cada tipus: aquelles partides són
        // d'abans de poder vendre, així que és exactament el que has comprat.
        public void Carregar(EncarregatGuardat[] dades, int[] compratsPerTipus)
        {
            _posseits.Clear();
            System.Array.Clear(_compratsPerTipus, 0, _compratsPerTipus.Length);
            _seguentId = 1;

            if (dades != null && cataleg != null)
            {
                foreach (EncarregatGuardat d in dades)
                {
                    if (d == null) continue;

                    EncarregatSO definicio = cataleg.PerId(d.idDefinicio);
                    if (definicio == null)
                    {
                        // L'asset ja no existeix o li han canviat l'id.
                        Debug.LogWarning($"GestorDEncarregats: no trobo l'encarregat '{d.idDefinicio}', el descarto", this);
                        continue;
                    }

                    _posseits.Add(new EncarregatPosseit
                    {
                        definicio = definicio,
                        id = d.idInstancia,
                        assignat = d.assignat,
                        ranura = new Ranura((TipusDeLloc)d.llocTipus, d.llocIndex),
                        preuPagat = d.preuPagat,
                        fiDeLHabilitat = System.DateTime.FromBinary(d.fiDeLHabilitatUtc),
                        fiDeRecarrega = System.DateTime.FromBinary(d.fiDeRecarregaUtc),

                        // Si es carrega una habilitat encara encesa, cal deixar
                        // pendent l'avís de caducitat. Amb el valor per defecte
                        // (true) el tick se la saltaria i la bonificació es
                        // quedaria posada per sempre.
                        caducitatAvisada =
                            System.DateTime.UtcNow >= System.DateTime.FromBinary(d.fiDeLHabilitatUtc)
                    });

                    if (d.idInstancia >= _seguentId) _seguentId = d.idInstancia + 1;
                }
            }

            ReconstruirPreusPagats();

            if (compratsPerTipus != null)
            {
                int n = System.Math.Min(compratsPerTipus.Length, _compratsPerTipus.Length);
                for (int i = 0; i < n; i++) _compratsPerTipus[i] = System.Math.Max(0, compratsPerTipus[i]);
            }
            else
            {
                foreach (EncarregatPosseit e in _posseits)
                    if (e.definicio != null) _compratsPerTipus[(int)e.definicio.lloc]++;
            }

            EnCanviDEncarregats?.Invoke();
        }
    }
}
