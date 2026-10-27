using System;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Comprar amb monedes quan no n'hi ha prou: en comptes de no fer res,
    // surt la finestra de confirmació amb el que falta i el botó d'acceptar
    // amb les gemes que costa (al preu de la botiga: 1 h d'ingressos = 60).
    // Si tampoc hi ha gemes, acceptar obre la botiga a Gemes.
    public static class CompraAmbGemes
    {
        // comprar: la compra de debò, que gasta 'preu' monedes. Es crida tal
        // qual si n'hi ha prou, o després de posar-hi el que falta amb gemes.
        // titol: l'acció ("Millorar"); explicacio: la frase sencera amb el
        // preu en gemes que li passem ("Millorar Pantà 3 per 25 gemes").
        public static void Intentar(double preu, string titol, Func<string, string> explicacio, Action comprar)
        {
            CurrencyManager c = CurrencyManager.Instancia;
            if (c == null || comprar == null) return;
            if (c.doblersActuals >= preu)
            {
                comprar();
                return;
            }

            double falta = preu - c.doblersActuals;
            int gemes = GestorDeBotiga.Instancia != null ? GestorDeBotiga.Instancia.GemesPer(falta) : 0;
            if (gemes <= 0)
            {
                // No es pot pagar amb gemes (sense ritme o massa car): només s'avisa.
                FinestraDeConfirmacio.Preguntar(titol, Textos.F("conf.falten", FormatadorDeNombres.Doblers(falta) + Icones.Moneda), null);
                return;
            }

            string enGemes = FormatadorDeNombres.Doblers(gemes) + Icones.Gema;
            FinestraDeConfirmacio.Preguntar(
                titol,
                explicacio != null ? explicacio(enGemes) : enGemes,
                () => PagarAmbGemes(preu, gemes, comprar),
                textAcceptar: enGemes);
        }

        private static void PagarAmbGemes(double preu, int gemes, Action comprar)
        {
            CurrencyManager c = CurrencyManager.Instancia;
            if (c == null) return;

            // Entre preguntar i acceptar els diners poden haver pujat.
            if (c.doblersActuals >= preu) { comprar(); return; }

            if (!c.GastarGemes(gemes))
            {
                if (PanellDeBotiga.Instancia != null) PanellDeBotiga.Instancia.ObrirA(SeccioDeBotiga.Gemes);
                return;
            }
            // Una mica de marge: amb decimals, posar just el que falta podria quedar curt.
            c.AfegirDoblers(preu - c.doblersActuals + preu * 1e-9);
            comprar();
        }

        // Un botó que sempre es pot tocar però es veu apagat (el seu Disabled
        // Color) quan no arriben les monedes.
        public static void Pintar(Button boto, bool arriba)
        {
            if (boto == null) return;
            ColorBlock colors = boto.colors;
            Color x = arriba ? Color.white : colors.disabledColor;
            colors.normalColor = x;
            colors.highlightedColor = x;
            colors.selectedColor = x;
            colors.pressedColor = new Color(x.r * 0.85f, x.g * 0.85f, x.b * 0.85f, x.a);
            boto.colors = colors;
        }
    }
}
