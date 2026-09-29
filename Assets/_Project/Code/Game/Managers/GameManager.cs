using System;
using UnityEngine;

namespace IdleFrogs.Game
{
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

        // Events per a que altres managers es suscriguin

        public event Action EnTickDeJoc;
        public event Action EnTickDeSegon;

        private float _temporitzadorTick;
        private float _temporitzadorSegon;
        private float _comptadorDAutoGuardat;


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

            // Al final: les unitats ja existeixen i els nivells ja s'han
            // carregat, així que aquí és on els bonus i l'automatisme
            // arriben als trams que toca.
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.RegistrarUnitats(llistaDePantans, tren, tenda);

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

        #region Application Lifecycle & Saving

        private void OnApplicationPause(bool pauseStatus)
        {
            // On mobile, app pausing happens when switching apps or locking the screen
            if (pauseStatus)
            {
                SaveGameData();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGameData();
        }

        private void CarregarPartida()
        {
            DadesDePartida dades = ServeiDeGuardat.Carregar();
            if (dades == null)
            {
                Debug.Log("GameManager: partida nova");
                return;
            }

            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.EstablirDoblers(dades.doblers);

            if (llistaDePantans != null)
            {
                // Les barreres primer: decideixen quins pantans es poden
                // comprar, i ActualitzarEstats() ho llegeix després.
                llistaDePantans.AplicarBarreres(dades.barreres);
                llistaDePantans.AplicarNivells(dades.nivellsDePanta);
                llistaDePantans.AplicarCapses(dades.capsesDePanta);
            }

            if (tren != null)
            {
                tren.AplicarNivell(dades.nivellDeTren);
                tren.AplicarCarrega(dades.carregaDeTren);
            }

            if (estacio != null) estacio.AplicarDoblers(dades.doblersALEstacio);

            // Abans que res que en depengui: les assignacions decideixen
            // quins trams van sols.
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.Carregar(dades.encarregats, dades.encarregatsComprats);

            if (tenda != null) tenda.AplicarNivell(dades.nivellDeTenda);

            // El que la granota duia a mitja entrega es cobra directament:
            // anava cap a la cartera igualment i així no es perd res ni cal
            // recrear-li l'estat de la corrutina.
            if (dades.carregaDelTransportista > 0 && CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(dades.carregaDelTransportista);

            Debug.Log("GameManager: partida carregada");
        }

        public void SaveGameData()
        {
            if (llistaDePantans == null) return;

            ServeiDeGuardat.Guardar(new DadesDePartida
            {
                marcaDeTempsUtc = DateTime.UtcNow.ToBinary(),
                doblers = CurrencyManager.Instancia != null ? CurrencyManager.Instancia.doblersActuals : 0,
                nivellsDePanta = llistaDePantans.ObtenirNivells(),
                capsesDePanta = llistaDePantans.ObtenirCapses(),
                barreres = llistaDePantans.ObtenirBarreres(),
                nivellDeTren = tren != null ? tren.Nivell : 1,
                carregaDeTren = tren != null ? tren.CarregaActual : 0,
                doblersALEstacio = estacio != null ? estacio.DoblersEmmagatzemats : 0,
                nivellDeTenda = tenda != null ? tenda.Nivell : 1,
                carregaDelTransportista = tenda != null ? tenda.CarregaDelTransportista : 0,
                encarregats = GestorDEncarregats.Instancia != null
                    ? GestorDEncarregats.Instancia.Guardar() : null,
                encarregatsComprats = GestorDEncarregats.Instancia != null
                    ? GestorDEncarregats.Instancia.Comprats : 0
            });
        }

        #endregion
    }
}
