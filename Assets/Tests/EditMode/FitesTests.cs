using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class FitesTests
    {
        private static readonly FitaDeMultiplicador[] DuesFites =
        {
            new FitaDeMultiplicador { nivell = 25, multiplicador = 2.1 },
            new FitaDeMultiplicador { nivell = 50, multiplicador = 2.1 },
        };

        [TestCase(1, 1.0)]
        [TestCase(24, 1.0)]
        [TestCase(25, 2.1)]
        [TestCase(49, 2.1)]
        [TestCase(50, 4.41)]
        [TestCase(800, 4.41)]
        public void Acumulat(int nivell, double esperat) =>
            Assert.AreEqual(esperat, Fites.Acumulat(DuesFites, nivell), 1e-9);

        [Test]
        public void SenseFites_EsU() => Assert.AreEqual(1, Fites.Acumulat(null, 100));

        // Amb creixement 1.33 la capacitat del tren es passa de rang cap al
        // nivell 2470: el tope de 2000 ha de quedar per sota.
        [Test]
        public void NivellMaximSegur_DelTren()
        {
            int segur = Fites.NivellMaximSegur(50, 1.33, 2.66 * 1.21);
            Assert.That(segur, Is.InRange(2400, 2500));
            Assert.Greater(segur, 2000);
        }

        [Test]
        public void NivellMaximSegur_SenseCreixement_NoTeLimit() =>
            Assert.AreEqual(int.MaxValue, Fites.NivellMaximSegur(50, 1.0));

        // ---- La barra del panell i les gemes ----

        private static readonly FitaDeMultiplicador[] AmbGemes =
        {
            // Desordenades a propòsit: l'array s'edita a mà.
            new FitaDeMultiplicador { nivell = 50, gemes = 30 },
            new FitaDeMultiplicador { nivell = 25, gemes = 20 },
            new FitaDeMultiplicador { nivell = 100, gemes = 50 },
        };

        [TestCase(1, 25)]
        [TestCase(24, 25)]
        [TestCase(25, 50)]
        [TestCase(99, 100)]
        public void Seguent(int nivell, int esperada) =>
            Assert.AreEqual(esperada, Fites.Seguent(AmbGemes, nivell).nivell);

        [Test]
        public void Seguent_QuanJaLesTens_EsNull()
        {
            Assert.IsNull(Fites.Seguent(AmbGemes, 100));
            Assert.IsNull(Fites.Seguent(null, 1));
        }

        [TestCase(1, 0f)]
        [TestCase(13, 0.5f)]
        [TestCase(25, 0f)]       // acabes de passar-ne una: la barra torna a zero
        [TestCase(40, 0.6f)]
        [TestCase(75, 0.5f)]
        [TestCase(100, 1f)]      // ja no en queden: plena
        [TestCase(500, 1f)]
        public void Progres(int nivell, float esperat) =>
            Assert.AreEqual(esperat, Fites.Progres(AmbGemes, nivell), 1e-5f);

        [TestCase(1, 24, 0)]
        [TestCase(24, 25, 20)]
        [TestCase(25, 26, 0)]    // la del 25 ja la tenies
        [TestCase(20, 70, 50)]   // ×50 en creua dues
        [TestCase(1, 1000, 100)]
        public void GemesEntre(int desde, int fins, int esperat) =>
            Assert.AreEqual(esperat, Fites.GemesEntre(AmbGemes, desde, fins));
    }
}
