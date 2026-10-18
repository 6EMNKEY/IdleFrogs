using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class RecompensaOfflineTests
    {
        [Test]
        public void IdlePerTemps()
        {
            var r = RecompensaOffline.Calcular(3600, 7200, 10);
            Assert.AreEqual(36000, r.doblers, 1e-9);
            Assert.AreEqual(3600, r.segonsComptats, 1e-9);
            Assert.IsFalse(r.HiHaTope);
        }

        [Test]
        public void AmbTope_NomesCompten_LesHoresMaximes()
        {
            var r = RecompensaOffline.Calcular(50000, 7200, 10);
            Assert.AreEqual(72000, r.doblers, 1e-9);
            Assert.AreEqual(50000, r.segonsFora, 1e-9);
            Assert.IsTrue(r.HiHaTope);
        }

        [Test]
        public void SenseIdle_NoHiHaRes() =>
            Assert.IsFalse(RecompensaOffline.Calcular(3600, 7200, 0).HiHaRecompensa);

        // Dues absències sense acceptar: es sumen, i segons × ritme encara
        // dona el total.
        [Test]
        public void EsSumen_IElRitmeQuadra()
        {
            var r = RecompensaOffline.Calcular(100, 7200, 10).Sumar(RecompensaOffline.Calcular(300, 7200, 30));
            Assert.AreEqual(100 * 10 + 300 * 30, r.doblers, 1e-9);
            Assert.AreEqual(r.doblers, r.segonsComptats * r.PerSegon, 1e-6);
        }

        // Es guarda amb la partida: JsonUtility l'ha de saber escriure i llegir.
        [Test]
        public void SobreviuAlGuardat()
        {
            var dades = new DadesDePartida { offlinePendent = RecompensaOffline.Calcular(3600, 7200, 10) };
            var llegides = JsonUtility.FromJson<DadesDePartida>(JsonUtility.ToJson(dades));
            Assert.AreEqual(36000, llegides.offlinePendent.doblers, 1e-9);
        }
    }
}
