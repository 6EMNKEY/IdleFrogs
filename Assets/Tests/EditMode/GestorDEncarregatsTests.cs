using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // El guardat i la càrrega del gestor. Comprar no es prova aquí perquè
    // necessita el CurrencyManager viu, i en EditMode no s'executa l'Awake.
    public class GestorDEncarregatsTests
    {
        private GameObject _go;
        private GestorDEncarregats _gestor;
        private CatalegDEncarregats _cataleg;

        private static EncarregatSO Nou(string id, TipusDeLloc lloc)
        {
            var e = ScriptableObject.CreateInstance<EncarregatSO>();
            e.id = id; e.lloc = lloc;
            e.segonsDeDuracio = 60; e.segonsDeRecarrega = 300;
            return e;
        }

        private static EncarregatGuardat Guardat(string id, int instancia, TipusDeLloc lloc,
                                                 DateTime fiHabilitat = default) => new EncarregatGuardat
        {
            idDefinicio = id, idInstancia = instancia, assignat = false,
            llocTipus = (int)lloc, llocIndex = 0,
            fiDeLHabilitatUtc = fiHabilitat.ToBinary(), fiDeRecarregaUtc = DateTime.MinValue.ToBinary()
        };

        [SetUp]
        public void Preparar()
        {
            _cataleg = ScriptableObject.CreateInstance<CatalegDEncarregats>();
            _cataleg.preuBase = 100;
            _cataleg.creixementDePreu = 4;
            _cataleg.encarregats = new[] { Nou("p", TipusDeLloc.Panta), Nou("t", TipusDeLloc.Tren), Nou("s", TipusDeLloc.Tenda) };

            _go = new GameObject("Gestor");
            _gestor = _go.AddComponent<GestorDEncarregats>();
            var so = new SerializedObject(_gestor);
            so.FindProperty("cataleg").objectReferenceValue = _cataleg;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Netejar()
        {
            UnityEngine.Object.DestroyImmediate(_go);
            foreach (var e in _cataleg.encarregats) UnityEngine.Object.DestroyImmediate(e);
            UnityEngine.Object.DestroyImmediate(_cataleg);
        }

        [Test]
        public void PartidaNova_TotsElsTipusComencenA100()
        {
            _gestor.Carregar(null, null);
            foreach (TipusDeLloc l in Enum.GetValues(typeof(TipusDeLloc)))
                Assert.AreEqual(100, _gestor.PreuDeLaSeguentCompra(l), 1e-9, l.ToString());
        }

        // Comprar-ne de tenda no encareix els de pantà.
        [Test]
        public void ElPreuPujaPerTipus()
        {
            _gestor.Carregar(null, new[] { 1, 0, 4 });   // Panta, Tren, Tenda
            Assert.AreEqual(400, _gestor.PreuDeLaSeguentCompra(TipusDeLloc.Panta), 1e-9);
            Assert.AreEqual(100, _gestor.PreuDeLaSeguentCompra(TipusDeLloc.Tren), 1e-9);
            Assert.AreEqual(25600, _gestor.PreuDeLaSeguentCompra(TipusDeLloc.Tenda), 1e-9);
        }

        // Partides d'abans del comptador per tipus: es reconstrueix comptant
        // els que tens de cada tipus.
        [Test]
        public void SenseComptadors_EsReconstrueixenDelsQueTens()
        {
            _gestor.Carregar(new[]
            {
                Guardat("s", 1, TipusDeLloc.Tenda),
                Guardat("s", 2, TipusDeLloc.Tenda),
                Guardat("p", 3, TipusDeLloc.Panta),
            }, null);

            Assert.AreEqual(1, _gestor.Comprats(TipusDeLloc.Panta));
            Assert.AreEqual(0, _gestor.Comprats(TipusDeLloc.Tren));
            Assert.AreEqual(2, _gestor.Comprats(TipusDeLloc.Tenda));
        }

        [Test]
        public void GuardarICarregar_EsConserva()
        {
            _gestor.Carregar(new[] { Guardat("t", 7, TipusDeLloc.Tren) }, new[] { 0, 3, 0 });
            var desats = _gestor.Guardar();
            var comptadors = _gestor.GuardarComprats();

            _gestor.Carregar(desats, comptadors);
            Assert.AreEqual(1, _gestor.Posseits.Count);
            Assert.AreEqual("t", _gestor.Posseits[0].definicio.id);
            Assert.AreEqual(7, _gestor.Posseits[0].id);
            Assert.AreEqual(3, _gestor.Comprats(TipusDeLloc.Tren));
        }

        // Si l'asset d'un encarregat ja no existeix, es descarta amb un avís
        // en comptes de petar o deixar una entrada buida.
        [Test]
        public void IdDesconegut_EsDescarta()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no trobo l'encarregat 'fantasma'"));
            _gestor.Carregar(new[] { Guardat("fantasma", 1, TipusDeLloc.Panta) }, null);
            Assert.AreEqual(0, _gestor.Posseits.Count);
        }

        // L'error que vam arreglar: una habilitat encara encesa en carregar
        // s'ha de poder apagar després (caducitatAvisada = false).
        [Test]
        public void HabilitatEncesaEnCarregar_QuedaPendentDApagar()
        {
            _gestor.Carregar(new[]
            {
                Guardat("p", 1, TipusDeLloc.Panta, DateTime.UtcNow.AddMinutes(5)),
                Guardat("t", 2, TipusDeLloc.Tren, DateTime.UtcNow.AddMinutes(-5)),
            }, null);

            Assert.IsTrue(_gestor.Posseits[0].HabilitatActiva);
            Assert.IsFalse(_gestor.Posseits[0].caducitatAvisada);
            Assert.IsFalse(_gestor.Posseits[1].HabilitatActiva);
            Assert.IsTrue(_gestor.Posseits[1].caducitatAvisada);
        }

        [Test]
        public void ElsIdsDInstancia_NoEsRepeteixen()
        {
            _gestor.Carregar(new[] { Guardat("p", 5, TipusDeLloc.Panta), Guardat("p", 9, TipusDeLloc.Panta) }, null);
            var ids = new System.Collections.Generic.HashSet<int>();
            foreach (var e in _gestor.Posseits) Assert.IsTrue(ids.Add(e.id));
        }

        // ---- Venda ----

        [Test]
        public void PreuDeVenda_EsLaMeitatDelQueVaCostar() =>
            Assert.AreEqual(200, GestorDEncarregats.PreuDeVenda(new EncarregatPosseit { preuPagat = 400 }), 1e-9);

        [Test]
        public void ElPreuPagat_EsGuardaICarrega()
        {
            var g = Guardat("p", 1, TipusDeLloc.Panta);
            g.preuPagat = 6400;
            _gestor.Carregar(new[] { g }, new[] { 3, 0, 0 });

            _gestor.Carregar(_gestor.Guardar(), _gestor.GuardarComprats());
            Assert.AreEqual(6400, _gestor.Posseits[0].preuPagat, 1e-9);
        }

        // Partides d'abans de la venda: el preu es dedueix de l'ordre de
        // compra de cada tipus (100, 400, 1600...), per id d'instància.
        [Test]
        public void SensePreuGuardat_EsReconstrueixPerOrdreDeCompra()
        {
            _gestor.Carregar(new[]
            {
                Guardat("p", 4, TipusDeLloc.Panta),
                Guardat("s", 2, TipusDeLloc.Tenda),
                Guardat("p", 1, TipusDeLloc.Panta),
                Guardat("p", 3, TipusDeLloc.Panta),
            }, null);

            double PerId(int id)
            {
                foreach (var e in _gestor.Posseits) if (e.id == id) return e.preuPagat;
                return -1;
            }

            Assert.AreEqual(100, PerId(1), 1e-9);
            Assert.AreEqual(400, PerId(3), 1e-9);
            Assert.AreEqual(1600, PerId(4), 1e-9);
            Assert.AreEqual(100, PerId(2), 1e-9);    // la tenda compta a part
        }

        [Test]
        public void NoAssignats_NomesElsDelTipusSenseFeina()
        {
            var assignat = Guardat("p", 1, TipusDeLloc.Panta);
            assignat.assignat = true;
            _gestor.Carregar(new[] { assignat, Guardat("p", 2, TipusDeLloc.Panta), Guardat("t", 3, TipusDeLloc.Tren) }, null);

            var llista = new System.Collections.Generic.List<EncarregatPosseit>();
            _gestor.NoAssignats(TipusDeLloc.Panta, llista);
            Assert.AreEqual(1, llista.Count);
            Assert.AreEqual(2, llista[0].id);
        }
    }
}
