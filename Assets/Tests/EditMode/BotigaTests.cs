using System;
using System.Collections.Generic;
using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // La lògica de la botiga que no necessita escena: multiplicadors,
    // calendari i sortejos.
    public class BotigaTests
    {
        // ---- Multiplicadors ----

        [Test]
        public void SenseMultiplicadors_Factor1()
        {
            var m = new MultiplicadorsTemporals();
            Assert.AreEqual(1, m.Factor);
            Assert.IsNull(m.Actiu);
        }

        // El fort mana; el feble espera amb el temps intacte.
        [Test]
        public void ElMesFortMana_IElAltreEspera()
        {
            var m = new MultiplicadorsTemporals();
            m.Afegir(2, 3600);
            m.Afegir(5, 600);
            Assert.AreEqual(5, m.Factor);

            m.Avancar(600);
            Assert.AreEqual(2, m.Factor);
            Assert.AreEqual(3600, m.Actiu.segonsRestants, 1e-6);
        }

        // El temps que sobra d'un passa al següent (amb el joc tancat, per exemple).
        [Test]
        public void ElTempsSobrantPassaAlSeguent()
        {
            var m = new MultiplicadorsTemporals();
            m.Afegir(2, 3600);
            m.Afegir(5, 600);
            m.Avancar(1200);
            Assert.AreEqual(2, m.Factor);
            Assert.AreEqual(3000, m.Actiu.segonsRestants, 1e-6);

            m.Avancar(99999);
            Assert.AreEqual(1, m.Factor);
            Assert.AreEqual(0, m.Actius.Count);
        }

        // Dos del mateix factor sumen temps, amb el tope si n'hi ha.
        [Test]
        public void MateixFactor_SumaTemps_AmbTope()
        {
            var m = new MultiplicadorsTemporals();
            m.Afegir(2, 1800, 4 * 3600);
            m.Afegir(2, 1800, 4 * 3600);
            Assert.AreEqual(1, m.Actius.Count);
            Assert.AreEqual(3600, m.Actiu.segonsRestants, 1e-6);

            for (int i = 0; i < 10; i++) m.Afegir(2, 1800, 4 * 3600);
            Assert.AreEqual(4 * 3600, m.Actiu.segonsRestants, 1e-6);
        }

        [Test]
        public void GuardarICarregar()
        {
            var m = new MultiplicadorsTemporals();
            m.Afegir(2, 100);
            m.Afegir(1000, 60);
            var n = new MultiplicadorsTemporals();
            n.Carregar(m.Guardar());
            Assert.AreEqual(1000, n.Factor);
            Assert.AreEqual(2, n.Actius.Count);
        }

        // ---- Calendari ----

        private static List<DiaDelCalendari> Dies(int n)
        {
            var l = new List<DiaDelCalendari>();
            for (int i = 0; i < n; i++)
                l.Add(new DiaDelCalendari { recompenses = new List<Recompensa> { new Recompensa { tipus = TipusDeRecompensa.Gemes, quantitat = i + 1 } } });
            return l;
        }

        [Test]
        public void Calendari_PrimerElMes_DespresElCicle()
        {
            var mes = Dies(30);
            var cicle = Dies(7);
            Assert.AreSame(mes[0], Calendari.DelDia(mes, cicle, 0));
            Assert.AreSame(mes[29], Calendari.DelDia(mes, cicle, 29));
            Assert.AreSame(cicle[0], Calendari.DelDia(mes, cicle, 30));
            Assert.AreSame(cicle[6], Calendari.DelDia(mes, cicle, 36));
            Assert.AreSame(cicle[0], Calendari.DelDia(mes, cicle, 37));
        }

        [Test]
        public void Calendari_Numero()
        {
            Assert.AreEqual((1, 30), Calendari.Numero(30, 7, 0));
            Assert.AreEqual((30, 30), Calendari.Numero(30, 7, 29));
            Assert.AreEqual((1, 7), Calendari.Numero(30, 7, 30));
            Assert.AreEqual((3, 7), Calendari.Numero(30, 7, 39));
        }

        // Un cop al dia: avui no, demà sí.
        [Test]
        public void Calendari_UnCopAlDia()
        {
            int avui = Calendari.Dia(new DateTime(2026, 10, 27, 23, 59, 0));
            int dema = Calendari.Dia(new DateTime(2026, 10, 28, 0, 1, 0));
            Assert.IsFalse(Calendari.PotRecollir(avui, avui));
            Assert.IsTrue(Calendari.PotRecollir(avui, dema));
            Assert.IsTrue(Calendari.PotRecollir(0, avui));
        }

        // ---- Sortejos ----

        [Test]
        public void TriarPerPes()
        {
            var a = new Recompensa { pes = 25 };
            var b = new Recompensa { pes = 75 };
            var l = new List<Recompensa> { a, b };
            Assert.AreSame(a, Recompensa.TriarPerPes(l, 0.0));
            Assert.AreSame(a, Recompensa.TriarPerPes(l, 0.249));
            Assert.AreSame(b, Recompensa.TriarPerPes(l, 0.25));
            Assert.AreSame(b, Recompensa.TriarPerPes(l, 0.9999));
        }

        [Test]
        public void TirarRaresaDelCofre()
        {
            var cofre = new Recompensa { tipus = TipusDeRecompensa.Cofre, probabilitats = new[] { 0f, 75f, 22f, 3f } };
            Assert.AreEqual(Raresa.Rar, cofre.TirarRaresa(0.0));
            Assert.AreEqual(Raresa.Rar, cofre.TirarRaresa(0.749));
            Assert.AreEqual(Raresa.Epic, cofre.TirarRaresa(0.75));
            Assert.AreEqual(Raresa.Llegendari, cofre.TirarRaresa(0.98));
            Assert.AreEqual(Raresa.Llegendari, cofre.TirarRaresa(0.99999));
        }
            // ---- Pagar amb gemes el que falta ----

        [Test]
        public void Gemes_TempsAlPreuDeLaBotiga()
        {
            // 10.000 que falten a 10.000/h = 1 h = 60 gemes.
            Assert.AreEqual(60, GestorDeBotiga.CalcularGemes(10000, 10000, 60, 5, 100000));
        }

        [Test]
        public void Gemes_ElMinim()
        {
            Assert.AreEqual(5, GestorDeBotiga.CalcularGemes(10, 100000, 60, 5, 100000));
        }

        // Sense ritme no se sap quant val el temps: no s'ofereix.
        [Test]
        public void Gemes_SenseRitme_NoSOfereix()
        {
            Assert.AreEqual(0, GestorDeBotiga.CalcularGemes(1e6, 0, 60, 5, 100000));
            Assert.AreEqual(0, GestorDeBotiga.CalcularGemes(1e6, double.NaN, 60, 5, 100000));
            Assert.AreEqual(0, GestorDeBotiga.CalcularGemes(1e6, double.PositiveInfinity, 60, 5, 100000));
        }

        // El que abans desbordava l'int i sortia a 1 gema.
        [Test]
        public void Gemes_MassaCar_NoSOfereix()
        {
            Assert.AreEqual(0, GestorDeBotiga.CalcularGemes(1e26, 18000, 60, 5, 100000));
            Assert.AreEqual(0, GestorDeBotiga.CalcularGemes(1e300, 1e-300, 60, 5, 100000));
        }

        [Test]
        public void Gemes_NoFaltaRes() => Assert.AreEqual(0, GestorDeBotiga.CalcularGemes(0, 1000, 60, 5, 100000));
    }
}
