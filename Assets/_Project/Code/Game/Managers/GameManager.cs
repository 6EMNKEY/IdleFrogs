using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleFrogs.Game
{
    // Abans que la resta: en despertar-se llegeix la partida per saber en quin
    // país som, i l'aspecte del país ho necessita en el seu Awake.
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {

        public static GameManager Instancia { get; private set; }

        [Header("Interval de ticks")]
        [Tooltip("La frequencia de la logica del joc funciona amb segons (0.1 = 10 ticks / segon)")]
        [SerializeField] private float intervalDeTick = 0.1f;

        [Header("Referencies")]
        [SerializeField] private LlistaDePantans llistaDePantans;
        [SerializeField] private ControladorTren tren;
        [SerializeField] private Estacio estacio;
        [SerializeField] private Tenda tenda;

        [Header("Guardat")]
        [SerializeField] private float segonsEntreAutoGuardats = 30f;

        [Header("Zona")]
        [Tooltip("El país en què comença una partida nova. Després mana el guardat: el país on eres l'últim cop.")]
        [SerializeField] private int zona = 0;

        [Header("Partida nova")]
        [Tooltip("Just el que costa el primer pantà: l'únic que pots fer és comprar-lo.")]
        [SerializeField] private double doblersInicials = 10;

        // El tope: més enllà d'aquest temps fora no es compta res més.
        [Header("Progrés offline")]
        [Tooltip("672 h = 4 setmanes")]
        [SerializeField] private float horesMaximesOffline = 672f;
        [Tooltip("Per sota d'això no s'ensenya el panell: els doblers es donen directament")]
        [SerializeField] private float segonsMinimsPerAvisar = 60f;

        // Events per a que altres managers es suscriguin

        public event Action EnTickDeJoc;
        public event Action EnTickDeSegon;

        // La recompensa offline queda pendent fins que el jugador l'accepta al
        // panell, i es guarda amb la partida: si tanca l'app abans, no la perd.
        // El panell la mira al seu Start i, per si arriba més tard (tornar de
        // segon pla), també se l'avisa per l'event.
        public event Action<RecompensaOffline> EnProgresOffline;
        private RecompensaOffline _offlinePendent;

        public bool HiHaRecompensaOffline => _offlinePendent.HiHaRecompensa;
        public RecompensaOffline RecompensaOfflinePendent => _offlinePendent;

        // Quan és true, tot el que depèn de la partida carregada ja és a lloc.
        public bool PartidaCarregada { get; private set; }

        // La guia inicial viu a la UI, que no podem referenciar; el seu
        // progrés sí que el guardem nosaltres.
        public int PasDeLaGuia { get; set; }

        private float _temporitzadorTick;
        private float _temporitzadorSegon;
        private float _comptadorDAutoGuardat;

        private bool _reiniciant;

        // Tot el que hi ha guardat, amb totes les zones. Es manté en memòria
        // perquè en guardar només se substitueixi la zona d'aquesta escena.
        private DadesDePartida _dades;

        public int Zona => zona;
        private DateTime _marcaCarregada = DateTime.MinValue;
        private DateTime _momentDePausa;
        private bool _enPausa;


        private void Awake()
        {
            // Asegurar que GameManager sigui un singleton
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            DontDestroyOnLoad(gameObject);

            // El país on eres: tot el que en depèn (aspecte, moneda) el vol saber aviat.
            _dades = ServeiDeGuardat.Carregar();
            if (_dades != null)
                zona = Mathf.Clamp(_dades.zonaActual, 0, Mathf.Max(0, _dades.paisosDesbloquejats - 1));
        }

        // Start i no Awake: aquí ja s'han executat tots els Awake, o sigui
        // que CurrencyManager.Instancia existeix. I com que som nosaltres
        // els qui cridem InicialitzarPantans(), les granotes es reparteixen
        // sempre DESPRÉS d'haver carregat els nivells guardats.
        private void Start()
        {
            CarregarPartida();

            if (llistaDePantans != null)
                llistaDePantans.InicialitzarPantans();
            else
                Debug.LogError("GameManager: falta la referència a LlistaDePantans");

            if (tenda != null) tenda.InicialitzarTenda();
            else Debug.LogError("GameManager: falta la referència a Tenda");

            // Les unitats ja existeixen i els nivells ja s'han carregat, així
            // que aquí és on els bonus i l'automatisme arriben als trams.
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.RegistrarUnitats(llistaDePantans, tren, tenda);

            // L'últim: l'idle depèn dels nivells i dels bonus dels encarregats,
            // que acaba de posar RegistrarUnitats.
            if (_marcaCarregada != DateTime.MinValue)
                AplicarProgresOffline(_marcaCarregada);

            PartidaCarregada = true;

            // Guardem ja: si l'app es tanqués abans del primer autoguardat, la
            // partida guardada encara tindria la marca de temps vella, i el
            // proper cop es tornaria a comptar el mateix temps fora (i a cobrar
            // la càrrega de la granota transportista) per segona vegada.
            SaveGameData();
            Debug.Log("GameManager: Inicialització completa");
        }

        void Update()
        {
            CorrerBuclePrincipal();
        }
        private void CorrerBuclePrincipal()
        {
            // Tick de Joc
            _temporitzadorTick += Time.deltaTime;
            if (_temporitzadorTick >= intervalDeTick)
            {
                _temporitzadorTick -= intervalDeTick;
                EnTickDeJoc?.Invoke();
            }

            // Tic de Segon
            _temporitzadorSegon += Time.deltaTime;
            if (_temporitzadorSegon >= 1.0f)
            {
                _temporitzadorSegon -= 1.0f;
                EnTickDeSegon?.Invoke();
            }

            // Autoguardat
            _comptadorDAutoGuardat += Time.deltaTime;
            if (_comptadorDAutoGuardat >= segonsEntreAutoGuardats)
            {
                _comptadorDAutoGuardat = 0f;
                SaveGameData();
            }

        }

        #region Progrés offline

        // Al mòbil el joc no sempre es tanca: sovint només passa a segon pla
        // i en tornar continua on era. Les corrutines no han avançat (Unity
        // estava suspès), així que aquest temps també s'ha de donar com a
        // offline, igual que si l'haguessis tancat del tot.
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGameData();
                _momentDePausa = DateTime.UtcNow;
                _enPausa = true;
            }
            else if (_enPausa)
            {
                _enPausa = false;
                if (!PartidaCarregada) return;
                AplicarProgresOffline(_momentDePausa);
                SaveGameData();   // mateix motiu que al final de Start
            }
        }

        // La crida el panell en acceptar (o tancar). Torna el que s'ha cobrat.
        public double CobrarRecompensaOffline()
        {
            double doblers = _offlinePendent.doblers;
            _offlinePendent = default;
            if (doblers > 0 && CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(doblers);
            SaveGameData();
            return doblers;
        }

        // Les tres capacitats de la cadena: el que podrien fer treballant sense
        // parar. Els pantans només compten si són comprats i a l'abast del tren
        // (no darrere d'una barrera).
        //
        // Amb 'nomesAutomatitzat' compta només el que va sol: un pantà sense
        // encarregat no suma, i un tren o un restaurant sense encarregat valen 0.
        public MesuresDeLaCadena Mesures(bool nomesAutomatitzat = false)
        {
            double pantans = 0;
            if (llistaDePantans != null)
            {
                int maxim = Mathf.Min(llistaDePantans.IndexMaximAccessible(), llistaDePantans.Quantitat - 1);
                for (int i = 0; i <= maxim; i++)
                {
                    Panta p = llistaDePantans.Obtenir(i);
                    if (p == null || !p.EstaComprat) continue;
                    if (nomesAutomatitzat && !p.EstaAutomatitzada) continue;
                    pantans += p.ProduccioReal;
                }
            }

            double cabalTren = tren != null && (!nomesAutomatitzat || tren.EstaAutomatitzada) ? tren.CabalReal : 0;
            double cabalTenda = tenda != null && (!nomesAutomatitzat || tenda.EstaAutomatitzada) ? tenda.CabalReal : 0;
            return new MesuresDeLaCadena(pantans, cabalTren, cabalTenda);
        }

        // Només la producció automàtica: el coll d'ampolla del que va sol, entre deu.
        public double IngresIdlePerSegon() => Mesures(nomesAutomatitzat: true).Idle;

        // Idle × temps fora, amb el tope d'hores. No es dona: queda pendent
        // fins que el jugador l'accepta al panell. Una absència curta es dona
        // directament, sense panell.
        // Per provar: com si haguessis tancat el joc fa 'segons' segons.
        public void SimularTempsFora(double segons)
        {
            if (segons <= 0) return;
            AplicarProgresOffline(DateTime.UtcNow.AddSeconds(-segons));
            SaveGameData();
        }

        private void AplicarProgresOffline(DateTime desDe)
        {
            double segons = (DateTime.UtcNow - desDe).TotalSeconds;
            if (segons <= 1) return;

            RecompensaOffline r = RecompensaOffline.Calcular(segons, horesMaximesOffline * 3600.0, IngresIdlePerSegon());
            if (!r.HiHaRecompensa) return;

            Debug.Log($"GameManager: offline {FormatadorDeNombres.Temps(r.segonsComptats)} × " +
                      $"{FormatadorDeNombres.Ritme(r.PerSegon)} = {FormatadorDeNombres.Doblers(r.doblers)}");

            if (segons < segonsMinimsPerAvisar && !_offlinePendent.HiHaRecompensa)
            {
                if (CurrencyManager.Instancia != null) CurrencyManager.Instancia.AfegirDoblers(r.doblers);
                return;
            }

            _offlinePendent = _offlinePendent.Sumar(r);
            EnProgresOffline?.Invoke(_offlinePendent);
        }

        #endregion

        #region Application Lifecycle & Saving

        private void OnApplicationQuit()
        {
            SaveGameData();
        }

        private void CarregarPartida()
        {
            if (_dades == null)
            {
                _dades = new DadesDePartida();
                IniciarPartidaNova();
                IniciarZonaNova();
                return;
            }

            MigrarAPaisos();

            // Globals
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EstablirGemes(_dades.gemes);
            PasDeLaGuia = _dades.pasDeLaGuia;

            // La zona d'aquesta escena. Si encara no s'hi ha jugat mai, comença
            // de zero encara que la partida en tingui d'altres.
            if (_dades.TeZona(zona))
            {
                ZonaGuardada z = _dades.zones[zona];
                AplicarZona(z);

                // Els doblers, el que quedava per cobrar i el rellotge són del país.
                if (CurrencyManager.Instancia != null)
                    CurrencyManager.Instancia.EstablirDoblers(z.doblers);
                _offlinePendent = z.offlinePendent;

                // Només hi ha offline a donar si la zona ja existia: una zona
                // nova no té encarregats que hagin treballat. Compta tant el
                // temps amb el joc tancat com el que has estat en un altre país.
                if (z.marcaDeTempsUtc != 0)
                    _marcaCarregada = DateTime.FromBinary(z.marcaDeTempsUtc);
            }
            else
            {
                IniciarZonaNova();
            }

            Debug.Log($"GameManager: partida carregada (zona {zona})");
        }

        private void AplicarZona(ZonaGuardada z)
        {
            if (llistaDePantans != null)
            {
                // Les barreres primer: decideixen quins pantans es poden
                // comprar, i ActualitzarEstats() ho llegeix després.
                llistaDePantans.AplicarBarreres(z.barreres);
                llistaDePantans.AplicarNivells(z.nivellsDePanta);
                llistaDePantans.AplicarCapses(z.capsesDePanta);
            }

            if (tren != null)
            {
                tren.AplicarNivell(z.nivellDeTren);
                tren.AplicarCarrega(z.carregaDeTren);
            }

            if (estacio != null) estacio.AplicarDoblers(z.doblersALEstacio);

            // Abans que res que en depengui: les assignacions decideixen
            // quins trams van sols.
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.Carregar(z.encarregats, z.encarregatsCompratsPerTipus);

            if (tenda != null) tenda.AplicarNivell(z.nivellDeTenda);

            // El que la granota duia a mitja entrega es cobra directament:
            // anava cap a la cartera igualment i així no es perd res ni cal
            // recrear-li l'estat de la corrutina.
            if (z.carregaDelTransportista > 0 && CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(z.carregaDelTransportista);
        }

        // Globals d'una partida que comença de zero.
        private void IniciarPartidaNova()
        {
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EstablirGemes(0);
            _dades.paisosDesbloquejats = 1;
            PasDeLaGuia = 0;
            Debug.Log("GameManager: partida nova");
        }

        // Una zona nova no pot dependre de com estigui el prefab: si el pantà
        // té nivell 1 a l'inspector, naixerien tots comprats. Ho posem a zero
        // aquí i el prefab deixa d'importar.
        private void IniciarZonaNova()
        {
            // Cada país comença amb la seva moneda a zero, menys el que costa
            // el primer pantà.
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EstablirDoblers(doblersInicials);
            _offlinePendent = default;

            if (llistaDePantans != null)
                llistaDePantans.AplicarNivells(new int[llistaDePantans.Quantitat]);
        }

        public void SaveGameData()
        {
            // Mentre es reinicia no s'ha de tornar a escriure la partida vella.
            if (_reiniciant || llistaDePantans == null) return;
            if (_dades == null) _dades = new DadesDePartida();

            // Es reescriu només la zona d'aquesta escena: les altres es
            // queden tal com es van carregar.
            _dades.versio = DadesDePartida.VersioActual;
            _dades.marcaDeTempsUtc = DateTime.UtcNow.ToBinary();
            _dades.gemes = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.gemesActuals : 0;
            _dades.pasDeLaGuia = PasDeLaGuia;
            _dades.zonaActual = zona;

            ZonaGuardada z = _dades.Zona(zona);
            z.doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;
            z.marcaDeTempsUtc = _dades.marcaDeTempsUtc;
            z.offlinePendent = _offlinePendent;
            z.nivellsDePanta = llistaDePantans.ObtenirNivells();
            z.capsesDePanta = llistaDePantans.ObtenirCapses();
            z.barreres = llistaDePantans.ObtenirBarreres();
            z.nivellDeTren = tren != null ? tren.Nivell : 1;
            z.carregaDeTren = tren != null ? tren.CarregaActual : 0;
            z.doblersALEstacio = estacio != null ? estacio.DoblersEmmagatzemats : 0;
            z.nivellDeTenda = tenda != null ? tenda.Nivell : 1;
            // Ja no hi ha transportista: el camp queda per a les partides d'abans,
            // que en carregar-se encara cobren el que portava.
            z.carregaDelTransportista = 0;
            z.encarregats = GestorDEncarregats.Instancia != null ? GestorDEncarregats.Instancia.Guardar() : null;
            z.encarregatsCompratsPerTipus = GestorDEncarregats.Instancia != null
                ? GestorDEncarregats.Instancia.GuardarComprats() : null;

            ServeiDeGuardat.Guardar(_dades);
        }

        #endregion

        #region Països

        public int PaisosDesbloquejats => _dades != null ? Mathf.Max(1, _dades.paisosDesbloquejats) : 1;

        public void DesbloquejarSeguentPais()
        {
            if (_dades == null) return;
            _dades.paisosDesbloquejats = PaisosDesbloquejats + 1;
            SaveGameData();
        }

        // Guarda aquest país i torna a carregar l'escena amb l'altre. En
        // tornar-hi més tard, el temps que hi has faltat es cobra com a offline.
        public void CanviarDeZona(int n)
        {
            if (_reiniciant || n == zona || n < 0 || n >= PaisosDesbloquejats) return;
            SaveGameData();
            _dades.zonaActual = n;
            ServeiDeGuardat.Guardar(_dades);
            _reiniciant = true;
            RecarregarLEscena();
        }

        private void MigrarAPaisos() => MigracioDePartida.PassarAPaisos(_dades);

        #endregion

        #region Reiniciar

        // Esborra la partida i torna a carregar l'escena com si fos el primer cop.
        public void ReiniciarPartida()
        {
            if (_reiniciant) return;
            _reiniciant = true;

            ServeiDeGuardat.Esborrar();
            RecarregarLEscena();
        }

        private void RecarregarLEscena()
        {
            // Som DontDestroyOnLoad: si no ens destruïm, sobreviuríem a la
            // recàrrega amb referències als pantans de l'escena vella, ja
            // destruïts, i la nova escena esborraria el seu GameManager en
            // veure que ja n'hi ha un. Ens apartem perquè el nou ocupi el lloc.
            Instancia = null;
            Destroy(gameObject);

            Scene escena = SceneManager.GetActiveScene();
            if (escena.buildIndex >= 0)
            {
                SceneManager.LoadScene(escena.buildIndex);
                return;
            }

#if UNITY_EDITOR
            // L'escena no és a la llista d'escenes del build. A l'editor la
            // podem carregar igualment; en una build no.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                escena.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError("GameManager: l'escena no és a la llista d'escenes del build i no es pot recarregar");
#endif
        }

        #endregion
    }
}
