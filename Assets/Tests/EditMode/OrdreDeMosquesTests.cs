using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class OrdreDeMosquesTests
    {
        private static int[] Calcular(float[] alcades, int ordreBase = 50)
        {
            var ordres = new int[alcades.Length];
            OrdreDeMosques.Calcular(alcades, ordreBase, ordres);
            return ordres;
        }

        [Test]
        public void LaMesBaixa_VaDavant()
        {
            int[] ordres = Calcular(new[] { 1f, -1f, 0f, 2f });
            CollectionAssert.AreEqual(new[] { 51, 53, 52, 50 }, ordres);
        }

        [Test]
        public void EnUnaFranjaDeBaseABaseMesN()
        {
            int[] ordres = Calcular(new[] { 0.3f, 0.1f, 0.2f }, 10);
            CollectionAssert.AreEquivalent(new[] { 10, 11, 12 }, ordres);
        }

        [Test]
        public void AmbYIguals_NoEsRepeteixen()
        {
            int[] ordres = Calcular(new[] { 0.5f, 0.5f, 0.5f });
            CollectionAssert.AllItemsAreUnique(ordres);
            CollectionAssert.AreEquivalent(new[] { 50, 51, 52 }, ordres);
        }

        [Test]
        public void UnaSolaMosca_TeLOrdreBase() =>
            CollectionAssert.AreEqual(new[] { 50 }, Calcular(new[] { 3f }));
    }
}
