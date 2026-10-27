using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // Els textos de les cartes del panell d'encarregats.
    public class EncarregatSOTests
    {
        private EncarregatSO _e;

        [SetUp]
        public void Preparar()
        {
            // Els textos esperats són els catalans.
            Textos.EstablirPerAProves(Idioma.Catala);
            _e = ScriptableObject.CreateInstance<EncarregatSO>();
            _e.segonsDeDuracio = 180;
            _e.magnitud = 2;
            _e.magnitudDeLHabilitat = 5;
        }

        [TearDown]
        public void Netejar()
        {
            Object.DestroyImmediate(_e);
            Textos.EstablirPerAProves(Idioma.English);
        }

        [Test]
        public void Duracio() => Assert.AreEqual("3m", _e.DuracioCurta);

        // Treballar vol dir recollir al pantà i carregar al tren.
        [TestCase(TipusDeLloc.Panta, "5x Velocitat de recol·lecció", "5x Recol")]
        [TestCase(TipusDeLloc.Tren, "5x Velocitat de càrrega", "5x Càrrega")]
        public void Treball_SegonsElLloc(TipusDeLloc lloc, string llarga, string curta)
        {
            _e.lloc = lloc;
            _e.efecte = TipusDEfecte.VelocitatDeTreball;
            Assert.AreEqual(llarga, _e.DescripcioDeLHabilitat);
            Assert.AreEqual(curta, _e.DescripcioCurtaDeLHabilitat);
        }

        // L'habilitat de rebaixa és la rebaixa total mentre dura, no un
        // multiplicador de la passiva, amb el topall del 95%.
        [Test]
        public void ReduccioDeCost_EnPercentatge()
        {
            _e.efecte = TipusDEfecte.ReduccioDeCost;
            _e.magnitud = 0.02;
            _e.magnitudDeLHabilitat = 0.4;
            Assert.AreEqual("-40% cost", _e.DescripcioCurtaDeLHabilitat);

            _e.magnitudDeLHabilitat = 1.5;
            Assert.AreEqual("-95% Cost de millora", _e.DescripcioDeLHabilitat);
        }
    }
}
