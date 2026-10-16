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
        [Tooltip("Un de sol. Dos efectes al mateix encarregat multipliquen massa.")]
        public TipusDEfecte efecte = TipusDEfecte.VelocitatDeMoviment;

        [Tooltip("Multiplicador (3 = ×3). Per a ReduccioDeCost és la fracció que es " +
                 "rebaixa: 0.4 = −40%.")]
        public double magnitud = 2;

        [Header("Habilitat activa")]
        [Tooltip("Multiplicador extra mentre l'habilitat està encesa")]
        public double magnitudDeLHabilitat = 3;
        [Tooltip("Com més rar, més llarga")]
        public float segonsDeDuracio = 60;
        [Tooltip("Compta des de l'activació. Amb 5 vegades la durada, l'habilitat està encesa un 20% del temps")]
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

        // L'habilitat multiplica l'efecte passiu pel seu factor mentre dura.
        // Per a la rebaixa de cost el que compta és el total, amb el mateix
        // topall del 95% que aplica el gestor.
        private string ValorDeLHabilitat => EsReduccioDeCost
            ? $"-{FormatadorDeNombres.Factor(System.Math.Min(0.95, magnitud * magnitudDeLHabilitat) * 100)}%"
            : $"{FormatadorDeNombres.Factor(magnitudDeLHabilitat)}x";

        // "5x Velocitat de recol·lecció" — la targeta de l'encarregat assignat.
        public string DescripcioDeLHabilitat => $"{ValorDeLHabilitat} {NomDeLEix(false)}";

        // "5x Vel" — les cartes petites de la llista.
        public string DescripcioCurtaDeLHabilitat => $"{ValorDeLHabilitat} {NomDeLEix(true)}";

        // "3m"
        public string DuracioCurta => FormatadorDeNombres.TempsCurt(segonsDeDuracio);

        // El mateix efecte vol dir coses diferents a cada tram: treballar, al
        // pantà, és recollir; al tren i a la tenda, carregar.
        private string NomDeLEix(bool curt) => efecte switch
        {
            TipusDEfecte.VelocitatDeMoviment => curt ? "Vel" : "Velocitat",
            TipusDEfecte.VelocitatDeTreball when lloc == TipusDeLloc.Panta =>
                curt ? "Recol" : "Velocitat de recol·lecció",
            TipusDEfecte.VelocitatDeTreball => curt ? "Càrrega" : "Velocitat de càrrega",
            TipusDEfecte.Capacitat => curt ? "Cap" : "Capacitat",
            TipusDEfecte.ReduccioDeCost => curt ? "cost" : "Cost de millora",
            _ => string.Empty
        };

        private void OnValidate()
        {
            // El pantà no té cap eix de capacitat que l'encarregat pugui
            // multiplicar, així que aquesta combinació no faria res.
            if (lloc == TipusDeLloc.Panta && efecte == TipusDEfecte.Capacitat)
                Debug.LogWarning($"{name}: un encarregat de pantà amb efecte Capacitat no fa res.", this);

            if (string.IsNullOrWhiteSpace(id)) id = name;
        }
    }
}
