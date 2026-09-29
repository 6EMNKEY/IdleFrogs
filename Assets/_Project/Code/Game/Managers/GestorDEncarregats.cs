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

        // Com a Barrera: venciment en UTC i no segons restants, perquè si no
        // tancar el joc congelaria el cooldown.
        public System.DateTime fiDeLHabilitat;
        public System.DateTime fiDeRecarrega;

        // Perquè el tick sàpiga que ja ha recalculat després de caducar
        public bool caducitatAvisada = true;

        public bool HabilitatActiva => System.DateTime.UtcNow < fiDeLHabilitat;
        public bool HabilitatLlesta => System.DateTime.UtcNow >= fiDeRecarrega;

        public double SegonsFinsLlesta
        {
            get
            {
                double queden = (fiDeRecarrega - System.DateTime.UtcNow).TotalSeconds;
                return queden < 0 ? 0 : queden;
            }
        }
    }

    // Qui té quins encarregats i on estan posats. Els efectes i l'automatisme
    // encara no hi són: això només compra, guarda i assigna.
    public class GestorDEncarregats : MonoBehaviour
    {
        public static GestorDEncarregats Instancia { get; private set; }

        [SerializeField] private CatalegDEncarregats cataleg;

        private readonly List<EncarregatPosseit> _posseits = new List<EncarregatPosseit>();
        private int _comprats;
        private int _seguentId = 1;

        public IReadOnlyList<EncarregatPosseit> Posseits => _posseits;
        public int Comprats => _comprats;
        public double PreuDeLaSeguentCompra => cataleg != null ? cataleg.PreuDeLaCompra(_comprats) : 0;

        public event System.Action EnCanviDEncarregats;

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
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= ComprovarHabilitats;
            if (Instancia == this) Instancia = null;
        }

        // ---------------------------------------------------------------
        // Compra
        // ---------------------------------------------------------------

        public bool PucPagarLaCompra =>
            CurrencyManager.Instancia != null &&
            CurrencyManager.Instancia.doblersActuals >= PreuDeLaSeguentCompra;

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

            double preu = PreuDeLaSeguentCompra;
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
                // Neix llest: el cooldown comença quan s'activa l'habilitat.
                fiDeLHabilitat = System.DateTime.MinValue,
                fiDeRecarrega = System.DateTime.MinValue
            });

            _comprats++;
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

            RecalcularTot();
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
            unitat.AplicarBonificacions(CalcularBonificacions(e), e != null);
        }

        private static Bonificacions CalcularBonificacions(EncarregatPosseit e)
        {
            Bonificacions b = Bonificacions.Cap;
            if (e == null || e.definicio == null) return b;

            double magnitud = e.definicio.magnitud;
            if (e.HabilitatActiva) magnitud *= e.definicio.magnitudDeLHabilitat;

            switch (e.definicio.efecte)
            {
                case TipusDEfecte.VelocitatDeMoviment: b.velocitat = magnitud; break;
                case TipusDEfecte.VelocitatDeTreball: b.treball = magnitud; break;
                case TipusDEfecte.Capacitat: b.capacitat = magnitud; break;

                case TipusDEfecte.ReduccioDeCost:
                    // Topall al 95%: 0.8 × 8 de l'habilitat passaria de 1 i
                    // les millores sortirien gratis o a preu negatiu.
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
                    fiDeRecarregaUtc = e.fiDeRecarrega.ToBinary()
                };
            }
            return dades;
        }

        public void Carregar(EncarregatGuardat[] dades, int comprats)
        {
            _posseits.Clear();
            _comprats = comprats < 0 ? 0 : comprats;
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

            EnCanviDEncarregats?.Invoke();
        }
    }
}
