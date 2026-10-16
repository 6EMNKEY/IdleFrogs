using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class CatalegDEncarregatsTests
    {
        private CatalegDEncarregats _cataleg;

        private static EncarregatSO Nou(string id, TipusDeLloc lloc, Raresa raresa)
        {
            var e = ScriptableObject.CreateInstance<EncarregatSO>();
            e.id = id; e.lloc = lloc; e.raresa = raresa;
            return e;
        }

        [SetUp]
        public void Preparar()
        {
            _cataleg = ScriptableObject.CreateInstance<CatalegDEncarregats>();
            _cataleg.preuBase = 100;
            _cataleg.creixementDePreu = 4;
            var tots = new System.Collections.Generic.List<EncarregatSO>();
            foreach (TipusDeLloc l in System.Enum.GetValues(typeof(TipusDeLloc)))
                foreach (Raresa r in System.Enum.GetValues(typeof(Raresa)))
                    tots.Add(Nou($"{l}_{r}", l, r));
            _cataleg.encarregats = tots.ToArray();
        }

        [TearDown]
        public void Netejar()
        {
            foreach (var e in _cataleg.encarregats) Object.DestroyImmediate(e);
            Object.DestroyImmediate(_cataleg);
        }

        [TestCase(0, 100.0)]
        [TestCase(1, 400.0)]
        [TestCase(2, 1600.0)]
        [TestCase(3, 6400.0)]
        public void Preu(int jaComprats, double esperat) =>
            Assert.AreEqual(esperat, _cataleg.PreuDeLaCompra(jaComprats), 1e-9);

        // El tipus surt de la silueta des d'on compres, mai de la sort.
        [Test]
        public void ElTipusSurtDelLlocDeCompra()
        {
            Random.InitState(1);
            foreach (TipusDeLloc l in System.Enum.GetValues(typeof(TipusDeLloc)))
                for (int i = 0; i < 500; i++)
                    Assert.AreEqual(l, _cataleg.Sortejar(l).lloc);
        }

        [Test]
        public void ElsPesosEsRespecten()
        {
            Random.InitState(7);
            var compte = new int[4];
            const int n = 40000;
            for (int i = 0; i < n; i++) compte[(int)_cataleg.TirarRaresa()]++;

            float[] esperat = { 80f, 15f, 4.8f, 0.2f };
            for (int r = 0; r < 4; r++)
                Assert.AreEqual(esperat[r], 100f * compte[r] / n, 0.8f, $"raresa {(Raresa)r}");
        }

        // Si no hi ha cap encarregat de la raresa que surt, baixa de raresa:
        // mai no es cobra sense donar res.
        [Test]
        public void SenseLaRaresa_BaixaFinsATrobarNe()
        {
            foreach (var e in _cataleg.encarregats) Object.DestroyImmediate(e);
            _cataleg.encarregats = new[] { Nou("unic", TipusDeLloc.Tenda, Raresa.Comu) };

            Random.InitState(3);
            for (int i = 0; i < 500; i++)
                Assert.IsNotNull(_cataleg.Sortejar(TipusDeLloc.Tenda));
            Assert.IsNull(_cataleg.Sortejar(TipusDeLloc.Panta));
        }

        [Test]
        public void PerId()
        {
            Assert.AreEqual("Tren_Epic", _cataleg.PerId("Tren_Epic").id);
            Assert.IsNull(_cataleg.PerId("no_existeix"));
            Assert.IsNull(_cataleg.PerId(null));
        }
    }
}
