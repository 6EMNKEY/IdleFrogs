using System;
using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class ProgresOfflineTests
    {
        private const double H = 3600;

        private static ResultatOffline Calcular(double segons, double[] produccio, double[] capses,
                                                double cabalTren, double estacio, double cabalTenda,
                                                out double estacioFinal, double tope = 2 * H) =>
            ProgresOffline.Calcular(segons, tope, produccio, capses, cabalTren, estacio, cabalTenda, out estacioFinal);

        [Test]
        public void SenseEncarregats_NoPassaRes()
        {
            var capses = new double[] { 50, 0, 0 };
            var r = Calcular(H, new double[3], capses, 0, 10, 0, out double est);

            Assert.IsFalse(r.HiHaAlgunGuany);
            Assert.AreEqual(50, capses[0]);
            Assert.AreEqual(10, est);
        }

        [Test]
        public void NomesPantans_SAcumulaALesCapses()
        {
            var capses = new double[2];
            var r = Calcular(100, new double[] { 3, 5 }, capses, 0, 0, 0, out double est);

            Assert.AreEqual(300, capses[0], 1e-9);
            Assert.AreEqual(500, capses[1], 1e-9);
            Assert.AreEqual(0, r.venut);
            Assert.AreEqual(0, est);
        }

        [Test]
        public void ElCollDAmpollaMana()
        {
            // Pantà 10/s, tren 20/s, tenda 4/s durant 1000 s: la tenda limita.
            var capses = new double[1];
            var r = Calcular(1000, new double[] { 10 }, capses, 20, 0, 4, out double est);

            Assert.AreEqual(10000, r.produitAlsPantans, 1e-6);
            Assert.AreEqual(10000, r.portatALEstacio, 1e-6);
            Assert.AreEqual(4000, r.venut, 1e-6);
            Assert.AreEqual(6000, est, 1e-6);
        }

        [Test]
        public void ElTopeEsRespecta()
        {
            var capses = new double[1];
            var r = Calcular(5 * H, new double[] { 1 }, capses, 0, 0, 0, out _);

            Assert.IsTrue(r.HiHaTope);
            Assert.AreEqual(2 * H, r.segonsComptats, 1e-9);
            Assert.AreEqual(2 * H, capses[0], 1e-9);
        }

        [Test]
        public void RellotgeEnrere_NoDonaRes()
        {
            var capses = new double[1];
            var r = Calcular(-500, new double[] { 10 }, capses, 10, 0, 10, out _);

            Assert.IsFalse(r.HiHaAlgunGuany);
            Assert.AreEqual(0, capses[0]);
        }

        [Test]
        public void ElTrenBuidaPerOrdre()
        {
            var capses = new double[] { 100, 100, 100 };
            Calcular(10, new double[3], capses, 15, 0, 0, out _);

            Assert.AreEqual(0, capses[0], 1e-9);
            Assert.AreEqual(50, capses[1], 1e-9);
            Assert.AreEqual(100, capses[2], 1e-9);
        }

        [Test]
        public void NaNIInfinity_NoContaminen()
        {
            var capses = new double[2];
            var r = Calcular(100, new[] { double.NaN, double.PositiveInfinity }, capses, 0, 0, 0, out _);

            Assert.IsFalse(r.HiHaAlgunGuany);
            Assert.AreEqual(0, capses[0]);
            Assert.AreEqual(0, capses[1]);
        }

        [Test]
        public void ConservaElsDoblersIRespectaElsCabals()
        {
            var rnd = new Random(42);
            for (int k = 0; k < 2000; k++)
            {
                int n = rnd.Next(1, 8);
                var capses = new double[n];
                var produccio = new double[n];
                double inicial = 0;
                for (int i = 0; i < n; i++)
                {
                    capses[i] = rnd.NextDouble() * 1e4;
                    produccio[i] = rnd.Next(0, 3) == 0 ? 0 : rnd.NextDouble() * 50;
                    inicial += capses[i];
                }
                double estacio = rnd.NextDouble() * 1e4;
                double tren = rnd.Next(0, 3) == 0 ? 0 : rnd.NextDouble() * 100;
                double tenda = rnd.Next(0, 3) == 0 ? 0 : rnd.NextDouble() * 100;

                var r = Calcular(rnd.NextDouble() * 10 * H, produccio, capses, tren, estacio, tenda, out double ef);

                double finals = 0;
                foreach (double c in capses) { Assert.GreaterOrEqual(c, -1e-6, $"cas {k}: capsa negativa"); finals += c; }
                Assert.GreaterOrEqual(ef, -1e-6, $"cas {k}: estació negativa");

                double esperat = inicial + estacio + r.produitAlsPantans;
                Assert.AreEqual(esperat, finals + ef + r.venut, Math.Max(1e-6, esperat * 1e-9), $"cas {k}: no conserva");
                Assert.LessOrEqual(r.portatALEstacio, tren * r.segonsComptats + 1e-6, $"cas {k}: el tren supera el seu cabal");
                Assert.LessOrEqual(r.venut, tenda * r.segonsComptats + 1e-6, $"cas {k}: la tenda supera el seu cabal");
            }
        }
    }
}
