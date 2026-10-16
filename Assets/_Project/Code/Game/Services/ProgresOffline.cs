using System;

namespace IdleFrogs.Game
{
    public struct ResultatOffline
    {
        public double segonsFora;        // el que ha passat de debò
        public double segonsComptats;    // després d'aplicar el tope
        public double produitAlsPantans;
        public double portatALEstacio;
        public double venut;             // el que ha entrat a la cartera

        public bool HiHaTope => segonsComptats < segonsFora;
        public bool HiHaAlgunGuany => produitAlsPantans > 0 || portatALEstacio > 0 || venut > 0;
    }

    // El que ha passat mentre el joc estava tancat. Només treballen els trams
    // automatitzats: sense encarregat ningú no toca les granotes.
    //
    // És pur a propòsit (ni Unity ni escena) perquè es pugui provar sol.
    //
    // Cada tram mou com a molt el seu cabal × temps, i mai més del que li ha
    // arribat. Això és exacte quan cada tram ha estat tota l'estona limitat o
    // pel seu cabal o pel que li arribava, que és el cas normal. Quan un tram
    // comença limitat per l'entrada i acaba limitat pel cabal dona una mica
    // més del real. S'accepta: simular viatge a viatge no val la pena.
    public static class ProgresOffline
    {
        public static ResultatOffline Calcular(
            double segonsFora,
            double segonsMaxims,
            double[] produccioPerPanta,   // doblers/s; 0 als que no van sols
            double[] capses,              // es modifica: com queden
            double cabalTren,             // 0 si el tren no va sol
            double estacioInicial,
            double cabalTenda,            // 0 si la tenda no va sola
            out double estacioFinal)
        {
            var r = new ResultatOffline();
            estacioFinal = estacioInicial < 0 ? 0 : estacioInicial;

            // Rellotge enrere (o sense marca): no hi ha res a donar.
            if (double.IsNaN(segonsFora) || segonsFora <= 0) return r;

            double t = segonsMaxims > 0 ? Math.Min(segonsFora, segonsMaxims) : segonsFora;
            r.segonsFora = segonsFora;
            r.segonsComptats = t;

            // 1. Pantans automatitzats → les seves capses
            int n = capses != null ? capses.Length : 0;
            for (int i = 0; i < n; i++)
            {
                double p = (produccioPerPanta != null && i < produccioPerPanta.Length)
                    ? produccioPerPanta[i] : 0;
                if (p <= 0 || double.IsNaN(p) || double.IsInfinity(p)) continue;

                double afegit = p * t;
                capses[i] += afegit;
                r.produitAlsPantans += afegit;
            }

            // 2. Tren → estació. Recull per ordre des del primer pantà, com
            //    fa de debò en el seu viatge d'anada.
            if (cabalTren > 0 && n > 0)
            {
                double disponible = 0;
                for (int i = 0; i < n; i++) disponible += Math.Max(0, capses[i]);

                double aMoure = Math.Min(cabalTren * t, disponible);
                double queda = aMoure;
                for (int i = 0; i < n && queda > 0; i++)
                {
                    double treu = Math.Min(Math.Max(0, capses[i]), queda);
                    capses[i] -= treu;
                    queda -= treu;
                }

                double mogut = aMoure - queda;
                estacioFinal += mogut;
                r.portatALEstacio = mogut;
            }

            // 3. Tenda → cartera
            if (cabalTenda > 0)
            {
                double venut = Math.Min(cabalTenda * t, estacioFinal);
                estacioFinal -= venut;
                r.venut = venut;
            }

            return r;
        }
    }
}
