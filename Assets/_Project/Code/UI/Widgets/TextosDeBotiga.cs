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

        // El panell (i): tot el que dona i les condicions, línia a línia.
        public static string Detall(ProducteDeBotiga p)
        {
            if (p == null) return string.Empty;
            var linies = new List<string>();

            if (p.unaAlAtzar)
            {
                linies.Add(Textos.T("botiga.info.atzar"));
                float total = 0;
                foreach (Recompensa r in p.recompenses) if (r != null) total += UnityEngine.Mathf.Max(0, r.pes);
                foreach (Recompensa r in p.recompenses)
                    if (r != null && total > 0)
                        linies.Add($"• {Descripcio(r)}  ({FormatadorDeNombres.Factor(UnityEngine.Mathf.Max(0, r.pes) / total * 100)}%)");
            }
            else
            {
                if (p.recompenses.Count > 1) linies.Add(Textos.T("botiga.info.inclou"));
                foreach (Recompensa r in p.recompenses)
                {
                    if (r == null) continue;
                    linies.Add((p.recompenses.Count > 1 ? "• " : string.Empty) + Descripcio(r));
                    if (r.tipus == TipusDeRecompensa.Cofre && r.probabilitats != null && r.probabilitats.Length >= 4)
                    {
                        linies.Add(Textos.T("botiga.info.cofre"));
                        for (int i = 0; i < 4; i++)
                            if (r.probabilitats[i] > 0)
                                linies.Add($"• {ColorsDeRaresa.Nom((Raresa)i)}  {FormatadorDeNombres.Factor(r.probabilitats[i])}%");
                    }
                    if (r.tipus == TipusDeRecompensa.Permanent) linies.Add(Textos.T("botiga.info.permanent"));
                    if (r.tipus == TipusDeRecompensa.PaseMensual) linies.Add(Textos.T("botiga.info.pase"));
                }
            }

            linies.Add(string.Empty);
            if (p.doblePrimeraCompra) linies.Add(Textos.T("botiga.info.primera"));
            if (p.limit == 1) linies.Add(Textos.T("botiga.info.una_vegada"));
            else if (p.limit > 1) linies.Add(Textos.F("botiga.info.limit", p.limit));
            if (p.limitAlDia > 0) linies.Add(Textos.F("botiga.info.limit_dia", p.limitAlDia));
            if (p.horesEntreUsos > 0) linies.Add(Textos.F("botiga.info.cada", FormatadorDeNombres.TempsCurt(p.horesEntreUsos * 3600)));
            if (p.horesDOferta > 0) linies.Add(Textos.F("botiga.info.oferta", FormatadorDeNombres.TempsCurt(p.horesDOferta * 3600)));

            return string.Join("\n", linies).TrimEnd('\n');
        }

        public static string Nom(ProducteDeBotiga p)
        {
            if (p == null) return string.Empty;
            string t = p.nom.Text;
            return string.IsNullOrEmpty(t) ? p.name : t;
        }
    }
}
