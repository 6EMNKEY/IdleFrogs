using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class IntensitatDeRecompensaTests
    {
        [TestCase(1000, 500, 1f)]        // més del que tenies: el màxim
        [TestCase(500, 500, 1f)]         // igual: el màxim
        [TestCase(250, 1000, 0.5f)]      // un 25%: a mig camí
        [TestCase(10, 1000000, 0.00316f)] // res comparat amb el que tens
        [TestCase(5, 0, 1f)]             // no tenies res
        [TestCase(0, 1000, 0f)]
        public void SegonsElQueTenies(double recompensa, double tenia, float esperat) =>
            Assert.AreEqual(esperat, IntensitatDeRecompensa.Calcular(recompensa, tenia), 1e-4f);

        [Test]
        public void UnaCompra_SempreElMaxim() =>
            Assert.AreEqual(1f, IntensitatDeRecompensa.Calcular(1, 1e12, esCompra: true));
    }
}
