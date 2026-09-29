using UnityEngine;

namespace IdleFrogs.Game
{
    // La definició d'un encarregat. És un asset compartit: no hi pot haver
    // cap estat d'instància aquí dins, perquè el jugador en pot tenir dos
    // del mateix assignats a pantans diferents. L'estat viu a GestorDEncarregats.
    [CreateAssetMenu(fileName = "Encarregat", menuName = "IdleFrogs/Encarregat")]
    public class EncarregatSO : ScriptableObject
    {
        [Header("Identitat")]
        [Tooltip("Identificador estable. El guardat el desa com a text, així que " +
                 "canviar-lo trenca les partides existents. El nom de l'asset sí que es pot canviar.")]
        public string id;
        public string nomVisible;
        public Sprite retrat;

        [Header("Classificació")]
        public TipusDeLloc lloc = TipusDeLloc.Panta;
        public Raresa raresa = Raresa.Comu;

        [Header("Efecte passiu — sempre actiu mentre està assignat")]
        [Tooltip("Un de sol, com a IMT. Dos efectes al mateix encarregat multipliquen massa.")]
        public TipusDEfecte efecte = TipusDEfecte.VelocitatDeMoviment;

        [Tooltip("Multiplicador (3 = ×3). Per a ReduccioDeCost és la fracció que es " +
                 "rebaixa: 0.4 = −40%.")]
        public double magnitud = 2;

        [Header("Habilitat activa")]
        [Tooltip("Multiplicador extra mentre l'habilitat està encesa")]
        public double magnitudDeLHabilitat = 3;
        [Tooltip("IMT: 1 min Junior, 3 min Senior, 10 min Executive")]
        public float segonsDeDuracio = 60;
        [Tooltip("IMT: 5 min Junior, 15 min Senior, 50 min Executive (≈20% d'ús)")]
        public float segonsDeRecarrega = 300;

        public bool EsReduccioDeCost => efecte == TipusDEfecte.ReduccioDeCost;

        // Text curt per a la fitxa del panell. La reducció s'ensenya com a
        // percentatge i la resta com a multiplicador, que és com es llegeixen.
        public string DescripcioDeLEfecte => efecte switch
        {
            TipusDEfecte.VelocitatDeMoviment => $"Velocitat ×{FormatadorDeNombres.Decimals(magnitud, 1)}",
            TipusDEfecte.VelocitatDeTreball => $"Treball ×{FormatadorDeNombres.Decimals(magnitud, 1)}",
            TipusDEfecte.Capacitat => $"Capacitat ×{FormatadorDeNombres.Decimals(magnitud, 1)}",
            TipusDEfecte.ReduccioDeCost => $"Cost −{FormatadorDeNombres.Decimals(magnitud * 100, 0)}%",
            _ => string.Empty
        };

        public string DescripcioDeLHabilitat =>
            $"×{FormatadorDeNombres.Decimals(magnitudDeLHabilitat, 1)} durant " +
            $"{FormatadorDeNombres.Temps(segonsDeDuracio)}";

        private void OnValidate()
        {
            // El pantà no té cap eix de capacitat (a IMT la taula del Mine
            // Shaft tampoc en té), així que aquesta combinació no faria res.
            if (lloc == TipusDeLloc.Panta && efecte == TipusDEfecte.Capacitat)
                Debug.LogWarning($"{name}: un encarregat de pantà amb efecte Capacitat no fa res.", this);

            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
