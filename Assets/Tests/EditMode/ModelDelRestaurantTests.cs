using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class ModelDelRestaurantTests
    {
        private static readonly int[] Fites = { 10, 25, 50 };

        [TestCase(0, 0)]
        [TestCase(1, 4)]
        [TestCase(9, 4)]
        [TestCase(10, 8)]
        [TestCase(25, 12)]
        [TestCase(200, 16)]
        public void ClientsPerNivell(int nivell, int esperat) =>
            Assert.AreEqual(esperat, ModelDelRestaurant.Clients(nivell, 4, 4, Fites));

        [TestCase(4, 20, 4)]
        [TestCase(60, 20, 20)]
        [TestCase(0, 20, 0)]
        public void Visibles(int clients, int maxim, int esperat) =>
            Assert.AreEqual(esperat, ModelDelRestaurant.Visibles(clients, maxim));

        [Test]
        public void PerSobreDelMaxim_CadascunEnRepresentaUnsQuants() =>
            Assert.AreEqual(3, ModelDelRestaurant.Representa(60, 20), 1e-9);

        // La taula: 4 clients de 250, 4 s menjant i 14 s caminant = 55,6/s,
        // el mateix que el transportista de 1.000.
        [Test]
        public void QuatreClients_IgualQueElTransportista() =>
            Assert.AreEqual(1000 / 18.0, ModelDelRestaurant.Venda(4, 250, 4, 7, 7, 1), 1e-9);

        [Test]
        public void SenseVelocitat_NoVenen() =>
            Assert.AreEqual(0, ModelDelRestaurant.Venda(4, 250, 4, 7, 7, 0), 1e-9);

        [Test]
        public void RepartitsAlLlargDelCicle()
        {
            Assert.AreEqual(0f, ModelDelRestaurant.RetardDeSortida(0, 4, 18f), 1e-5f);
            Assert.AreEqual(4.5f, ModelDelRestaurant.RetardDeSortida(1, 4, 18f), 1e-5f);
            Assert.AreEqual(13.5f, ModelDelRestaurant.RetardDeSortida(3, 4, 18f), 1e-5f);
        }
    }
}
