using System;
using System.Collections.Generic;
using NUnit.Framework;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // Els combats, els cicles i el gacha de l'arena, sense escena.
    public class ArenaTests
    {
        private static TaulaDeLArena Taula(double hores = 24)
        {
            var r = new DadesDeRaresa[6];
            for (int i = 0; i < 6; i++)
                r[i] = new DadesDeRaresa
                {
                    raresa = (RaresaDeLluita)i,
                    vidaDelBoss = 1000 * (i + 1),
                    regeneracio = i,
                    punts = i + 1,
                    probabilitatDelBoss = 1,
                    danyBase = 10,
                    creixementDelDany = 2,
                    probabilitatComu = i < 4 ? 1 : 0,
                    probabilitatPremium = 1,
                };
            return new TaulaDeLArena(r, hores);
        }

        private static ArenaGuardada Arena(DateTime inici)
        {
            var a = new ArenaGuardada
            {
                desbloquejada = true,
                iniciDelCicleUtc = inici.ToBinary(),
                actualitzadaUtc = inici.ToBinary(),
            };
            a.lluitadors.Add(new LluitadorGuardat { id = 1, raresa = 0, nivell = 1 });
            return a;
        }

        // ---- Combat ----

        [Test]
        public void SenseRegeneracio_MorEnVidaEntreDany()
        {
            Assert.AreEqual(100, CombatDeLArena.SegonsPerMatar(1000, 10, 0), 1e-9);
        }

        [Test]
        public void AmbRegeneracio_TardaMes()
        {
            Assert.AreEqual(200, CombatDeLArena.SegonsPerMatar(1000, 10, 5), 1e-9);
        }

        [Test]
        public void DanyNoSuperaLaRegeneracio_Impossible()
        {
            Assert.IsTrue(double.IsPositiveInfinity(CombatDeLArena.SegonsPerMatar(1000, 5, 5)));
            Assert.IsTrue(double.IsPositiveInfinity(CombatDeLArena.SegonsPerMatar(1000, 0, 0)));
        }

        [Test]
        public void SenseLluitador_ElBossEsRecuperaFinsAlMaxim()
        {
            Assert.AreEqual(1000, CombatDeLArena.VidaDespres(500, 1000, 0, 2, 1000), 1e-9);
            Assert.AreEqual(600, CombatDeLArena.VidaDespres(500, 1000, 0, 2, 50), 1e-9);
        }

        [Test]
        public void ElDanyCreixAmbElNivell()
        {
            var d = new DadesDeRaresa { danyBase = 3, creixementDelDany = 2 };
            Assert.AreEqual(3, d.Dany(1), 1e-9);
            Assert.AreEqual(24, d.Dany(4), 1e-9);
        }

        [Test]
        public void ElCostPujaDePressaFinsAlCanviIDespresAPocAPoc()
        {
            var d = new DadesDeRaresa { costBase = 10, creixementDelCost = 3, nivellDeCanvi = 3, creixementDespres = 1.5 };
            Assert.AreEqual(10, d.CostDeMillora(1), 1e-9);
            Assert.AreEqual(90, d.CostDeMillora(3), 1e-9);
            Assert.AreEqual(135, d.CostDeMillora(4), 1e-9);
        }

        // ---- Avançar ----

        [Test]
        public void MorDinsDelCicle_DonaPuntsUnSolCop()
        {
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ArenaGuardada a = Arena(t0);
            a.bosses.Add(new BossALArena { idBoss = "D", raresa = 0, vida = 1000, vidaMaxima = 1000, lluitador = 1 });
            int morts = 0;
            // 10 de dany, 0 de regeneració: 100 s.
            ModelDeLArena.Avancar(a, t0.AddSeconds(150), Taula(), id => 10, b => morts++, null);
            Assert.IsTrue(a.bosses[0].mort);
            Assert.AreEqual(1, morts);
            ModelDeLArena.Avancar(a, t0.AddSeconds(500), Taula(), id => 10, b => morts++, null);
            Assert.AreEqual(1, morts);
        }

        [Test]
        public void AMitges_ElBossConservaLaVida()
        {
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ArenaGuardada a = Arena(t0);
            a.bosses.Add(new BossALArena { idBoss = "C", raresa = 1, vida = 1000, vidaMaxima = 1000, lluitador = 1 });
            // 10 de dany − 1 de regeneració = 9/s; 50 s → 550.
            ModelDeLArena.Avancar(a, t0.AddSeconds(50), Taula(), id => 10, null, null);
            Assert.IsFalse(a.bosses[0].mort);
            Assert.AreEqual(550, a.bosses[0].vida, 1e-6);
        }

        [Test]
        public void EnAcabarElCicle_SurtenBossesNousIEsPerdElQueQuedava()
        {
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ArenaGuardada a = Arena(t0);
            a.bosses.Add(new BossALArena { idBoss = "vell", raresa = 5, vida = 6000, vidaMaxima = 6000, lluitador = 1 });
            int cicles = 0;
            TaulaDeLArena t = Taula(1);
            ModelDeLArena.Avancar(a, t0.AddHours(1.5), t, id => 0, null, (ar, inici) =>
            {
                cicles++;
                ModelDeLArena.BossesNous(ar, t, new Random(1), (r, rr) => "nou");
            });
            Assert.AreEqual(1, cicles);
            Assert.AreEqual(3, a.bosses.Count);
            Assert.AreEqual("nou", a.bosses[0].idBoss);
            // El lluitador es queda al seu nenúfar.
            Assert.AreEqual(1, a.bosses[0].lluitador);
            Assert.AreEqual(t0.AddHours(1), DateTime.FromBinary(a.iniciDelCicleUtc));
        }

        [Test]
        public void DiesFora_CadaCicleEsLluitaSencer()
        {
            DateTime t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ArenaGuardada a = Arena(t0);
            TaulaDeLArena t = Taula(24);
            ModelDeLArena.BossesNous(a, t, new Random(3), (r, rr) => "b");
            // Un lluitador a cada nenúfar: es queden d'un cicle al següent.
            foreach (BossALArena b in a.bosses) b.lluitador = 1;
            int morts = 0;
            // Dany enorme: mor tot. 5 dies = 5 cicles complets (el 6è acaba de començar).
            ModelDeLArena.Avancar(a, t0.AddDays(5).AddHours(1), t, id => 1e9, b => morts++,
                (ar, inici) => ModelDeLArena.BossesNous(ar, t, new Random(inici.Day), (r, rr) => "b"));
            Assert.AreEqual(18, morts);
        }

        // ---- Gacha ----

        [Test]
        public void TicketComu_MaiNoDonaSNiSSS()
        {
            TaulaDeLArena t = Taula();
            for (int i = 0; i < 1000; i++)
            {
                RaresaDeLluita r = ModelDeLArena.TirarRaresa(t, false, 0, i / 1000.0);
                Assert.Less((int)r, (int)RaresaDeLluita.S);
            }
        }

        [Test]
        public void TicketPremium_PotDonarSSS()
        {
            Assert.AreEqual(RaresaDeLluita.SSS, ModelDeLArena.TirarRaresa(Taula(), true, 0, 0.999));
        }

        [Test]
        public void LaSort_FaMesProbablesLesRaresesAltes()
        {
            TaulaDeLArena t = Taula();
            int senseSort = 0, ambSort = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (ModelDeLArena.TirarRaresa(t, true, 0, i / 1000.0) >= RaresaDeLluita.S) senseSort++;
                if (ModelDeLArena.TirarRaresa(t, true, 1, i / 1000.0) >= RaresaDeLluita.S) ambSort++;
            }
            Assert.Greater(ambSort, senseSort);
        }

        [Test]
        public void PantaDe_TrobaElNenufar()
        {
            ArenaGuardada a = Arena(DateTime.UtcNow);
            a.bosses.Add(new BossALArena());
            a.bosses.Add(new BossALArena { lluitador = 1 });
            Assert.AreEqual(1, ModelDeLArena.PantaDe(a, 1));
            Assert.AreEqual(-1, ModelDeLArena.PantaDe(a, 7));
        }
    }
}
