using System.Text.RegularExpressions;
using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class TextosTests
    {
        [TearDown]
        public void Netejar() => Textos.EstablirPerAProves(Idioma.English);

        // Cap clau sense traduir, i les mateixes {0}, {1}... a tots els idiomes:
        // si en falta una, string.Format petaria només en aquell idioma.
        [Test]
        public void TotesLesClaus_TenenElsTresIdiomes_AmbElsMateixosForats()
        {
            foreach (var parella in Textos.Totes)
            {
                Assert.AreEqual(3, parella.Value.Length, parella.Key);
                string forats = Forats(parella.Value[0]);
                for (int i = 0; i < 3; i++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(parella.Value[i]), $"{parella.Key} [{i}]");
                    Assert.AreEqual(forats, Forats(parella.Value[i]), $"{parella.Key} [{i}]");
                }
            }
        }

        [Test]
        public void SegonsLIdioma()
        {
            Textos.EstablirPerAProves(Idioma.English);
            Assert.AreEqual("Swamp 3", Textos.F("unitat.panta", 3));
            Textos.EstablirPerAProves(Idioma.Espanol);
            Assert.AreEqual("Pantano 3", Textos.F("unitat.panta", 3));
            Textos.EstablirPerAProves(Idioma.Catala);
            Assert.AreEqual("Pantà 3", Textos.F("unitat.panta", 3));
        }

        [Test]
        public void ClauQueNoExisteix_EsVeuLaClau() =>
            Assert.AreEqual("no.existeix", Textos.T("no.existeix"));

        [Test]
        public void TextTraduit_SiFaltaLIdioma_LAngles()
        {
            var t = new TextTraduit { angles = "Golden World", catala = "El Món d'Or" };
            Textos.EstablirPerAProves(Idioma.Espanol);
            Assert.AreEqual("Golden World", t.Text);
            Textos.EstablirPerAProves(Idioma.Catala);
            Assert.AreEqual("El Món d'Or", t.Text);
        }

        private static string Forats(string s)
        {
            var m = Regex.Matches(s, @"\{\d+\}");
            var llista = new System.Collections.Generic.List<string>();
            foreach (Match x in m) llista.Add(x.Value);
            llista.Sort();
            return string.Join(",", llista);
        }
    }
}
