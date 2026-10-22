using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // Les dues imatges d'un encarregat: el retrat del panell i el sprite del
    // món. Cadascuna es busca pel seu compte.
    public class RetratsDEncarregatsTests
    {
        private CatalegDEncarregats _cataleg;
        private Sprite _retratPerDefecte, _spritePerDefecte, _retratDeTren, _spriteDeVelocitat, _propi;

        private static Sprite Nou(string nom)
        {
            var s = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            s.name = nom;
            return s;
        }

        private static EncarregatSO Encarregat(TipusDeLloc lloc, TipusDEfecte efecte)
        {
            var e = ScriptableObject.CreateInstance<EncarregatSO>();
            e.lloc = lloc; e.efecte = efecte;
            return e;
        }

        [SetUp]
        public void Preparar()
        {
            _retratPerDefecte = Nou("retrat"); _spritePerDefecte = Nou("sprite");
            _retratDeTren = Nou("retratTren"); _spriteDeVelocitat = Nou("spriteVelocitat"); _propi = Nou("propi");

            _cataleg = ScriptableObject.CreateInstance<CatalegDEncarregats>();
            _cataleg.retratPerDefecte = _retratPerDefecte;
            _cataleg.spriteAlMonPerDefecte = _spritePerDefecte;
            _cataleg.retratsPerTipus = new[]
            {
                new RetratPerTipus { lloc = TipusDeLloc.Tren, qualsevolEfecte = true, retrat = _retratDeTren },
                new RetratPerTipus { lloc = TipusDeLloc.Panta, efecte = TipusDEfecte.VelocitatDeMoviment, spriteAlMon = _spriteDeVelocitat },
            };
        }

        [Test]
        public void RetratDeTotElLloc() =>
            Assert.AreEqual(_retratDeTren, _cataleg.RetratDe(Encarregat(TipusDeLloc.Tren, TipusDEfecte.Capacitat)));

        // El tren té retrat però no sprite: el sprite surt del de per defecte.
        [Test]
        public void SenseSpriteDelTipus_ElDePerDefecte() =>
            Assert.AreEqual(_spritePerDefecte, _cataleg.SpriteAlMonDe(Encarregat(TipusDeLloc.Tren, TipusDEfecte.Capacitat)));

        [Test]
        public void SpriteDelLlocIEfecte() =>
            Assert.AreEqual(_spriteDeVelocitat, _cataleg.SpriteAlMonDe(Encarregat(TipusDeLloc.Panta, TipusDEfecte.VelocitatDeMoviment)));

        // El pantà de velocitat té sprite però no retrat: el retrat, el de per defecte.
        [Test]
        public void SenseRetratDelTipus_ElDePerDefecte() =>
            Assert.AreEqual(_retratPerDefecte, _cataleg.RetratDe(Encarregat(TipusDeLloc.Panta, TipusDEfecte.VelocitatDeMoviment)));

        [Test]
        public void ElPropiManaSobreTot()
        {
            EncarregatSO e = Encarregat(TipusDeLloc.Tren, TipusDEfecte.Capacitat);
            e.retrat = _propi;
            e.spriteAlMon = _propi;
            Assert.AreEqual(_propi, _cataleg.RetratDe(e));
            Assert.AreEqual(_propi, _cataleg.SpriteAlMonDe(e));
        }
    }
}
