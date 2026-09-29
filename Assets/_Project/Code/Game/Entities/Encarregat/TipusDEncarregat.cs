using UnityEngine;

namespace IdleFrogs.Game
{
    // On pot treballar un encarregat. Ve determinat per la silueta des d'on
    // el compres, no per sort: si el compres des d'un pantà surt de pantà.
    // Així no pots quedar-te pagant tirades que no et serveixen de res.
    public enum TipusDeLloc { Panta, Tren, Tenda }

    // Només la raresa és aleatòria. Marca la força de l'efecte i el color.
    public enum Raresa { Comu, Rar, Epic, Llegendari }

    // Un encarregat té UN sol efecte passiu, com a IMT. El segon efecte és
    // l'habilitat activa amb cooldown, que va a part.
    public enum TipusDEfecte
    {
        VelocitatDeMoviment,   // caminar (pantà, tenda) / moure's (tren)
        VelocitatDeTreball,    // capturar (pantà) / transferir (tren, tenda)
        Capacitat,             // load expansion — no aplica al pantà
        ReduccioDeCost         // rebaixa el cost de millora d'aquell tram
    }

    public static class ColorsDeRaresa
    {
        public static Color Per(Raresa raresa) => raresa switch
        {
            Raresa.Rar => new Color(0.30f, 0.65f, 1.00f),
            Raresa.Epic => new Color(0.70f, 0.40f, 0.95f),
            Raresa.Llegendari => new Color(1.00f, 0.70f, 0.15f),
            _ => new Color(0.75f, 0.75f, 0.75f)
        };
    }

    // Una ranura concreta del món: quin tram i, si és un pantà, quin.
    // L'índex del pantà ja és l'identificador estable que fa servir
    // LlistaDePantans i el guardat, així que reaprofitem el mateix.
    [System.Serializable]
    public struct Ranura : System.IEquatable<Ranura>
    {
        public TipusDeLloc tipus;
        public int index;

        public Ranura(TipusDeLloc tipus, int index = 0)
        {
            this.tipus = tipus;
            this.index = index;
        }

        public bool Equals(Ranura altra) => tipus == altra.tipus && index == altra.index;
        public override bool Equals(object o) => o is Ranura r && Equals(r);
        public override int GetHashCode() => ((int)tipus * 397) ^ index;
        public override string ToString() => tipus == TipusDeLloc.Panta ? $"Pantà {index + 1}" : tipus.ToString();
    }
}
