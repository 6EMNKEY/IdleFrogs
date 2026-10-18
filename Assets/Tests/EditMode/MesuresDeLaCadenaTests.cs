using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class MesuresDeLaCadenaTests
    {
        [Test]
        public void ElMesPetit_EsElCollDAmpolla()
        {
            var m = new MesuresDeLaCadena(30, 25, 100);
            Assert.AreEqual(25, m.CollDAmpolla, 1e-9);
            Assert.AreEqual(TramDeLaCadena.Tren, m.QuinTram);
        }

        [TestCase(10, 50, 50, TramDeLaCadena.Pantans)]
        [TestCase(50, 50, 10, TramDeLaCadena.Tenda)]
        [TestCase(20, 20, 20, TramDeLaCadena.Pantans)]   // empat: el primer de la cadena
        public void QuinTram(double pantans, double tren, double tenda, TramDeLaCadena esperat) =>
            Assert.AreEqual(esperat, new MesuresDeLaCadena(pantans, tren, tenda).QuinTram);

        [Test]
        public void LIdle_EsUnaDesenaDelColl() =>
            Assert.AreEqual(2.5, new MesuresDeLaCadena(30, 25, 100).Idle, 1e-9);

        [Test]
        public void SenseTenda_IdleZero() =>
            Assert.AreEqual(0, new MesuresDeLaCadena(30, 25, 0).Idle, 1e-9);

        // Recull 20 en 4 s, 1 s de camí cada sentit (2 unitats a velocitat 2),
        // 0,2 s per buidar: 20 / 6,2.
        [Test]
        public void UnaGranota_CompteAnadaITornada() =>
            Assert.AreEqual(20 / 6.2, CicleDeGranota.Produccio(20, 4, 0.2, 2, 2), 1e-9);

        [Test]
        public void EsperesMinimes_DeLaCorrutina() =>
            Assert.AreEqual(20 / 0.04, CicleDeGranota.Produccio(20, 0, 0, 0, 2), 1e-9);

        [Test]
        public void SenseVelocitat_NoArribaMai() =>
            Assert.AreEqual(0, CicleDeGranota.Produccio(20, 4, 0.2, 2, 0), 1e-9);
    }
}
