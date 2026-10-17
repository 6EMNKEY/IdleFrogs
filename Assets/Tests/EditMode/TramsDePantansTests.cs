using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class TramsDePantansTests
    {
        private static readonly int[] Trams = { 5, 5, 4, 4, 4, 4 };

        // La barrera n va just després d'aquest pantà (índex des de 0).
        [TestCase(0, 4)]
        [TestCase(1, 9)]
        [TestCase(2, 13)]
        [TestCase(3, 17)]
        [TestCase(4, 21)]
        [TestCase(5, 25)]
        public void UltimDelTram(int tram, int esperat) =>
            Assert.AreEqual(esperat, TramsDePantans.UltimDelTram(Trams, tram));

        [Test]
        public void ElTotalSonElsVintISisPantans() => Assert.AreEqual(26, TramsDePantans.Total(Trams));

        [Test]
        public void UnTramQueNoExisteix_DonaElDarrer() =>
            Assert.AreEqual(25, TramsDePantans.UltimDelTram(Trams, 9));

        [Test]
        public void SenseTrams_NoHiHaRes()
        {
            Assert.AreEqual(-1, TramsDePantans.UltimDelTram(null, 0));
            Assert.AreEqual(0, TramsDePantans.Total(new int[0]));
        }
    }
}
