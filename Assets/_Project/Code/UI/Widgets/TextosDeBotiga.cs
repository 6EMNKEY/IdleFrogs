using System.Collections.Generic;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Com es diu cada cosa de la botiga: "500 gemes", "x2 durant 4h",
    // "4h d'ingressos"... Viu a la UI perquè fa servir les icones del text.
    public static class TextosDeBotiga
    {
        public static string Descripcio(Recompensa r)
        {
            if (r == null) return string.Empty;
            switch (r.tipus)
            {
                case TipusDeRecompensa.Gemes:
                    return Textos.F("rec.gemes", FormatadorDeNombres.Doblers(r.quantitat), Icones.Gema);
                case TipusDeRecompensa.TempsDIngressos:
                    return Textos.F("rec.ingressos", FormatadorDeNombres.TempsCurt(r.quantitat * 60));
                case TipusDeRecompensa.Multiplicador:
                    return Textos.F("rec.multiplicador", FormatadorDeNombres.Factor(r.multiplicador), FormatadorDeNombres.TempsCurt(r.minuts * 60));
                case TipusDeRecompensa.Encarregat:
                    return Textos.F("rec.encarregat", ColorsDeRaresa.Nom(r.raresa));
                case TipusDeRecompensa.Cofre:
                    return Textos.T("rec.cofre");
                case TipusDeRecompensa.Permanent:
                    return Textos.T(r.efectePermanent switch
                    {
                        EfectePermanent.X2Ingressos => "rec.perm.x2",
                        EfectePermanent.SenseAnuncis => "rec.perm.anuncis",
                        _ => "rec.perm.offline"
                    });
                case TipusDeRecompensa.PaseMensual:
                    return Textos.F("rec.pase", FormatadorDeNombres.Doblers(r.quantitat), Icones.Gema, r.dies);
            }
            return string.Empty;
        }

        public static string Descripcio(IEnumerable<Recompensa> recompenses, string separador = "\n")
        {
            var parts = new List<string>();
            if (recompenses != null)
                foreach (Recompensa r in recompenses)
                    if (r != null) parts.Add(Descripcio(r));
            return string.Join(separador, parts);
        }

        // El que diu la targeta sota el nom.
        public static string Contingut(ProducteDeBotiga p)
        {
            if (p == null) return string.Empty;
            if (p.unaAlAtzar) return Textos.T("botiga.a_latzar");

            string text = Descripcio(p.recompenses);
            // Les probabilitats dels cofres s'han d'ensenyar (Apple ho exigeix).
            foreach (Recompensa r in p.recompenses)
                if (r != null && r.tipus == TipusDeRecompensa.Cofre && r.probabilitats != null && r.probabilitats.Length >= 4)
                    text += "\n" + Textos.F("rec.probabilitats",
                        FormatadorDeNombres.Factor(r.probabilitats[0]), FormatadorDeNombres.Factor(r.probabilitats[1]),
                        FormatadorDeNombres.Factor(r.probabilitats[2]), FormatadorDeNombres.Factor(r.probabilitats[3]));
            return text;
        }

        public static string Nom(ProducteDeBotiga p)
        {
            if (p == null) return string.Empty;
            string t = p.nom.Text;
            return string.IsNullOrEmpty(t) ? p.name : t;
        }
    }
}
