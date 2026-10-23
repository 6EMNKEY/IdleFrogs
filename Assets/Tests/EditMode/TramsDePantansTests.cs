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

        // Cada barrera ocupa el lloc d'un pantà: els de darrere produeixen i
        // costen com si aquell pantà hi fos.
        [TestCase(0, 0)]
        [TestCase(4, 4)]
        [TestCase(5, 6)]
        [TestCase(9, 10)]
        [TestCase(10, 12)]
        [TestCase(13, 15)]
        [TestCase(14, 17)]
        [TestCase(22, 27)]
        [TestCase(25, 30)]
        public void PosicioDelPanta(int index, int esperat) =>
            Assert.AreEqual(esperat, TramsDePantans.PosicioDelPanta(Trams, index, 5));

        // La barrera costa el que costaria el pantà que es salta: just entre
        // l'últim del tram i el primer del següent.
        [TestCase(0, 5)]
        [TestCase(1, 11)]
        [TestCase(2, 16)]
        [TestCase(3, 21)]
        [TestCase(4, 26)]
        public void PosicioDeLaBarrera(int barrera, int esperat)
        {
            Assert.AreEqual(esperat, TramsDePantans.PosicioDeLaBarrera(Trams, barrera));
            int ultim = TramsDePantans.UltimDelTram(Trams, barrera);
            Assert.AreEqual(esperat - 1, TramsDePantans.PosicioDelPanta(Trams, ultim, 5));
            Assert.AreEqual(esperat + 1, TramsDePantans.PosicioDelPanta(Trams, ultim + 1, 5));
        }

        // Els preus per defecte: 10, 1.000, 30.000 i ×20 a partir d'aquí.
        private static double PreuBase(int posicio) =>
            posicio == 0 ? 10 : posicio == 1 ? 1000 : 30000 * System.Math.Pow(20, posicio - 2);

        private static double Preu(int index) =>
            PreuBase(TramsDePantans.PosicioDelPanta(Trams, index, 5)) *
            TramsDePantans.FactorDePreu(Trams, index, 5, PreuBase, 1.5);

        private static double PreuDeBarrera(int barrera) =>
            PreuBase(TramsDePantans.PosicioDeLaBarrera(Trams, barrera)) *
            TramsDePantans.FactorDePreuDeLaBarrera(Trams, barrera, PreuBase, 1.5);

        // La barrera costa el pantà que es salta; el de darrere, només ×1,5;
        // i a partir d'aquí, els ×20 de sempre.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void DespresDeLaBarrera_SaltPetit(int barrera)
        {
            int primer = TramsDePantans.UltimDelTram(Trams, barrera) + 1;
            Assert.AreEqual(1.5, Preu(primer) / PreuDeBarrera(barrera), 1e-9);
            Assert.AreEqual(20, Preu(primer + 1) / Preu(primer), 1e-9);
            Assert.AreEqual(20, PreuDeBarrera(barrera) / Preu(primer - 1), 1e-9);
        }

        [Test]
        public void AbansDeLaPrimeraBarrera_PreuDeSempre()
        {
            for (int i = 0; i <= 4; i++) Assert.AreEqual(PreuBase(i), Preu(i), 1e-9);
            Assert.AreEqual(PreuBase(5), PreuDeBarrera(0), 1e-9);
        }

        [Test]
        public void SenseBarreres_LaPosicioEsLIndex() =>
            Assert.AreEqual(12, TramsDePantans.PosicioDelPanta(Trams, 12, 0));

        [Test]
        public void SenseTrams_NoHiHaRes()
        {
            Assert.AreEqual(-1, TramsDePantans.UltimDelTram(null, 0));
            Assert.AreEqual(0, TramsDePantans.Total(new int[0]));
        }
    }
}
