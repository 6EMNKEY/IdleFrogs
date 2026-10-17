using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class PlaDeLlenguadesTests
    {
        private readonly Llenguada[] _pla = new Llenguada[4];

        // Una recol·lecció normal de 4 s amb dues llenguades de 0,45 s:
        // acaben als 2 s i als 4 s.
        [Test]
        public void LaDarrera_AcabaAmbLaRecoleccio()
        {
            int n = PlaDeLlenguades.Calcular(4f, 2, 0.45f, 0.2f, _pla);
            Assert.AreEqual(2, n);
            Assert.AreEqual(1.55f, _pla[0].inici, 1e-5f);
            Assert.AreEqual(2f, _pla[0].inici + _pla[0].durada, 1e-5f);
            Assert.AreEqual(4f, _pla[1].inici + _pla[1].durada, 1e-5f);
            Assert.AreEqual(0.45f, _pla[1].durada, 1e-5f);
        }

        [Test]
        public void TresIgualDeRepartides()
        {
            int n = PlaDeLlenguades.Calcular(3f, 3, 0.45f, 0.2f, _pla);
            Assert.AreEqual(3, n);
            for (int k = 0; k < 3; k++)
                Assert.AreEqual(k + 1f, _pla[k].inici + _pla[k].durada, 1e-5f);
        }

        // Si el tram és més curt que la llenguada, s'accelera: dura tot el tram.
        [Test]
        public void AmbPocTemps_SAccelera()
        {
            int n = PlaDeLlenguades.Calcular(0.6f, 2, 0.45f, 0.2f, _pla);
            Assert.AreEqual(2, n);
            Assert.AreEqual(0.3f, _pla[0].durada, 1e-5f);
            Assert.AreEqual(0f, _pla[0].inici, 1e-5f);
        }

        // Massa ràpid per a totes: se'n fan menys abans que siguin un parpelleig.
        [Test]
        public void MoltRapid_SenFanMenys()
        {
            Assert.AreEqual(1, PlaDeLlenguades.Calcular(0.3f, 3, 0.45f, 0.2f, _pla));
            Assert.AreEqual(0.3f, _pla[0].durada, 1e-5f);
        }

        [Test]
        public void SiNiUnaHiCap_Cap() =>
            Assert.AreEqual(0, PlaDeLlenguades.Calcular(0.1f, 2, 0.45f, 0.2f, _pla));

        [Test]
        public void SenseTemps_Cap() =>
            Assert.AreEqual(0, PlaDeLlenguades.Calcular(0f, 2, 0.45f, 0.2f, _pla));
    }
}
