using System;

namespace IdleFrogs.Game
{
    // Els números del restaurant, sense res de Unity per poder-los provar.
    //
    // Cada client fa un cicle: arriba des de baix, entra, menja, paga en
    // acabar i se'n torna. Un antic transportista equival a uns quants
    // clients que carreguen una part cadascun; el total és el mateix.
    public static class ModelDelRestaurant
    {
        // Clients reals al nivell n: els inicials, i a cada nivell de la
        // llista se n'hi afegeixen 'perFita' (com les granotes del pantà).
        public static int Clients(int nivell, int inicials, int perFita, int[] nivellsExtra)
        {
            if (nivell < 1) return 0;
            int total = Math.Max(0, inicials);
            if (nivellsExtra != null)
                foreach (int fita in nivellsExtra)
                    if (nivell >= fita) total += Math.Max(0, perFita);
            return total;
        }

        // Els que es dibuixen: tots fins al màxim. Per sobre, cadascun en
        // representa uns quants.
        public static int Visibles(int clients, int maxim) =>
            Math.Max(0, Math.Min(clients, Math.Max(0, maxim)));

        // A quants clients reals paga cada client dibuixat.
        public static double Representa(int clients, int visibles) =>
            visibles > 0 ? (double)clients / visibles : 0;

        // Doblers per segon amb tots els clients fent cicles sencers.
        public static double Venda(int clients, double perClient, double segonsMenjant,
                                   double distanciaDAnada, double distanciaDeTornada, double velocitat)
        {
            if (clients <= 0 || perClient <= 0 || velocitat <= 0) return 0;
            double cicle = Math.Max(0.02, segonsMenjant) + (distanciaDAnada + distanciaDeTornada) / velocitat;
            return clients * perClient / cicle;
        }

        // Quan surt cada client dibuixat la primera vegada: repartits al
        // llarg d'un cicle, perquè sempre n'hi hagi d'anada i de tornada.
        public static float RetardDeSortida(int index, int visibles, float segonsDelCicle) =>
            visibles > 0 && segonsDelCicle > 0 ? segonsDelCicle * index / visibles : 0f;
    }
}
