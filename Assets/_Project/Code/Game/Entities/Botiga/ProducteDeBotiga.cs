using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum SeccioDeBotiga { Ofertes, Pase, Gratis, Gemes, Millores }

    // En una botiga horitzontal amb columnes de dues files: la petita ocupa
    // mitja columna; la gran, l'alçada sencera i dues columnes d'amplada.
    // GranDoble és el mateix que Gran (es manté pels assets que ja la fan servir).
    public enum MidaDeTargeta { Petita, Gran, GranDoble }

    public enum TipusDePagament { Gratis, Anunci, Gemes, DinerReal }

    // Automàtic: senzill si dona una sola cosa i no és cap oferta.
    public enum InformacioDelProducte { Automatica, Senzill, AmbInformacio }

    // Un producte de la botiga. La botiga es munta sola amb tots els del
    // catàleg: afegir una oferta és crear un asset, sense tocar l'escena.
    [CreateAssetMenu(fileName = "Producte", menuName = "IdleFrogs/ProducteDeBotiga")]
    public class ProducteDeBotiga : ScriptableObject
    {
        [Header("Identitat")]
        [Tooltip("Estable: el guardat hi apunta (compres fetes, límits). No el canviïs un cop publicat")]
        public string id;
        public TextTraduit nom;
        public Sprite icona;
        [Tooltip("Opcional: \"Popular\", \"Millor valor\", \"+25%\"")]
        public TextTraduit etiqueta;
        [Tooltip("Senzill: la targeta només diu què dona i el preu. Amb informació: el nom i un botó (i) " +
                 "amb tot el detall. Automàtica: amb informació si dona més d'una cosa o és una oferta")]
        public InformacioDelProducte informacio;

        [Header("On surt")]
        public SeccioDeBotiga seccio;
        public MidaDeTargeta mida;

        [Header("Pagament")]
        public TipusDePagament pagament;
        [Min(0)] public int preuEnGemes;
        [Tooltip("L'id del producte a App Store / Google Play")]
        public string idDeCompra;
        [Tooltip("El que surt mentre la botiga real no dona el preu de la seva moneda")]
        public string preuDeMostra = "0,99 €";

        [Header("Què dona")]
        public List<Recompensa> recompenses = new List<Recompensa>();
        [Tooltip("Marcat: en dona UNA a l'atzar segons el pes. Desmarcat: les dona totes")]
        public bool unaAlAtzar;
        [Tooltip("La primera compra dona el doble de gemes (packs de gemes)")]
        public bool doblePrimeraCompra;

        [Header("Límits")]
        [Tooltip("Quantes vegades es pot comprar en total (0 = sense límit)")]
        [Min(0)] public int limit;
        [Tooltip("Quantes vegades al dia (0 = sense límit). Es reinicia a mitjanit")]
        [Min(0)] public int limitAlDia;
        [Tooltip("Hores entre un ús i el següent (regal gratis). 0 = sense espera")]
        [Min(0)] public float horesEntreUsos;
        [Tooltip("Hores que dura l'oferta des de la primera vegada que el jugador la veu. 0 = sempre")]
        [Min(0)] public float horesDOferta;

        // Si la targeta porta el botó (i) amb el detall.
        public bool TeInformacio
        {
            get
            {
                if (informacio != InformacioDelProducte.Automatica) return informacio == InformacioDelProducte.AmbInformacio;
                if (recompenses.Count != 1 || unaAlAtzar) return true;
                if (seccio == SeccioDeBotiga.Ofertes || horesDOferta > 0) return true;
                Recompensa r = recompenses[0];
                return r == null || r.tipus == TipusDeRecompensa.Cofre || r.tipus == TipusDeRecompensa.PaseMensual;
            }
        }

        public bool EsPermanent
        {
            get
            {
                foreach (Recompensa r in recompenses)
                    if (r != null && r.tipus == TipusDeRecompensa.Permanent) return true;
                return false;
            }
        }
    }
}
