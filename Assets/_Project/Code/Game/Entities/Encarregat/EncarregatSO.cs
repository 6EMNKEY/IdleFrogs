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
        [Tooltip("El de les cartes del panell. Buit: el del seu tipus al catàleg")]
        public Sprite retrat;
        [Tooltip("El que es veu al món, a la silueta. Buit: el del seu tipus al catàleg")]
        public Sprite spriteAlMon;

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

        // Text curt de l'efecte passiu. Tot en percentatge: amb bonus petits
        // (×1.05) un multiplicador es llegeix pitjor que "+5%".
        public string DescripcioDeLEfecte => efecte switch
        {
            TipusDEfecte.ReduccioDeCost => $"−{Percentatge(magnitud)}% {NomDeLEix(false)}",
            TipusDEfecte.VelocitatDeMoviment or TipusDEfecte.VelocitatDeTreball or TipusDEfecte.Capacitat =>
                $"+{Percentatge(magnitud - 1)}% {NomDeLEix(false)}",
            _ => string.Empty
        };

        // L'habilitat multiplica l'efecte passiu pel seu factor mentre dura.
        // Per a la rebaixa de cost el que compta és el total, amb el mateix
        // topall del 95% que aplica el gestor.
        private string ValorDeLHabilitat => EsReduccioDeCost
            ? $"-{FormatadorDeNombres.Factor(System.Math.Min(0.95, magnitud * magnitudDeLHabilitat) * 100)}%"
            : $"{FormatadorDeNombres.Factor(magnitudDeLHabilitat)}x";

        // "5x Velocitat de recol·lecció" — la targeta de l'encarregat assignat.
        private static string Percentatge(double fraccio) => FormatadorDeNombres.Factor(fraccio * 100);

        public string DescripcioDeLHabilitat => $"{ValorDeLHabilitat} {NomDeLEix(false)}";

        // "5x Vel" — les cartes petites de la llista.
        public string DescripcioCurtaDeLHabilitat => $"{ValorDeLHabilitat} {NomDeLEix(true)}";

        // "3m"
        public string DuracioCurta => FormatadorDeNombres.TempsCurt(segonsDeDuracio);

        // El mateix efecte vol dir coses diferents a cada tram: treballar, al
        // pantà, és recollir; al tren i a la tenda, carregar.
        private static string Eix(string nom, bool curt) => Textos.T(curt ? $"eix.{nom}.curt" : $"eix.{nom}");

        private string NomDeLEix(bool curt) => efecte switch
        {
            TipusDEfecte.VelocitatDeMoviment => Eix("velocitat", curt),
            TipusDEfecte.VelocitatDeTreball when lloc == TipusDeLloc.Panta => Eix("recollir", curt),
            TipusDEfecte.VelocitatDeTreball when lloc == TipusDeLloc.Tenda => Eix("menjar", curt),
            TipusDEfecte.VelocitatDeTreball => Eix("carregar", curt),
            TipusDEfecte.Capacitat when lloc == TipusDeLloc.Tenda => Eix("per_client", curt),
            TipusDEfecte.Capacitat => Eix("capacitat", curt),
            TipusDEfecte.ReduccioDeCost => Eix("cost", curt),
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
