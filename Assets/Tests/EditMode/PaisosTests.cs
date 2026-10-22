using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class PaisosTests
    {
        private static DadesDePartida AmbZona0()
        {
            var d = new DadesDePartida();
            d.Zona(0).nivellsDePanta = new[] { 3, 0 };
            return d;
        }

        // Una partida d'abans dels països: els doblers, el rellotge i l'offline
        // pendent eren globals i passen a la zona 0.
        [Test]
        public void PartidaVella_PassaAlPrimerPais()
        {
            DadesDePartida d = AmbZona0();
            d.doblers = 860;
            d.marcaDeTempsUtc = 123;
            d.offlinePendent = RecompensaOffline.Calcular(100, 7200, 2);

            MigracioDePartida.PassarAPaisos(d);

            Assert.AreEqual(860, d.zones[0].doblers, 1e-9);
            Assert.AreEqual(123, d.zones[0].marcaDeTempsUtc);
            Assert.AreEqual(200, d.zones[0].offlinePendent.doblers, 1e-9);
            Assert.AreEqual(0, d.doblers, 1e-9);
            Assert.IsFalse(d.offlinePendent.HiHaRecompensa);
        }

        // Si ja s'havia passat, no trepitja el que hi ha.
        [Test]
        public void JaPassada_NoFaRes()
        {
            DadesDePartida d = AmbZona0();
            d.zones[0].doblers = 50;
            MigracioDePartida.PassarAPaisos(d);
            Assert.AreEqual(50, d.zones[0].doblers, 1e-9);
        }

        [Test]
        public void PartidaVella_TeElPrimerPaisObert()
        {
            DadesDePartida d = JsonUtility.FromJson<DadesDePartida>("{\"versio\":4,\"zones\":[]}");
            Assert.AreEqual(1, d.paisosDesbloquejats);
        }

        // Cada país guarda la seva moneda i el seu rellotge.
        [Test]
        public void CadaZona_GuardaElsSeusDoblers()
        {
            DadesDePartida d = AmbZona0();
            d.zones[0].doblers = 10;
            d.Zona(1).nivellsDePanta = new[] { 1 };
            d.zones[1].doblers = 999;
            d.zones[1].marcaDeTempsUtc = 42;
            d.paisosDesbloquejats = 2;

            var tornada = JsonUtility.FromJson<DadesDePartida>(JsonUtility.ToJson(d));
            Assert.AreEqual(10, tornada.zones[0].doblers, 1e-9);
            Assert.AreEqual(999, tornada.zones[1].doblers, 1e-9);
            Assert.AreEqual(42, tornada.zones[1].marcaDeTempsUtc);
            Assert.AreEqual(2, tornada.paisosDesbloquejats);
        }
    }
}
