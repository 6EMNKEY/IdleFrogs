using UnityEngine;

namespace IdleFrogs.Game
{
    // Les peces d'un país: va a l'escena dels pantans. En arrencar es
    // presenta al GameManager (que viu fora de les escenes), que hi carrega
    // la zona guardada; en descarregar-se l'escena, se'n despedeix.
    public class EscenaDelPais : MonoBehaviour
    {
        public static EscenaDelPais Instancia { get; private set; }

        [SerializeField] private LlistaDePantans llistaDePantans;
        [SerializeField] private ControladorTren tren;
        [SerializeField] private Estacio estacio;
        [SerializeField] private Tenda tenda;

        public LlistaDePantans Llista => llistaDePantans;
        public ControladorTren Tren => tren;
        public Estacio Estacio => estacio;
        public Tenda Tenda => tenda;

        private void Awake() => Instancia = this;

        private void Start()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.EntrarAlPais(this);
            else Debug.LogError("EscenaDelPais: no hi ha GameManager (falta Resources/Sistemes?)", this);
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null) GameManager.Instancia.SortirDelPais(this);
            if (Instancia == this) Instancia = null;
        }
    }
}
