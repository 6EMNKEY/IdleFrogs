using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // Les fórmules del pantà, sobre un Panta de debò (no una còpia de les
    // fórmules). La config es posa a mà amb els valors del primer pantà,
    // perquè si un dia canvies el TestingPantaConfig aquests tests no petin.
    public class EconomiaDelPantaTests
    {
        private GameObject _go;
        private PantaConfigSO _config;
        private Panta _panta;

        [SetUp]
        public void Preparar()
        {
            _config = ScriptableObject.CreateInstance<PantaConfigSO>();
            _config.capacitatBase = 20;
            _config.velocitatDeRecoleccioBase = 5;
            _config.creixementDeCapacitat = 1.1;
            _config.creixementDeVelocitatDeRecoleccio = 1.1;
            _config.tempsBuidar = 0.2f;
            _config.velocitatDeMoviment = 2f;
            _config.granotesInicials = 1;
            _config.nivellsDeGranotaExtra = new[] { 10, 50 };
            _config.fites = new[]
            {
                new FitaDeMultiplicador { nivell = 25, multiplicador = 2.1 },
                new FitaDeMultiplicador { nivell = 50, multiplicador = 2.1 },
            };
            _config.costBase = 100;
            _config.creixementDeCost = 1.2;
            _config.multiplicadorDelPrimerSalt = 50;
            _config.multiplicadorDeCadaSalt = 10;
            _config.preusDeCompra = new double[] { 10, 1000, 30000 };
            _config.factorDePreuDeCompra = 20;
            _config.nivellMaxim = 800;

            _go = new GameObject("PantaDeProva");
            _panta = _go.AddComponent<Panta>();
            var so = new SerializedObject(_panta);
            so.FindProperty("config").objectReferenceValue = _config;
            so.ApplyModifiedPropertiesWithoutUndo();
            _panta.AssignarIndex(0);
            _panta.AplicarNivell(1);
        }

        [TearDown]
        public void Netejar()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_config);
        }

        // Capacitat i velocitat de recol·lecció del primer pantà, nivells 1
        // a 10. Amb el floor surten exactes.
        [Test]
        public void TaulaDelPrimerPanta_Nivells1A10()
        {
            int[] capacitat = { 20, 22, 24, 26, 29, 32, 35, 38, 42, 47 };
            int[] recolleccio = { 5, 5, 6, 6, 7, 8, 8, 9, 10, 11 };
            int[] granotes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 2 };

            for (int n = 1; n <= 10; n++)
            {
                Assert.AreEqual(capacitat[n - 1], _panta.CapacitatAlNivell(n), $"capacitat al nivell {n}");
                Assert.AreEqual(recolleccio[n - 1], _panta.VelocitatDeRecoleccioAlNivell(n), $"recol·lecció al nivell {n}");
                Assert.AreEqual(granotes[n - 1], _panta.GranotesAlNivell(n), $"granotes al nivell {n}");
            }
        }

        [Test]
        public void TempsDOmplir_EsCapacitatEntreVelocitat()
        {
            for (int n = 1; n <= 100; n++)
            {
                float esperat = (float)(_panta.CapacitatAlNivell(n) / _panta.VelocitatDeRecoleccioAlNivell(n));
                Assert.AreEqual(esperat, _panta.TempsFinsPleAlNivell(n), 1e-5f, $"nivell {n}");
                // Pugen pel mateix factor: el temps es mou poc (només pel floor).
                Assert.That(_panta.TempsFinsPleAlNivell(n), Is.InRange(3.9f, 4.5f), $"nivell {n}");
            }
        }

        [TestCase(0, 1.0)]
        [TestCase(1, 50.0)]
        [TestCase(2, 500.0)]
        [TestCase(5, 500000.0)]
        public void Profunditat_50IDespres10(int index, double esperat) =>
            Assert.AreEqual(esperat, _config.MultiplicadorDeProfunditat(index), 1e-9);

        [TestCase(0, 10.0)]
        [TestCase(1, 1000.0)]
        [TestCase(2, 30000.0)]
        [TestCase(3, 600000.0)]
        [TestCase(4, 12000000.0)]
        public void PreuDeCompra(int index, double esperat) =>
            Assert.AreEqual(esperat, _config.PreuDeCompraAlIndex(index), 1e-6);

        [Test]
        public void ElSegonPanta_Produeix50Vegades()
        {
            double primer = _panta.CapacitatAlNivell(1);
            _panta.AssignarIndex(1);
            Assert.AreEqual(primer * 50, _panta.CapacitatAlNivell(1), 1e-6);
        }

        [Test]
        public void LaFitaDel25_MultiplicaPer2i1()
        {
            double sense = 20 * System.Math.Pow(1.1, 24);
            Assert.AreEqual(System.Math.Floor(sense * 2.1), _panta.CapacitatAlNivell(25));
        }

        [Test]
        public void SenseComprar_NoHiHaGranotesNiCost()
        {
            _panta.AplicarNivell(0);
            Assert.IsFalse(_panta.EstaComprat);
            Assert.AreEqual(0, _panta.GranotesActives);
            Assert.AreEqual(0, _panta.CostDeMillora(1));
        }

        [Test]
        public void ElPrimerNivell_Costa100()
        {
            Assert.AreEqual(100, _panta.CostDeMillora(1), 1e-9);
        }

        // Pujar k nivells de cop costa el mateix que pujar-los d'un en un.
        [TestCase(1, 10)]
        [TestCase(16, 50)]
        [TestCase(100, 25)]
        public void SerieGeometrica_IgualALaSumaDUnEnUn(int nivell, int k)
        {
            _panta.AplicarNivell(nivell);
            double deCop = _panta.CostDeMillora(k);

            double suma = 0;
            for (int i = 0; i < k; i++)
            {
                _panta.AplicarNivell(nivell + i);
                suma += _panta.CostDeMillora(1);
            }
            Assert.AreEqual(suma, deCop, suma * 1e-9);
        }

        [Test]
        public void LaReduccioDeCost_SAplica()
        {
            var b = Bonificacions.Cap;
            b.reduccioDeCost = 0.4;
            _panta.AplicarBonificacions(b, false);
            Assert.AreEqual(60, _panta.CostDeMillora(1), 1e-9);
        }

        [Test]
        public void ElBonusDeTreball_EscurcaElTemps()
        {
            float abans = _panta.TempsFinsPle;
            var b = Bonificacions.Cap;
            b.treball = 2;
            _panta.AplicarBonificacions(b, true);
            Assert.AreEqual(abans / 2f, _panta.TempsFinsPle, 1e-4f);
            Assert.IsTrue(_panta.EstaAutomatitzada);
        }

        [Test]
        public void AlLimit_NoDesborda()
        {
            _panta.AssignarIndex(34);
            _panta.AplicarNivell(800);
            Assert.IsFalse(double.IsInfinity(_panta.CapacitatAlNivell(800)));
            Assert.IsFalse(double.IsNaN(_panta.ProduccioAlNivell(800)));
            Assert.IsFalse(double.IsInfinity(_panta.CostDeMillora(1)));
        }
    }
}
