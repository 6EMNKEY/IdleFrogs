using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    [System.Serializable]
    public class CanviDeSprite
    {
        [Tooltip("El del primer país: el que hi ha posat a l'escena o al prefab")]
        public Sprite original;
        [Tooltip("El d'aquest país. Buit: es queda l'original")]
        public Sprite perAquestPais;
    }

    // Quin sprite fa servir el país on ets en lloc de cada un del primer país.
    //
    // L'escena i els prefabs són el primer país. Un país només diu què canvia
    // ("aquest tren → aquest altre"); el que no diu es queda com està. Per
    // això no cal tocar cap prefab per fer un país nou.
    //
    // Els objectes de l'escena els canvia AspecteDelPais en arrencar. Els que
    // posen un sprite pel seu compte més tard (clients, boca oberta, siluetes,
    // retrats, aspecte dels pantans) el passen per Substituir.
    public static class SpritesDelPais
    {
        private static PaisSO _pais;
        private static bool _preparat;
        private static readonly Dictionary<Sprite, Sprite> _canvis = new Dictionary<Sprite, Sprite>();

        public static Sprite Substituir(Sprite original)
        {
            if (original == null) return null;
            Preparar();
            return _canvis.TryGetValue(original, out Sprite s) ? s : original;
        }

        public static int QuantsCanvis
        {
            get { Preparar(); return _canvis.Count; }
        }

        // Torna a llegir el país. AspecteDelPais ho crida a cada càrrega
        // d'escena: amb el Domain Reload apagat, l'estàtic sobreviu entre Plays
        // i es quedaria amb els canvis de l'anterior.
        public static void Refrescar() => _preparat = false;

        private static void Preparar()
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            if (_preparat && pais == _pais) return;

            _pais = pais;
            _preparat = true;
            Omplir(_canvis, pais);
        }

        // Separat per poder-ho provar sense escena.
        public static void Omplir(Dictionary<Sprite, Sprite> canvis, PaisSO pais)
        {
            canvis.Clear();
            if (pais == null) return;

            foreach (CanviDeSprite c in pais.CanvisDeSprites())
            {
                if (c == null || c.original == null || c.perAquestPais == null) continue;
                // Si el mateix original surt dues vegades, mana el primer.
                if (!canvis.ContainsKey(c.original)) canvis.Add(c.original, c.perAquestPais);
            }
        }
    }
}
