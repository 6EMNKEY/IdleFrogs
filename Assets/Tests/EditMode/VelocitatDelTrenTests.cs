using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // La velocitat del tren puja de pressa al principi i cada cop més a poc
    // a poc, fins a la màxima al nivell que diu la configuració.
    public class VelocitatDelTrenTests
    {
        private TrenConfigSO _c;

        [SetUp]
        public void Preparar()
        {
            _c = ScriptableObject.CreateInstance<TrenConfigSO>();
            _c.velocitatBase = 3f;
            _c.velocitatMaxima = 40f;
            _c.nivellDeVelocitatMaxima = 800;
            _c.suavitatInicial = 2.2f;
        }

        [TearDown]
        public void Netejar() => Object.DestroyImmediate(_c);

        [Test]
        public void ComencaALaBase() => Assert.AreEqual(3f, _c.VelocitatAlNivell(1), 1e-4f);

        [Test]
        public void ArribaALaMaximaAlNivellDit()
        {
            Assert.AreEqual(40f, _c.VelocitatAlNivell(800), 1e-3f);
            Assert.AreEqual(40f, _c.VelocitatAlNivell(1500), 1e-3f);
            Assert.Less(_c.VelocitatAlNivell(799), 40f);
        }

        // Ràpid però no tant: com a molt 5,2 al nivell 8.
        [Test]
        public void AlNivell8_NoPassaDe5i2() => Assert.LessOrEqual(_c.VelocitatAlNivell(8), 5.2f);

        // Del 400 al 800 va de ~30 a 40.
        [Test]
        public void AlNivell400_Uns30() => Assert.AreEqual(29f, _c.VelocitatAlNivell(400), 1f);

        // Cada nivell suma menys que l'anterior.
        [Test]
        public void CadaCopMesAPocAPoc()
        {
            float salt10 = _c.VelocitatAlNivell(11) - _c.VelocitatAlNivell(10);
            float salt200 = _c.VelocitatAlNivell(201) - _c.VelocitatAlNivell(200);
            Assert.Greater(salt10, salt200 * 3);
        }

        [Test]
        public void SenseMarge_EsQuedaALaBase()
        {
            _c.velocitatMaxima = 2f;
            Assert.AreEqual(3f, _c.VelocitatAlNivell(100), 1e-4f);
        }
    }
}
