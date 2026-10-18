using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class MosquesSegonsLaCarregaTests
    {
        private static readonly TramDeMosques[] Trams =
        {
            new TramDeMosques { desDe = 0.5f,  mosques = 7 },   // desordenats a propòsit
            new TramDeMosques { desDe = 0f,    mosques = 1 },
            new TramDeMosques { desDe = 0.75f, mosques = 15 },
            new TramDeMosques { desDe = 0.25f, mosques = 3 },
        };

        [TestCase(0f, 0)]        // buit: cap
        [TestCase(0.01f, 1)]
        [TestCase(0.2499f, 1)]
        [TestCase(0.25f, 3)]
        [TestCase(0.6f, 7)]
        [TestCase(0.75f, 15)]
        [TestCase(1f, 15)]
        public void SegonsComDePle(float fraccio, int esperat) =>
            Assert.AreEqual(esperat, MosquesSegonsLaCarrega.Quantes(fraccio, Trams));

        [Test]
        public void SenseTrams_Cap() =>
            Assert.AreEqual(0, MosquesSegonsLaCarrega.Quantes(0.5f, null));
    }
}
