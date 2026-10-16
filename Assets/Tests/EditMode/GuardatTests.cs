using NUnit.Framework;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.Tests.EditMode
{
    // El format del guardat i la migració de partides velles. Tot passa per
    // text, sense tocar el fitxer de partida de debò.
    public class GuardatTests
    {
        // Una partida de la versió 2: una sola zona, comptador global
        // d'encarregats i sense guia.
        private const string JsonV2 = @"{
            ""versio"": 2, ""marcaDeTempsUtc"": 123, ""doblers"": 860.0,
            ""nivellsDePanta"": [1, 1, 1, 1, 1], ""capsesDePanta"": [0.0, 8450.0, 0.0, 0.0, 0.0],
            ""barreres"": [{ ""iniciada"": false, ""oberta"": false, ""vencimentUtc"": 0 }],
            ""nivellDeTren"": 3, ""carregaDeTren"": 0.0, ""doblersALEstacio"": 400.0,
            ""nivellDeTenda"": 2, ""carregaDelTransportista"": 0.0,
            ""encarregats"": [], ""encarregatsComprats"": 4 }";

        private const string JsonV3 = @"{
            ""versio"": 3, ""doblers"": 10.0, ""nivellsDePanta"": [2, 0],
            ""encarregatsCompratsPerTipus"": [1, 0, 2], ""pasDeLaGuia"": 2 }";

        [Test]
        public void V2_EsConverteixEnLaZona0()
        {
            DadesDePartida d = MigracioDePartida.Llegir(JsonV2);

            Assert.AreEqual(DadesDePartida.VersioActual, d.versio);
            Assert.AreEqual(860, d.doblers);
            Assert.AreEqual(123, d.marcaDeTempsUtc);
            Assert.IsTrue(d.TeZona(0));

            ZonaGuardada z = d.zones[0];
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1 }, z.nivellsDePanta);
            Assert.AreEqual(8450, z.capsesDePanta[1]);
            Assert.AreEqual(3, z.nivellDeTren);
            Assert.AreEqual(2, z.nivellDeTenda);
            Assert.AreEqual(400, z.doblersALEstacio);
            Assert.AreEqual(1, z.barreres.Length);
        }

        // Abans de la versió 3 no hi havia guia: aquelles partides ja estaven
        // jugades i no se'ls ha d'ensenyar.
        [Test]
        public void V2_NoVeuLaGuia() =>
            Assert.AreEqual(int.MaxValue, MigracioDePartida.Llegir(JsonV2).pasDeLaGuia);

        [Test]
        public void V3_ConservaLaGuiaIElsComptadors()
        {
            DadesDePartida d = MigracioDePartida.Llegir(JsonV3);
            Assert.AreEqual(2, d.pasDeLaGuia);
            CollectionAssert.AreEqual(new[] { 1, 0, 2 }, d.zones[0].encarregatsCompratsPerTipus);
        }

        // El comptador global de la v2 no es converteix: el gestor el
        // reconstrueix comptant els encarregats que tens.
        [Test]
        public void V2_ComptadorsPerTipusQuedenNull() =>
            Assert.IsNull(MigracioDePartida.Llegir(JsonV2).zones[0].encarregatsCompratsPerTipus);

        [Test]
        public void V4_IdaITornada_ConservaTotesLesZones()
        {
            var d = new DadesDePartida { doblers = 55, pasDeLaGuia = 3, zonaActual = 1 };
            d.Zona(0).nivellsDePanta = new[] { 10, 2 };
            d.Zona(1).nivellsDePanta = new[] { 1 };
            d.Zona(1).nivellDeTren = 7;

            DadesDePartida tornada = MigracioDePartida.Llegir(JsonUtility.ToJson(d));

            Assert.AreEqual(55, tornada.doblers);
            Assert.AreEqual(1, tornada.zonaActual);
            Assert.AreEqual(2, tornada.zones.Length);
            CollectionAssert.AreEqual(new[] { 10, 2 }, tornada.zones[0].nivellsDePanta);
            Assert.AreEqual(7, tornada.zones[1].nivellDeTren);
        }

        // Tocar una zona no ha d'esborrar mai les altres.
        [Test]
        public void EscriureUnaZona_NoTocaLesAltres()
        {
            var d = new DadesDePartida();
            d.Zona(0).nivellsDePanta = new[] { 5 };
            d.Zona(2).nivellsDePanta = new[] { 9 };

            CollectionAssert.AreEqual(new[] { 5 }, d.zones[0].nivellsDePanta);
            Assert.AreEqual(3, d.zones.Length);
        }

        // Una zona que només existeix perquè n'hi ha una de posterior no
        // compta com a jugada.
        [Test]
        public void ZonaBuidaEntremig_NoCompta()
        {
            var d = new DadesDePartida();
            d.Zona(2).nivellsDePanta = new[] { 1 };

            Assert.IsFalse(d.TeZona(1));
            Assert.IsTrue(d.TeZona(2));
            Assert.IsFalse(d.TeZona(7));
            Assert.IsFalse(d.TeZona(-1));
        }

        // JsonUtility guarda els arrays null com a []: una zona de farciment no
        // pot passar a semblar jugada després de guardar i carregar.
        [Test]
        public void ZonaDeFarciment_SegueixSenseComptarDespresDeGuardar()
        {
            var d = new DadesDePartida();
            d.Zona(2).nivellsDePanta = new[] { 1 };

            DadesDePartida tornada = MigracioDePartida.Llegir(JsonUtility.ToJson(d));

            Assert.IsFalse(tornada.TeZona(0));
            Assert.IsFalse(tornada.TeZona(1));
            Assert.IsTrue(tornada.TeZona(2));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TextBuit_TornaNull(string json) => Assert.IsNull(MigracioDePartida.Llegir(json));
    }
}
