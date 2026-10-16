using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class PlanificadorDeDecoracioTests
    {
        private static readonly Rect Zona = new Rect(-10, -5, 20, 10);

        private static VariantPlana V(float pes, float min = 1, float max = 1) => new VariantPlana
        {
            pes = pes, escalaMinima = min, escalaMaxima = max, girarALAtzar = true,
            limits = new Rect(-0.25f, 0, 0.5f, 0.5f)   // pivot a baix al mig
        };

        private static List<Colocacio> Planificar(int llavor, int quantitat, VariantPlana[] variants,
                                                  IList<Rect> evitar = null, float marge = 0, float distancia = 0) =>
            PlanificadorDeDecoracio.Planificar(llavor, Zona, quantitat, variants, evitar, marge, distancia);

        [Test]
        public void LaMateixaLlavor_DonaElMateixCamp()
        {
            var variants = new[] { V(1, 0.5f, 2), V(2) };
            var a = Planificar(42, 300, variants, distancia: 0.3f);
            var b = Planificar(42, 300, variants, distancia: 0.3f);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].posicio, b[i].posicio);
                Assert.AreEqual(a[i].variant, b[i].variant);
                Assert.AreEqual(a[i].escala, b[i].escala);
                Assert.AreEqual(a[i].girada, b[i].girada);
            }
        }

        [Test]
        public void LlavorsDiferents_CampsDiferents()
        {
            var variants = new[] { V(1) };
            Assert.AreNotEqual(Planificar(1, 50, variants)[0].posicio, Planificar(2, 50, variants)[0].posicio);
        }

        [Test]
        public void NoTocaElQueSEvita_NiElMarge()
        {
            var evitar = new List<Rect> { new Rect(-3, -2, 6, 4), new Rect(6, -5, 4, 10) };
            var camp = Planificar(7, 800, new[] { V(1, 0.5f, 3) }, evitar, marge: 0.5f);

            Assert.Greater(camp.Count, 100);
            foreach (Colocacio c in camp)
                foreach (Rect r in evitar)
                    Assert.IsFalse(c.requadre.Overlaps(new Rect(r.xMin - 0.5f, r.yMin - 0.5f, r.width + 1, r.height + 1)),
                                   $"{c.requadre} toca {r}");
        }

        [Test]
        public void RespectaLaDistanciaMinima()
        {
            var camp = Planificar(3, 2000, new[] { V(1) }, distancia: 0.6f);
            for (int i = 0; i < camp.Count; i++)
                for (int j = i + 1; j < camp.Count; j++)
                    Assert.GreaterOrEqual((camp[i].posicio - camp[j].posicio).magnitude, 0.6f);
        }

        [Test]
        public void ElsPesosMarquenLaProporcio()
        {
            var camp = Planificar(11, 4000, new[] { V(3), V(1), V(0) });
            int primera = 0, tercera = 0;
            foreach (Colocacio c in camp)
            {
                if (c.variant == 0) primera++;
                if (c.variant == 2) tercera++;
            }
            Assert.AreEqual(0.75, (double)primera / camp.Count, 0.03);
            Assert.AreEqual(0, tercera, "una variant amb pes 0 no surt mai");
        }

        [Test]
        public void SensePes_NoPosaRes() =>
            Assert.AreEqual(0, Planificar(1, 100, new[] { V(0), V(0) }).Count);

        [Test]
        public void LEscalaEsDinsDelRang()
        {
            foreach (Colocacio c in Planificar(5, 500, new[] { V(1, 0.7f, 1.4f) }))
                Assert.That(c.escala, Is.InRange(0.7f, 1.4f));
        }

        [Test]
        public void ElDeMesAvall_VaDavant()
        {
            var camp = new List<Colocacio>
            {
                new Colocacio { posicio = new Vector2(0, 1) },
                new Colocacio { posicio = new Vector2(0, -2) },
                new Colocacio { posicio = new Vector2(0, 3) },
            };
            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, PlanificadorDeDecoracio.OrdresDeDibuix(camp));
        }

        // Girada en horitzontal, un dibuix descentrat queda a l'altra banda del pivot.
        [Test]
        public void ElRequadre_EsGiraAmbElDibuix()
        {
            var limits = new Rect(0, 0, 1, 1);   // tot a la dreta del pivot
            Rect normal = PlanificadorDeDecoracio.Requadre(limits, Vector2.zero, 2, false);
            Rect girat = PlanificadorDeDecoracio.Requadre(limits, Vector2.zero, 2, true);
            Assert.AreEqual(new Rect(0, 0, 2, 2), normal);
            Assert.AreEqual(new Rect(-2, 0, 2, 2), girat);
        }
    }
}
