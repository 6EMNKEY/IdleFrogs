using System;
using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // El temps de la fitxa i de damunt del sprite: durada, el que queda
    // encesa o el que falta per tornar a estar llesta.
    public class TempsDeLHabilitatTests
    {
        private EncarregatSO _d;

        [SetUp]
        public void Preparar()
        {
            _d = ScriptableObject.CreateInstance<EncarregatSO>();
            _d.segonsDeDuracio = 180;
            _d.segonsDeRecarrega = 600;
        }

        [TearDown]
        public void Netejar() => UnityEngine.Object.DestroyImmediate(_d);

        private EncarregatPosseit Amb(double segonsEncesa, double segonsRecarrega)
        {
            DateTime ara = DateTime.UtcNow;
            return new EncarregatPosseit
            {
                definicio = _d,
                fiDeLHabilitat = ara.AddSeconds(segonsEncesa),
                fiDeRecarrega = ara.AddSeconds(segonsRecarrega)
            };
        }

        [Test]
        public void MaiActivada_EnsenyaLaDurada()
        {
            var e = new EncarregatPosseit { definicio = _d, fiDeLHabilitat = DateTime.MinValue, fiDeRecarrega = DateTime.MinValue };
            Assert.AreEqual(EstatDHabilitat.Llesta, e.Estat);
            Assert.AreEqual("3m", e.TempsDeLHabilitat);
        }

        // Els 0,5 s de marge: entre crear-lo i llegir-lo passa una mica de temps.
        [Test]
        public void Encesa_DescomptaLaDurada()
        {
            EncarregatPosseit e = Amb(90.5, 510.5);
            Assert.AreEqual(EstatDHabilitat.Activa, e.Estat);
            Assert.AreEqual("1m 31s", e.TempsDeLHabilitat);
        }

        [Test]
        public void Recarregant_DescomptaFinsLlesta()
        {
            EncarregatPosseit e = Amb(-10, 120.5);
            Assert.AreEqual(EstatDHabilitat.Recarregant, e.Estat);
            Assert.AreEqual("2m 1s", e.TempsDeLHabilitat);
        }

        [Test]
        public void RecarregaAcabada_TornaALaDurada()
        {
            EncarregatPosseit e = Amb(-400, -1);
            Assert.AreEqual(EstatDHabilitat.Llesta, e.Estat);
            Assert.AreEqual("3m", e.TempsDeLHabilitat);
        }

        // Si la recàrrega és més curta que la durada, mentre dura és "encesa".
        [Test]
        public void EncesaIJaLlesta_ManaEncesa()
        {
            EncarregatPosseit e = Amb(30.5, -1);
            Assert.AreEqual(EstatDHabilitat.Activa, e.Estat);
        }
    }
}
