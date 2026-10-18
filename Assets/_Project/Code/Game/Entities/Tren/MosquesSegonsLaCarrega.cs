using UnityEngine;

namespace IdleFrogs.Game
{
    [System.Serializable]
    public class TramDeMosques
    {
        [Tooltip("A partir de quina part de la capacitat (0,25 = 25%)")]
        [Range(0f, 1f)] public float desDe;
        [Min(0)] public int mosques;
    }

    // Les mosques que volen dins del recipient del tren: com més ple va, més
    // n'hi ha. Buit, cap. Només és visual: la càrrega la porta ControladorTren.
    //
    // Va al tren (o a qualsevol fill seu). Les mosques són un EixamDeMosques
    // fill, amb la zona ajustada al recipient i "No Sortir De La Zona" marcat.
    public class MosquesSegonsLaCarrega : MonoBehaviour
    {
        [Tooltip("Buit: es busca pujant per la jerarquia")]
        [SerializeField] private ControladorTren tren;
        [Tooltip("Buit: es busca entre els fills")]
        [SerializeField] private EixamDeMosques eixam;

        [Header("Succionador de l'estació")]
        [Tooltip("La boca del succionador. Quan el tren descarrega, les mosques hi van una a una")]
        [SerializeField] private Transform puntDAspiracio;
        [Tooltip("Segons que tarda cada mosca a arribar-hi. Si la descàrrega és més curta, s'escurça")]
        [Min(0.05f)] [SerializeField] private float duracioDelViatge = 0.5f;

        [Header("Quantes segons la càrrega")]
        [Tooltip("Quantes mosques es veuen segons com de ple va. Mana el tram més alt que s'hagi " +
                 "superat; amb el tren buit no n'hi ha cap")]
        [SerializeField] private TramDeMosques[] trams =
        {
            new TramDeMosques { desDe = 0f,    mosques = 1 },
            new TramDeMosques { desDe = 0.25f, mosques = 3 },
            new TramDeMosques { desDe = 0.5f,  mosques = 7 },
            new TramDeMosques { desDe = 0.75f, mosques = 15 },
        };

        private void Awake()
        {
            if (tren == null) tren = GetComponentInParent<ControladorTren>();
            if (eixam == null) eixam = GetComponentInChildren<EixamDeMosques>(true);
        }

        private void Start()
        {
            if (tren == null || eixam == null)
            {
                Debug.LogError("MosquesSegonsLaCarrega: necessita un ControladorTren a sobre i un EixamDeMosques a sota", this);
                enabled = false;
                return;
            }

            tren.EnCanviDeCarrega += Refrescar;
            // Millorar el tren canvia la capacitat, i amb ella el percentatge.
            tren.EnCanviDeNivell += RefrescarPerNivell;
            tren.EnComencaADescarregar += EnDescarregar;
            Refrescar(tren.CarregaActual);
        }

        private void OnDestroy()
        {
            if (tren == null) return;
            tren.EnCanviDeCarrega -= Refrescar;
            tren.EnCanviDeNivell -= RefrescarPerNivell;
            tren.EnComencaADescarregar -= EnDescarregar;
        }

        private void EnDescarregar(float segons)
        {
            if (puntDAspiracio == null || !isActiveAndEnabled) return;
            StartCoroutine(Aspirar(segons));
        }

        // Repartides al llarg de la descàrrega, la més propera a la boca
        // primer; l'última hi arriba just quan s'acaba.
        private System.Collections.IEnumerator Aspirar(float segons)
        {
            int n = eixam.QuantesVolen();
            if (n == 0) yield break;

            float viatge = Mathf.Min(duracioDelViatge, Mathf.Max(0.05f, segons * 0.5f));
            float finestra = Mathf.Max(0f, segons - viatge);
            float inici = Time.time;

            for (int k = 0; k < n; k++)
            {
                float quan = n > 1 ? finestra * k / (n - 1) : 0f;
                while (Time.time - inici < quan) yield return null;

                int i = eixam.LaMesProperA(puntDAspiracio.position);
                if (i < 0) yield break;
                eixam.Aspirar(i, puntDAspiracio.position, viatge);
            }
        }

        private void RefrescarPerNivell() => Refrescar(tren.CarregaActual);

        private void Refrescar(double carrega)
        {
            double capacitat = tren.Capacitat;
            float fraccio = capacitat > 0 ? (float)(carrega / capacitat) : 0f;
            eixam.EstablirVisibles(Quantes(fraccio, trams));
        }

        // El tram amb el 'desDe' més alt que la fracció ja ha superat. Buit
        // (0) no en té cap encara que hi hagi un tram que comenci a 0.
        public static int Quantes(float fraccio, TramDeMosques[] trams)
        {
            if (fraccio <= 0f || trams == null) return 0;
            int quantes = 0;
            float millor = -1f;
            foreach (TramDeMosques t in trams)
                if (t != null && fraccio >= t.desDe && t.desDe > millor) { millor = t.desDe; quantes = t.mosques; }
            return quantes;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EixamDeMosques e = eixam != null ? eixam : GetComponentInChildren<EixamDeMosques>(true);
            if (e == null || trams == null) return;
            foreach (TramDeMosques t in trams)
                if (t != null && t.mosques > e.Quantes)
                    Debug.LogWarning($"{name}: un tram demana {t.mosques} mosques però l'eixam només en té {e.Quantes}. " +
                                     "Puja Quantes a l'EixamDeMosques.", this);
        }
#endif
    }
}
