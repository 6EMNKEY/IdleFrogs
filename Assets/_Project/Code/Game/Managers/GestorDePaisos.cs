using UnityEngine;

namespace IdleFrogs.Game
{
    // Els països del mapa, en ordre. Cada país de pantans és una zona del
    // guardat (la zona n és el n-è país sense comptar la ciutat comercial).
    // Es desbloquegen en ordre, pagant amb la moneda del país on ets.
    [DefaultExecutionOrder(-190)]
    public class GestorDePaisos : MonoBehaviour
    {
        public static GestorDePaisos Instancia { get; private set; }

        [Tooltip("En l'ordre del mapa. El primer de pantans és la zona 0 i ja ve obert")]
        [SerializeField] private PaisSO[] paisos;

        public event System.Action EnCanviDePaisos;

        // En l'ordre del mapa, amb la ciutat comercial si hi és.
        public System.Collections.Generic.IReadOnlyList<PaisSO> Paisos => paisos;

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

        // ---------------------------------------------------------------
        // Zones
        // ---------------------------------------------------------------

        // La zona d'un país (o -1 si és la ciutat comercial o no hi és).
        public int ZonaDe(PaisSO pais)
        {
            if (pais == null || paisos == null || pais.esCiutatComercial) return -1;
            int zona = 0;
            foreach (PaisSO p in paisos)
            {
                if (p == null || p.esCiutatComercial) continue;
                if (p == pais) return zona;
                zona++;
            }
            return -1;
        }

        public PaisSO PaisDeLaZona(int zona)
        {
            if (paisos == null || zona < 0) return null;
            int i = 0;
            foreach (PaisSO p in paisos)
            {
                if (p == null || p.esCiutatComercial) continue;
                if (i == zona) return p;
                i++;
            }
            return null;
        }

        public PaisSO PaisActual => GameManager.Instancia != null ? PaisDeLaZona(GameManager.Instancia.Zona) : PaisDeLaZona(0);

        public AspectePantaSO AspecteDelsPantans => PaisActual != null ? PaisActual.aspecteDelsPantans : null;

        // ---------------------------------------------------------------
        // Desbloqueig
        // ---------------------------------------------------------------

        private int Desbloquejats => GameManager.Instancia != null ? GameManager.Instancia.PaisosDesbloquejats : 1;

        public bool EstaDesbloquejat(PaisSO pais)
        {
            if (pais == null) return false;
            if (pais.esCiutatComercial) return true;
            int zona = ZonaDe(pais);
            return zona >= 0 && zona < Desbloquejats;
        }

        // El següent que es pot comprar, o null si ja estan tots oberts.
        public PaisSO Seguent => PaisDeLaZona(Desbloquejats);

        public bool EsElSeguent(PaisSO pais) => pais != null && pais == Seguent;

        public bool PucComprar(PaisSO pais) =>
            EsElSeguent(pais) && CurrencyManager.Instancia != null &&
            CurrencyManager.Instancia.doblersActuals >= pais.preuDeDesbloqueig;

        public bool Comprar(PaisSO pais)
        {
            if (!EsElSeguent(pais) || GameManager.Instancia == null || CurrencyManager.Instancia == null) return false;
            if (!CurrencyManager.Instancia.GastarDoblers(pais.preuDeDesbloqueig)) return false;

            GameManager.Instancia.DesbloquejarSeguentPais();
            EnCanviDePaisos?.Invoke();
            return true;
        }

        // Anar als pantans d'un país obert. Guarda i torna a carregar l'escena.
        public void Viatjar(PaisSO pais)
        {
            int zona = ZonaDe(pais);
            if (zona < 0 || !EstaDesbloquejat(pais) || GameManager.Instancia == null) return;
            GameManager.Instancia.CanviarDeZona(zona);
        }
    }
}
