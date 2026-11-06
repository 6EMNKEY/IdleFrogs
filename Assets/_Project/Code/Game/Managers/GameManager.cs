using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleFrogs.Game
{
    // La partida sencera: llegeix i desa el fitxer, porta els globals (gemes,
    // guia, botiga, països oberts) i sap en quin país som. Viu fora de les
    // escenes (Resources/Sistemes, l'instancia Arrencada) i sobreviu als canvis
    // d'escena: país → mapa → país.
    //
    // El que és de cada país (pantans, tren, estació, restaurant) viu a la
    // seva escena i ens ho dona EscenaDelPais en arrencar. Sense país carregat
    // (al mapa) la partida continua: els països produeixen com a offline.
    [DefaultExecutionOrder(-200)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instancia { get; private set; }

        [Header("Interval de ticks")]
        [Tooltip("La frequencia de la logica del joc funciona amb segons (0.1 = 10 ticks / segon)")]
        [SerializeField] private float intervalDeTick = 0.1f;

        [Header("Escenes")]
        [Tooltip("La dels pantans: la mateixa per a tots els països")]
        [SerializeField] private string escenaDelPais = "MainScene";
        [SerializeField] private string escenaDelMapa = "Mapa";

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

        public event Action EnTickDeJoc;
        public event Action EnTickDeSegon;

        // La recompensa offline queda pendent fins que el jugador l'accepta al
        // panell, i es guarda amb la partida: si tanca l'app abans, no la perd.
        public event Action<RecompensaOffline> EnProgresOffline;
        private RecompensaOffline _offlinePendent;

        public bool HiHaRecompensaOffline => _offlinePendent.HiHaRecompensa;
        public RecompensaOffline RecompensaOfflinePendent => _offlinePendent;

        // El país de l'escena està carregat i en marxa (al mapa és false).
        public bool PartidaCarregada { get; private set; }
        public bool HiHaPais => _pais != null;

        // La guia inicial viu a la UI, que no podem referenciar; el seu
        // progrés sí que el guardem nosaltres.
        public int PasDeLaGuia { get; set; }
        // Quants passos té la guia (el posa GuiaInicial). Sense guia a
        // l'escena és 0, i llavors es dona per acabada.
        public int PassosDeLaGuia { get; set; }
        public bool GuiaAcabada => PasDeLaGuia >= PassosDeLaGuia;

        // El segon pantà no es ven fins que s'acaba la guia: ara ja es pot.
        public void AvisarQueLaGuiaSHaAcabat()
        {
            if (Llista != null) Llista.ActualitzarEstats();
        }

        private float _temporitzadorTick;
        private float _temporitzadorSegon;
        private float _comptadorDAutoGuardat;

        // Tot el que hi ha guardat, amb totes les zones. Es manté en memòria
        // perquè en guardar només se substitueixi la zona d'aquesta escena.
        private DadesDePartida _dades;
        private bool _globalsCarregats;
        // De quina zona són els doblers que té ara el CurrencyManager (-1 = de cap).
        private int _zonaDeLaMoneda = -1;

        // L'escena del país, quan n'hi ha.
        private EscenaDelPais _pais;
        private LlistaDePantans Llista => _pais != null ? _pais.Llista : null;
        private ControladorTren Tren => _pais != null ? _pais.Tren : null;
        private Estacio Estacio => _pais != null ? _pais.Estacio : null;
        private Tenda Tenda => _pais != null ? _pais.Tenda : null;

        public int Zona => zona;
        private DateTime _marcaCarregada = DateTime.MinValue;
        private DateTime _momentDePausa;
        private bool _enPausa;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            // Si som l'arrel (Sistemes ho és), sobrevivim als canvis d'escena.
            if (transform.parent == null) DontDestroyOnLoad(gameObject);

            // El país on eres: tot el que en depèn (aspecte, moneda) el vol saber aviat.
            _dades = ServeiDeGuardat.Carregar();
            if (_dades != null)
                zona = Mathf.Clamp(_dades.zonaActual, 0, Mathf.Max(0, _dades.paisosDesbloquejats - 1));
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // Start i no Awake: aquí ja s'han executat tots els Awake (CurrencyManager,
        // GestorDeBotiga...). Si el país arrenca abans, ell mateix ho demana.
        private void Start() => CarregarGlobals();

        void Update()
        {
            _temporitzadorTick += Time.deltaTime;
            if (_temporitzadorTick >= intervalDeTick)
            {
                _temporitzadorTick -= intervalDeTick;
                EnTickDeJoc?.Invoke();
            }

            _temporitzadorSegon += Time.deltaTime;
            if (_temporitzadorSegon >= 1.0f)
            {
                _temporitzadorSegon -= 1.0f;
                EnTickDeSegon?.Invoke();
            }

            _comptadorDAutoGuardat += Time.deltaTime;
            if (_comptadorDAutoGuardat >= segonsEntreAutoGuardats)
            {
                _comptadorDAutoGuardat = 0f;
                SaveGameData();
            }
        }

        // ---------------------------------------------------------------
        // El país de l'escena
        // ---------------------------------------------------------------

        // La crida EscenaDelPais al seu Start. Com que som nosaltres els qui
        // cridem InicialitzarPantans(), les granotes es reparteixen sempre
        // DESPRÉS d'haver carregat els nivells guardats.
        public void EntrarAlPais(EscenaDelPais pais)
        {
            _pais = pais;
            CarregarGlobals();
            CarregarZona();

            if (Llista != null) Llista.InicialitzarPantans();
            else Debug.LogError("GameManager: EscenaDelPais no té LlistaDePantans");

            if (Tenda != null) Tenda.InicialitzarTenda();
            else Debug.LogError("GameManager: EscenaDelPais no té Tenda");

            // Les unitats ja existeixen i els nivells ja s'han carregat, així
            // que aquí és on els bonus i l'automatisme arriben als trams.
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.RegistrarUnitats(Llista, Tren, Tenda);

            // L'últim: l'idle depèn dels nivells i dels bonus dels encarregats.
            if (_marcaCarregada != DateTime.MinValue)
                AplicarProgresOffline(_marcaCarregada);

            PartidaCarregada = true;

            // Guardem ja: si l'app es tanqués abans del primer autoguardat, la
            // partida guardada encara tindria la marca de temps vella, i el
            // proper cop es tornaria a comptar el mateix temps fora.
            SaveGameData();
            Debug.Log($"GameManager: país carregat (zona {zona})");
        }

        // L'escena del país es descarrega: ja no hi ha unitats a qui preguntar.
        public void SortirDelPais(EscenaDelPais pais)
        {
            if (_pais != pais) return;
            _pais = null;
            PartidaCarregada = false;
            _marcaCarregada = DateTime.MinValue;
        }

        // ---------------------------------------------------------------
        // Progrés offline
        // ---------------------------------------------------------------

        // Al mòbil el joc no sempre es tanca: sovint només passa a segon pla
        // i en tornar continua on era. Aquest temps també es dona com a
        // offline. Al mapa no cal: el temps es compta en tornar al país.
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
                SaveGameData();
            }
        }

        // La crida el panell en acceptar (o tancar). Torna el que s'ha cobrat.
        // factor: 2 si ha vist l'anunci de doblar-ho.
        public double CobrarRecompensaOffline(double factor = 1)
        {
            double doblers = _offlinePendent.doblers * factor;
            _offlinePendent = default;
            if (doblers > 0 && CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(doblers);
            SaveGameData();
            return doblers;
        }

        // Les tres capacitats de la cadena: el que podrien fer treballant sense
        // parar. Els pantans només compten si són comprats i a l'abast del tren
        // (no darrere d'una barrera). Sense país carregat, tot és 0.
        //
        // Amb 'nomesAutomatitzat' compta només el que va sol: un pantà sense
        // encarregat no suma, i un tren o un restaurant sense encarregat valen 0.
        // Amb 'senseHabilitats' es mesura amb els bonus passius dels encarregats,
        // sense les habilitats que estiguin enceses: el ritme de sempre, que no
        // es pot inflar un moment per pagar menys gemes.
        public MesuresDeLaCadena Mesures(bool nomesAutomatitzat = false, bool senseHabilitats = false)
        {
            if (!senseHabilitats) return MesurarAra(nomesAutomatitzat);

            var unitats = new System.Collections.Generic.List<UnitatMillorable>();
            if (Llista != null)
                for (int i = 0; i < Llista.Quantitat; i++)
                    if (Llista.Obtenir(i) != null) unitats.Add(Llista.Obtenir(i));
            if (Tren != null) unitats.Add(Tren);
            if (Tenda != null) unitats.Add(Tenda);

            var abans = new Bonificacions[unitats.Count];
            for (int i = 0; i < unitats.Count; i++) abans[i] = unitats[i].CanviarBonusEnSilenci(unitats[i].BonusPassiu);
            try { return MesurarAra(nomesAutomatitzat); }
            finally
            {
                for (int i = 0; i < unitats.Count; i++) unitats[i].CanviarBonusEnSilenci(abans[i]);
            }
        }

        private MesuresDeLaCadena MesurarAra(bool nomesAutomatitzat)
        {
            double pantans = 0;
            if (Llista != null)
            {
                int maxim = Mathf.Min(Llista.IndexMaximAccessible(), Llista.Quantitat - 1);
                for (int i = 0; i <= maxim; i++)
                {
                    Panta p = Llista.Obtenir(i);
                    if (p == null || !p.EstaComprat) continue;
                    if (nomesAutomatitzat && !p.EstaAutomatitzada) continue;
                    pantans += p.ProduccioReal;
                }
            }

            double cabalTren = Tren != null && (!nomesAutomatitzat || Tren.EstaAutomatitzada) ? Tren.CabalReal : 0;
            double cabalTenda = Tenda != null && (!nomesAutomatitzat || Tenda.EstaAutomatitzada) ? Tenda.CabalReal : 0;
            return new MesuresDeLaCadena(pantans, cabalTren, cabalTenda);
        }

        // Només la producció automàtica: el coll d'ampolla del que va sol, entre deu.
        // Amb el x2 permanent de la botiga, si el tens.
        public double IngresIdlePerSegon() =>
            Mesures(nomesAutomatitzat: true).Idle * (GestorDeBotiga.Instancia != null ? GestorDeBotiga.Instancia.FactorPermanent : 1);

        private double FactorOffline => GestorDeBotiga.Instancia != null && GestorDeBotiga.Instancia.OfflineX2 ? 2 : 1;

        // Per provar: com si haguessis tancat el joc fa 'segons' segons.
        public void SimularTempsFora(double segons)
        {
            if (segons <= 0 || !PartidaCarregada) return;
            AplicarProgresOffline(DateTime.UtcNow.AddSeconds(-segons));
            SaveGameData();
        }

        // Idle × temps fora, amb el tope d'hores. No es dona: queda pendent
        // fins que el jugador l'accepta al panell. Una absència curta es dona
        // directament, sense panell.
        private void AplicarProgresOffline(DateTime desDe)
        {
            if (_pais == null) return;
            double segons = (DateTime.UtcNow - desDe).TotalSeconds;
            if (segons <= 1) return;

            RecompensaOffline r = RecompensaOffline.Calcular(segons, horesMaximesOffline * 3600.0, IngresIdlePerSegon() * FactorOffline);
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

        // ---------------------------------------------------------------
        // Guanys dels països des del mapa
        // ---------------------------------------------------------------

        // El que un país ha guanyat des de l'últim cop que s'hi va guardar:
        // el pendent de l'offline més el seu idle (guardat en sortir-ne) pel
        // temps que fa, amb el mateix tope que l'offline. En la seva moneda.
        public double GuanysPendents(int z)
        {
            if (_dades == null || !_dades.TeZona(z)) return 0;
            // El país que està obert produeix de debò: no hi ha res pendent.
            if (z == zona && _pais != null) return 0;

            ZonaGuardada d = _dades.zones[z];
            double total = d.offlinePendent.doblers;
            if (d.marcaDeTempsUtc != 0 && d.idlePerSegon > 0)
            {
                double segons = (DateTime.UtcNow - DateTime.FromBinary(d.marcaDeTempsUtc)).TotalSeconds;
                total += RecompensaOffline.Calcular(segons, horesMaximesOffline * 3600.0, d.idlePerSegon * FactorOffline).doblers;
            }
            return total;
        }

        // Ho passa a la cartera d'aquell país i torna a comptar des d'ara.
        // Torna el que s'ha recollit.
        public double RecollirDeLaZona(int z)
        {
            double guanys = GuanysPendents(z);
            if (guanys <= 0) return 0;

            ZonaGuardada d = _dades.zones[z];
            if (z == _zonaDeLaMoneda && CurrencyManager.Instancia != null) CurrencyManager.Instancia.AfegirDoblers(guanys);
            else d.doblers += guanys;
            d.offlinePendent = default;
            d.marcaDeTempsUtc = DateTime.UtcNow.ToBinary();

            SaveGameData();
            return guanys;
        }

        public double DoblersDeLaZona(int z)
        {
            if (z == _zonaDeLaMoneda && CurrencyManager.Instancia != null) return CurrencyManager.Instancia.doblersActuals;
            return _dades != null && _dades.TeZona(z) ? _dades.zones[z].doblers : 0;
        }

        // ---------------------------------------------------------------
        // Carregar i desar
        // ---------------------------------------------------------------

        private void OnApplicationQuit()
        {
            SaveGameData();
        }

        // Els globals, un sol cop per sessió (o després de reiniciar).
        private void CarregarGlobals()
        {
            if (_globalsCarregats) return;
            _globalsCarregats = true;

            if (_dades == null)
            {
                _dades = new DadesDePartida();
                IniciarPartidaNova();
                return;
            }

            MigrarAPaisos();

            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EstablirGemes(_dades.gemes);
            PasDeLaGuia = _dades.pasDeLaGuia;
            if (GestorDeBotiga.Instancia != null)
            {
                double fora = _dades.marcaDeTempsUtc != 0
                    ? (DateTime.UtcNow - DateTime.FromBinary(_dades.marcaDeTempsUtc)).TotalSeconds : 0;
                GestorDeBotiga.Instancia.Carregar(_dades.botiga, fora);
            }

            // Els doblers del país on ets ja des del principi: el mapa (comprar
            // el país següent) els fa servir encara que no hi entris.
            if (_dades.TeZona(zona) && CurrencyManager.Instancia != null)
            {
                CurrencyManager.Instancia.EstablirDoblers(_dades.zones[zona].doblers);
                _zonaDeLaMoneda = zona;
            }

            Debug.Log($"GameManager: partida carregada (zona {zona})");
        }

        // La zona d'aquesta escena. Si encara no s'hi ha jugat mai, comença
        // de zero encara que la partida en tingui d'altres.
        private void CarregarZona()
        {
            _marcaCarregada = DateTime.MinValue;
            _offlinePendent = default;

            if (!_dades.TeZona(zona))
            {
                IniciarZonaNova();
                return;
            }

            ZonaGuardada z = _dades.zones[zona];
            AplicarZona(z);

            // Els doblers, el que quedava per cobrar i el rellotge són del país.
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EstablirDoblers(z.doblers);
            _zonaDeLaMoneda = zona;
            _offlinePendent = z.offlinePendent;

            // Només hi ha offline a donar si la zona ja existia: compta tant el
            // temps amb el joc tancat com el que has estat en un altre país o al mapa.
            if (z.marcaDeTempsUtc != 0)
                _marcaCarregada = DateTime.FromBinary(z.marcaDeTempsUtc);
        }

        private void AplicarZona(ZonaGuardada z)
        {
            if (Llista != null)
            {
                // Les barreres primer: decideixen quins pantans es poden
                // comprar, i ActualitzarEstats() ho llegeix després.
                Llista.AplicarBarreres(z.barreres);
                Llista.AplicarNivells(z.nivellsDePanta);
                Llista.AplicarCapses(z.capsesDePanta);
            }

            if (Tren != null)
            {
                Tren.AplicarNivell(z.nivellDeTren);
                Tren.AplicarCarrega(z.carregaDeTren);
            }

            if (Estacio != null) Estacio.AplicarDoblers(z.doblersALEstacio);

            // Abans que res que en depengui: les assignacions decideixen
            // quins trams van sols.
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.Carregar(z.encarregats, z.encarregatsCompratsPerTipus);

            if (Tenda != null) Tenda.AplicarNivell(z.nivellDeTenda);

            // El que la granota duia a mitja entrega es cobra directament.
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
            if (GestorDeBotiga.Instancia != null) GestorDeBotiga.Instancia.Carregar(null, 0);
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
            _zonaDeLaMoneda = zona;
            _offlinePendent = default;

            if (Llista != null)
                Llista.AplicarNivells(new int[Llista.Quantitat]);
        }

        public void SaveGameData()
        {
            if (_dades == null || !_globalsCarregats) return;

            DateTime ara = DateTime.UtcNow;
            _dades.versio = DadesDePartida.VersioActual;
            _dades.marcaDeTempsUtc = ara.ToBinary();
            _dades.gemes = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.gemesActuals : 0;
            _dades.pasDeLaGuia = PasDeLaGuia;
            _dades.zonaActual = zona;
            if (GestorDeBotiga.Instancia != null) _dades.botiga = GestorDeBotiga.Instancia.Guardar();

            // Els doblers del país on ets, també des del mapa (s'hi pot comprar
            // el país següent o recollir-hi guanys).
            if (_zonaDeLaMoneda >= 0 && CurrencyManager.Instancia != null && _dades.TeZona(_zonaDeLaMoneda))
                _dades.zones[_zonaDeLaMoneda].doblers = CurrencyManager.Instancia.doblersActuals;

            // La resta de la zona només amb el país carregat. Al mapa no es toca
            // la marca de temps: el temps al mapa es cobra com a offline.
            if (PartidaCarregada && _pais != null)
            {
                ZonaGuardada z = _dades.Zona(zona);
                z.doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0;
                z.marcaDeTempsUtc = _dades.marcaDeTempsUtc;
                z.offlinePendent = _offlinePendent;
                z.idlePerSegon = IngresIdlePerSegon();
                z.nivellsDePanta = Llista.ObtenirNivells();
                z.capsesDePanta = Llista.ObtenirCapses();
                z.barreres = Llista.ObtenirBarreres();
                z.nivellDeTren = Tren != null ? Tren.Nivell : 1;
                z.carregaDeTren = Tren != null ? Tren.CarregaActual : 0;
                z.doblersALEstacio = Estacio != null ? Estacio.DoblersEmmagatzemats : 0;
                z.nivellDeTenda = Tenda != null ? Tenda.Nivell : 1;
                z.carregaDelTransportista = 0;
                z.encarregats = GestorDEncarregats.Instancia != null ? GestorDEncarregats.Instancia.Guardar() : null;
                z.encarregatsCompratsPerTipus = GestorDEncarregats.Instancia != null
                    ? GestorDEncarregats.Instancia.GuardarComprats() : null;
            }

            ServeiDeGuardat.Guardar(_dades);
        }

        // ---------------------------------------------------------------
        // Països i escenes
        // ---------------------------------------------------------------

        public int PaisosDesbloquejats => _dades != null ? Mathf.Max(1, _dades.paisosDesbloquejats) : 1;

        public void DesbloquejarSeguentPais()
        {
            if (_dades == null) return;
            _dades.paisosDesbloquejats = PaisosDesbloquejats + 1;
            SaveGameData();
        }

        // Desa el país i obre l'escena del mapa.
        public void AnarAlMapa()
        {
            SaveGameData();
            DeixarElPais();
            CarregarEscena(escenaDelMapa);
        }

        // Del mapa al país on eres.
        public void TornarAlPais() => CarregarEscena(escenaDelPais);

        // Desa aquest país i obre l'escena del país amb l'altre. En tornar-hi
        // més tard, el temps que hi has faltat es cobra com a offline.
        public void CanviarDeZona(int n)
        {
            if (n < 0 || n >= PaisosDesbloquejats) return;
            if (n == zona && _pais != null) return;
            SaveGameData();
            DeixarElPais();
            zona = n;
            _dades.zonaActual = n;
            // La moneda del país nou es carrega en entrar-hi.
            _zonaDeLaMoneda = -1;
            ServeiDeGuardat.Guardar(_dades);
            CarregarEscena(escenaDelPais);
        }

        private void MigrarAPaisos() => MigracioDePartida.PassarAPaisos(_dades);

        // Esborra la partida i torna a començar al primer país.
        public void ReiniciarPartida()
        {
            ServeiDeGuardat.Esborrar();
            DeixarElPais();
            _dades = null;
            _globalsCarregats = false;
            _zonaDeLaMoneda = -1;
            zona = 0;
            CarregarGlobals();
            CarregarEscena(escenaDelPais);
        }

        // Fins que l'escena vella es descarrega (al final del frame) els seus
        // objectes encara existeixen: que cap desat no hi llegeixi res.
        private void DeixarElPais()
        {
            _pais = null;
            PartidaCarregada = false;
            _marcaCarregada = DateTime.MinValue;
        }

        private static void CarregarEscena(string nom)
        {
            if (Application.CanStreamedLevelBeLoaded(nom))
            {
                SceneManager.LoadScene(nom);
                return;
            }
#if UNITY_EDITOR
            // A l'editor, encara que no sigui a la llista d'escenes del build.
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"{nom} t:Scene"))
            {
                string ruta = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(ruta) != nom) continue;
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            Debug.LogError($"GameManager: l'escena {nom} no existeix o no és a la llista d'escenes del build");
        }
    }
}
