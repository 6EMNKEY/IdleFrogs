using System.Collections.Generic;
using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // L'arbre de recerca sense escena: prerequisits, costos i efectes.
    public class RecercaTests
    {
        private static NodeDeRecerca Node(string id, int pais, params string[] abans) => new NodeDeRecerca
        {
            id = id,
            nivellMaxim = 3,
            prerequisits = abans,
            cost = new[] { new CostDeRecerca { pais = pais, costBase = 10, creixement = 2 } },
            efecte = TipusDEfecteDeRecerca.Ingressos,
            pais = pais,
            valorPerNivell = 0.1,
        };

        [Test]
        public void SensePrerequisit_EsBloqueja()
        {
            var d = new DadesDeRecerca();
            NodeDeRecerca b = Node("b", 0, "a");
            Assert.AreEqual(EstatDeNode.Bloquejat, ModelDeRecerca.Estat(b, d));
            d.PosarNivell("a", 1);
            Assert.AreEqual(EstatDeNode.Disponible, ModelDeRecerca.Estat(b, d));
        }

        [Test]
        public void Comprar_GastaElsPuntsDelColorIPujaElNivell()
        {
            var d = new DadesDeRecerca();
            NodeDeRecerca a = Node("a", 1);
            d.AfegirPunts(1, 25);
            Assert.IsTrue(ModelDeRecerca.Comprar(a, d));
            Assert.AreEqual(1, d.Nivell("a"));
            Assert.AreEqual(15, d.Punts(1), 1e-9);
            // El segon nivell en costa 20: no n'hi ha prou.
            Assert.IsFalse(ModelDeRecerca.Comprar(a, d));
        }

        [Test]
        public void AlMaxim_NoEsPotComprarMes()
        {
            var d = new DadesDeRecerca();
            NodeDeRecerca a = Node("a", 0);
            d.AfegirPunts(0, 1000);
            for (int i = 0; i < 3; i++) Assert.IsTrue(ModelDeRecerca.Comprar(a, d));
            Assert.AreEqual(EstatDeNode.Maxim, ModelDeRecerca.Estat(a, d));
            Assert.IsFalse(ModelDeRecerca.Comprar(a, d));
        }

        [Test]
        public void DiversosColors_ElsDemanaTots()
        {
            var d = new DadesDeRecerca();
            var n = new NodeDeRecerca
            {
                id = "x", nivellMaxim = 1,
                cost = new[] { new CostDeRecerca { pais = 0, costBase = 5, creixement = 1 },
                               new CostDeRecerca { pais = 2, costBase = 5, creixement = 1 } },
            };
            d.AfegirPunts(0, 5);
            Assert.IsFalse(ModelDeRecerca.LiArriba(n, d));
            d.AfegirPunts(2, 5);
            Assert.IsTrue(ModelDeRecerca.Comprar(n, d));
        }

        [Test]
        public void Suma_NomesElsDelPaisIElsDeTots()
        {
            var d = new DadesDeRecerca();
            var nodes = new List<NodeDeRecerca> { Node("a", 0), Node("b", 1), Node("tots", -1) };
            d.PosarNivell("a", 2);
            d.PosarNivell("b", 3);
            d.PosarNivell("tots", 1);
            Assert.AreEqual(0.3, ModelDeRecerca.Suma(nodes, d, TipusDEfecteDeRecerca.Ingressos, 0), 1e-9);
            Assert.AreEqual(0.4, ModelDeRecerca.Suma(nodes, d, TipusDEfecteDeRecerca.Ingressos, 1), 1e-9);
            Assert.AreEqual(0, ModelDeRecerca.Suma(nodes, d, TipusDEfecteDeRecerca.VelocitatDelTren, 0), 1e-9);
        }

        [Test]
        public void CostTotal_SumaTotsElsNivells()
        {
            double[] t = ModelDeRecerca.CostTotal(new List<NodeDeRecerca> { Node("a", 0) }, 2);
            Assert.AreEqual(10 + 20 + 40, t[0], 1e-9);
            Assert.AreEqual(0, t[1], 1e-9);
        }
    }
}
