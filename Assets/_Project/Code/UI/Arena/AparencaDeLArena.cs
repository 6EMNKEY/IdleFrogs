using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // El nom, el dibuix i el color de lluitadors i bosses per a la UI.
    // Sense dibuix (els de mostra), un quadrat del color de la raresa.
    public static class AparencaDeLArena
    {
        private static ArenaConfigSO Config => ArenaConfigSO.Actual;

        public static Color Color(int raresa)
        {
            DadesDeRaresa d = Config != null ? Config.Taula.Dades(raresa) : null;
            return d != null ? d.color : UnityEngine.Color.white;
        }

        public static string Raresa(int raresa) => ((RaresaDeLluita)raresa).ToString();

        public static Sprite Sprite(LluitadorGuardat l)
        {
            LluitadorSO so = l != null && Config != null ? Config.Lluitador(l.idDefinicio) : null;
            return so != null ? so.sprite : null;
        }

        public static string Nom(LluitadorGuardat l)
        {
            if (l == null) return string.Empty;
            LluitadorSO so = Config != null ? Config.Lluitador(l.idDefinicio) : null;
            return so != null ? so.NomVisible : Raresa(l.raresa);
        }

        public static Sprite Sprite(BossALArena b)
        {
            BossSO so = b != null && Config != null ? Config.Boss(b.idBoss) : null;
            return so != null ? so.sprite : null;
        }

        public static string Nom(BossALArena b)
        {
            if (b == null) return string.Empty;
            BossSO so = Config != null ? Config.Boss(b.idBoss) : null;
            return so != null ? so.NomVisible : "Boss " + Raresa(b.raresa);
        }

        // El dibuix, o el quadrat de color si no n'hi ha.
        public static void Posar(UnityEngine.UI.Image img, Sprite sprite, int raresa)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.color = sprite != null ? UnityEngine.Color.white : Color(raresa);
        }
    }
}
