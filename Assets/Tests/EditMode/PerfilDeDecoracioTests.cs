using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // Cada país té el seu perfil; sense, el de l'objecte de decoració. Cada
    // capa va en el seu Sorting Group amb el seu ordre.
    public class PerfilDeDecoracioTests
    {
        private GameObject _go;
        private DecoracioDeGespa _d;
        private PerfilDeDecoracio _perDefecte, _delPais;
        private PaisSO _pais;
        private Texture2D _tex;
        private Sprite _herba;

        [SetUp]
        public void Preparar()
        {
            _go = new GameObject("Decoracio");
            _d = _go.AddComponent<DecoracioDeGespa>();
            _perDefecte = ScriptableObject.CreateInstance<PerfilDeDecoracio>();
            _delPais = ScriptableObject.CreateInstance<PerfilDeDecoracio>();
            _pais = ScriptableObject.CreateInstance<PaisSO>();
            _d.perfil = _perDefecte;
            _tex = new Texture2D(4, 4);
            _herba = Sprite.Create(_tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 100);
        }

        [TearDown]
        public void Netejar()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_perDefecte);
            Object.DestroyImmediate(_delPais);
            Object.DestroyImmediate(_pais);
            Object.DestroyImmediate(_herba);
            Object.DestroyImmediate(_tex);
        }

        [Test]
        public void PaisSensePerfil_ElDeLaDecoracio() => Assert.AreSame(_perDefecte, _d.PerfilPer(_pais));

        [Test]
        public void PaisAmbPerfil_ElSeu()
        {
            _pais.decoracio = _delPais;
            Assert.AreSame(_delPais, _d.PerfilPer(_pais));
        }

        [Test]
        public void SensePais_ElDeLaDecoracio() => Assert.AreSame(_perDefecte, _d.PerfilPer(null));

        [Test]
        public void CadaCapa_ElSeuGrupIOrdre()
        {
            var v = new VariantDeDecoracio { sprite = _herba, pes = 1, escalaMinima = 1, escalaMaxima = 1 };
            _perDefecte.capes.Add(new CapaDeDecoracio { nom = "Gespa", variants = new[] { v }, llavor = 1, densitat = 0.05f, ordreDeDibuix = -100, nomsAEvitar = new string[0] });
            _perDefecte.capes.Add(new CapaDeDecoracio { nom = "Arbres", variants = new[] { v }, llavor = 2, densitat = 0.02f, ordreDeDibuix = -40, nomsAEvitar = new string[0] });

            int posats = _d.Generar(_perDefecte);

            Assert.Greater(posats, 0);
            Transform camp = _d.Contenidor;
            Assert.IsNotNull(camp);
            Assert.AreEqual(-100, camp.Find("Capa: Gespa").GetComponent<SortingGroup>().sortingOrder);
            Assert.AreEqual(-40, camp.Find("Capa: Arbres").GetComponent<SortingGroup>().sortingOrder);
            Assert.AreEqual(posats, _d.Quantitat);
        }

        // La mateixa llavor torna el mateix camp.
        [Test]
        public void MateixaLlavor_MateixCamp()
        {
            var v = new VariantDeDecoracio { sprite = _herba, pes = 1, escalaMinima = 1, escalaMaxima = 1 };
            _perDefecte.capes.Add(new CapaDeDecoracio { nom = "Gespa", variants = new[] { v }, llavor = 7, densitat = 0.05f, nomsAEvitar = new string[0] });

            _d.Generar(_perDefecte);
            Vector3 primer = _d.Contenidor.GetComponentInChildren<SpriteRenderer>().transform.position;
            _d.Generar(_perDefecte);
            Vector3 segon = _d.Contenidor.GetComponentInChildren<SpriteRenderer>().transform.position;
            Assert.AreEqual(primer, segon);
        }
    }
}
