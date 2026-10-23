using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    public class SpritesDelPaisTests
    {
        private readonly List<Object> _creats = new List<Object>();

        private Sprite NouSprite(string nom)
        {
            var tex = new Texture2D(2, 2);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            s.name = nom;
            _creats.Add(tex);
            _creats.Add(s);
            return s;
        }

        private PaisSO NouPais()
        {
            var p = ScriptableObject.CreateInstance<PaisSO>();
            _creats.Add(p);
            return p;
        }

        [TearDown]
        public void Netejar()
        {
            foreach (Object o in _creats) Object.DestroyImmediate(o);
            _creats.Clear();
        }

        [Test]
        public void SenseCanvis_NoHiHaRes()
        {
            var canvis = new Dictionary<Sprite, Sprite>();
            SpritesDelPais.Omplir(canvis, NouPais());
            Assert.AreEqual(0, canvis.Count);

            SpritesDelPais.Omplir(canvis, null);
            Assert.AreEqual(0, canvis.Count);
        }

        // Totes les llistes compten igual, i les files a mig omplir no.
        [Test]
        public void JuntaTotesLesLlistes_IIgnoraLesBuides()
        {
            Sprite tren = NouSprite("tren"), trenFoc = NouSprite("trenFoc");
            Sprite herba = NouSprite("herba"), herbaFoc = NouSprite("herbaFoc");
            Sprite granota = NouSprite("granota");

            PaisSO p = NouPais();
            p.tren = new[] { new CanviDeSprite { original = tren, perAquestPais = trenFoc } };
            p.herbesIFlors = new[] { new CanviDeSprite { original = herba, perAquestPais = herbaFoc } };
            p.granotes = new[]
            {
                new CanviDeSprite { original = granota, perAquestPais = null },
                new CanviDeSprite { original = null, perAquestPais = herbaFoc },
                null
            };

            var canvis = new Dictionary<Sprite, Sprite>();
            SpritesDelPais.Omplir(canvis, p);

            Assert.AreEqual(2, canvis.Count);
            Assert.AreSame(trenFoc, canvis[tren]);
            Assert.AreSame(herbaFoc, canvis[herba]);
            Assert.IsFalse(canvis.ContainsKey(granota));
        }

        [Test]
        public void OriginalRepetit_ManaElPrimer()
        {
            Sprite mosca = NouSprite("mosca"), a = NouSprite("a"), b = NouSprite("b");
            PaisSO p = NouPais();
            p.mosques = new[] { new CanviDeSprite { original = mosca, perAquestPais = a } };
            p.botons = new[] { new CanviDeSprite { original = mosca, perAquestPais = b } };

            var canvis = new Dictionary<Sprite, Sprite>();
            SpritesDelPais.Omplir(canvis, p);

            Assert.AreSame(a, canvis[mosca]);
        }

        // Sense gestor de països (editor, tests) no canvia res.
        [Test]
        public void SenseGestor_TornaElMateix()
        {
            SpritesDelPais.Refrescar();
            Sprite s = NouSprite("s");
            Assert.AreSame(s, SpritesDelPais.Substituir(s));
            Assert.IsNull(SpritesDelPais.Substituir(null));
        }
    }
}
