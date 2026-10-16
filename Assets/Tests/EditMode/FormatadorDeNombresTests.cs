using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class FormatadorDeNombresTests
    {
        [TestCase(0, "0")]
        [TestCase(999, "999")]
        [TestCase(1000, "1.00K")]
        [TestCase(1500, "1.50K")]
        [TestCase(1.234e6, "1.23M")]
        [TestCase(5e9, "5.00B")]
        [TestCase(7.5e12, "7.50T")]
        [TestCase(3e15, "3.00aa")]
        [TestCase(3e18, "3.00ab")]
        [TestCase(-1500, "-1.50K")]
        public void Doblers(double valor, string esperat) =>
            Assert.AreEqual(esperat, FormatadorDeNombres.Doblers(valor));

        // El punt decimal ha de ser el mateix en tots els mòbils, sigui quin
        // sigui l'idioma del sistema.
        [Test]
        public void NoDepenDeLIdiomaDelSistema()
        {
            var abans = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-ES");
                Assert.AreEqual("1.50K", FormatadorDeNombres.Doblers(1500));
                Assert.AreEqual("2.5", FormatadorDeNombres.Decimals(2.5, 1));
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = abans; }
        }

        [Test]
        public void ValorsRars_NoPeten()
        {
            Assert.AreEqual("0", FormatadorDeNombres.Doblers(double.NaN));
            Assert.AreEqual("0", FormatadorDeNombres.Doblers(double.PositiveInfinity));
            Assert.Less(FormatadorDeNombres.Doblers(1e300).Length, 12);
        }

        [TestCase(0, "+0")]
        [TestCase(-5, "+0")]
        [TestCase(1500, "+1.50K")]
        public void Delta(double valor, string esperat) =>
            Assert.AreEqual(esperat, FormatadorDeNombres.Delta(valor));

        [TestCase(45, "45s")]
        [TestCase(300, "5m 0s")]
        [TestCase(3700, "1h 1m")]
        [TestCase(90000, "1d 1h")]
        public void Temps(double segons, string esperat) =>
            Assert.AreEqual(esperat, FormatadorDeNombres.Temps(segons));

        [TestCase(45, "45s")]
        [TestCase(180, "3m")]
        [TestCase(90, "1m 30s")]
        [TestCase(3600, "1h")]
        [TestCase(5400, "1h 30m")]
        [TestCase(86400, "1d")]
        public void TempsCurt(double segons, string esperat) =>
            Assert.AreEqual(esperat, FormatadorDeNombres.TempsCurt(segons));

        [TestCase(5, "5")]
        [TestCase(2.5, "2.5")]
        [TestCase(40, "40")]
        public void Factor(double valor, string esperat) =>
            Assert.AreEqual(esperat, FormatadorDeNombres.Factor(valor));
    }
}
