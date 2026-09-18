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

        // Events per a que altres managers es suscriguin

        public event Action EnTickDeJoc;
        public event Action EnTickDeSegon;

        private float _temporitzadorTick;
        private float _temporitzadorSegon;


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

            InicialitzarJoc();
        }

        private void InicialitzarJoc()
        {
            // 1. Carregar dades guardades
            // 2: Calcular el progres offline
            // 3; Inicialitzar les entitats (mines , tenda etc)
            // 4. Actualitzar la IU 
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

        public void SaveGameData()
        {
            // Save timestamp (DateTime.UtcNow) along with player currency and shaft levels
            Debug.Log("GameManager: Auto-saving game state...");
        }

        #endregion
    }
}
