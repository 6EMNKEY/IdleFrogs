using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Les compres amb diners de debò. Ara hi ha la de prova; quan hi hagi
    // comptes d'Apple i Google se'n fa una amb Unity IAP i no canvia res més.
    public interface IServeiDeCompres
    {
        // El preu en la moneda del jugador, o null si encara no se sap.
        string PreuLocalitzat(string idDeCompra);
        void Comprar(string idDeCompra, Action<bool> enAcabar);
        // Els ids no consumibles que el compte ja té comprats.
        void Restaurar(Action<string[]> enAcabar);
    }

    // Els anuncis amb recompensa. Ara el de prova.
    public interface IServeiDAnuncis
    {
        void Mostrar(Action<bool> enAcabar);
    }

    // Dona la compra per feta a l'editor i a les versions de prova. En una
    // versió final no compra res, perquè no s'hi pugui regalar res per error.
    public class CompresDeProva : IServeiDeCompres
    {
        public string PreuLocalitzat(string idDeCompra) => null;

        public void Comprar(string idDeCompra, Action<bool> enAcabar)
        {
            bool fet = Application.isEditor || Debug.isDebugBuild;
            Debug.Log(fet ? $"Botiga: compra de prova {idDeCompra}" : $"Botiga: sense compres reals encara ({idDeCompra})");
            enAcabar?.Invoke(fet);
        }

        public void Restaurar(Action<string[]> enAcabar)
        {
            Debug.Log("Botiga: restaurar compres (de prova, no hi ha res a restaurar)");
            enAcabar?.Invoke(new string[0]);
        }
    }

    public class AnuncisDeProva : IServeiDAnuncis
    {
        public void Mostrar(Action<bool> enAcabar)
        {
            Debug.Log("Botiga: anunci de prova vist");
            enAcabar?.Invoke(true);
        }
    }
}
