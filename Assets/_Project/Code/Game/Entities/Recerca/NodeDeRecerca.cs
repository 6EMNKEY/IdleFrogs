using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Què millora un node. Tots se sumen per tipus: el valor per nivell × el
    // nivell, de tots els nodes del tipus que toquen el país. Els que són
    // factors (ingressos, velocitat...) valen 1 + la suma; els que són
    // quantitats (minuts, refrescos) valen la suma.
    public enum TipusDEfecteDeRecerca
    {
        Ingressos,              // factor dels ingressos
        ProduccioDePantans,     // factor de la producció dels pantans
        VelocitatDelTren,       // factor
        CapacitatDelRestaurant, // factor
        CostDeMillores,         // factor (valor negatiu: −0,03 = −3 %)
        DanyDeLluitadors,       // factor
        PuntsDeBosses,          // factor
        IngressosOffline,       // factor
        DuracioDHabilitats,     // factor
        RecarregaDHabilitats,   // factor (negatiu)
        TempsDeBarreres,        // factor (negatiu)
        GemesDeFites,           // factor
        AnunciBarrera,          // minuts de més que treu cada vídeo
        AnunciMosca,            // factor de la recompensa de la mosca daurada
        AnunciOffline,          // se suma al x2 del vídeo de l'offline
        AnunciEtFalten,         // minuts de més per pagar "Et falten X" amb vídeo
        AnunciRegal,            // se suma al x2 del vídeo del calendari i del regal
        AnunciPotenciador,      // minuts de més dels potenciadors per vídeo
        RefrescosGratis,        // refrescos gratis de més al dia a l'arena
        SortAlGacha,            // fa més probables les rareses altes
    }

    // El que costa un nivell d'un node en punts d'un color (país).
    [Serializable]
    public class CostDeRecerca
    {
        [Tooltip("El color: la zona del país (0 = el primer)")]
        [Min(0)] public int pais;
        [Tooltip("El que costa el primer nivell")]
        [Min(0)] public double costBase = 5;
        [Tooltip("Cada nivell costa això vegades l'anterior")]
        [Min(1)] public double creixement = 1.5;

        public double AlNivell(int nivellActual) => Math.Ceiling(costBase * Math.Pow(creixement, Math.Max(0, nivellActual)));
    }

    [Serializable]
    public class NodeDeRecerca
    {
        [Tooltip("Estable: el guardat hi apunta")]
        public string id;
        public TextTraduit nom;
        [Tooltip("Què fa. {0} = el valor total ara, {1} = el del nivell següent")]
        public TextTraduit descripcio;
        public Sprite icona;
        [Tooltip("On surt a l'arbre, en píxels des del centre")]
        public Vector2 posicio;
        [Tooltip("Els nodes que han de tenir com a mínim un nivell abans")]
        public string[] prerequisits = new string[0];
        [Min(1)] public int nivellMaxim = 5;
        [Tooltip("Un per color; un node de diversos colors els demana tots")]
        public CostDeRecerca[] cost = new CostDeRecerca[0];

        [Header("Efecte")]
        public TipusDEfecteDeRecerca efecte;
        [Tooltip("La zona del país on fa efecte; -1 = a tots")]
        public int pais = -1;
        [Tooltip("Per nivell: 0,1 = +10 %; −0,03 = −3 %; 10 = 10 minuts")]
        public double valorPerNivell = 0.1;

        public bool TocaElPais(int zona) => pais < 0 || pais == zona;
    }
}
