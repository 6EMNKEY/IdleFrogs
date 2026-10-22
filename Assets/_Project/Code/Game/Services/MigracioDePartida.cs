using UnityEngine;

namespace IdleFrogs.Game
{
    // Converteix el JSON guardat a la versió actual de DadesDePartida.
    //
    // Separat de ServeiDeGuardat (que només llegeix i escriu fitxers) perquè
    // es pugui provar sense tocar el disc: rep text i torna dades.
    public static class MigracioDePartida
    {
        public static DadesDePartida Llegir(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            DadesDePartida dades = JsonUtility.FromJson<DadesDePartida>(json);
            if (dades == null) return null;

            if (dades.versio >= 4 && dades.zones != null) return dades;

            // Versió 3 o anterior: una sola zona en camps plans.
            return DesDeV3(JsonUtility.FromJson<DadesDePartidaV3>(json));
        }

        // Les partides d'abans dels països tenien els doblers, el rellotge i
        // l'offline pendent globals: passen a la zona 0, que és on es jugava.
        // Es pot cridar sempre: si ja estan passats, no fa res.
        public static void PassarAPaisos(DadesDePartida d)
        {
            if (d == null) return;
            if (d.paisosDesbloquejats < 1) d.paisosDesbloquejats = 1;
            if (!d.TeZona(0)) return;

            ZonaGuardada z0 = d.zones[0];
            if (d.doblers > 0 && z0.doblers <= 0) z0.doblers = d.doblers;
            if (d.marcaDeTempsUtc != 0 && z0.marcaDeTempsUtc == 0) z0.marcaDeTempsUtc = d.marcaDeTempsUtc;
            if (d.offlinePendent.HiHaRecompensa && !z0.offlinePendent.HiHaRecompensa) z0.offlinePendent = d.offlinePendent;
            d.doblers = 0;
            d.offlinePendent = default;
        }

        public static DadesDePartida DesDeV3(DadesDePartidaV3 v)
        {
            if (v == null) return null;

            var dades = new DadesDePartida
            {
                versio = DadesDePartida.VersioActual,
                marcaDeTempsUtc = v.marcaDeTempsUtc,
                doblers = v.doblers,
                // Abans de la versió 3 no hi havia guia: aquestes partides ja
                // estaven jugades i no se'ls ha d'ensenyar.
                pasDeLaGuia = v.versio < 3 ? int.MaxValue : v.pasDeLaGuia,
                zonaActual = 0
            };

            ZonaGuardada z = dades.Zona(0);
            z.nivellsDePanta = v.nivellsDePanta ?? new int[0];
            z.capsesDePanta = v.capsesDePanta;
            z.barreres = v.barreres;
            z.nivellDeTren = v.nivellDeTren;
            z.carregaDeTren = v.carregaDeTren;
            z.doblersALEstacio = v.doblersALEstacio;
            z.nivellDeTenda = v.nivellDeTenda;
            z.carregaDelTransportista = v.carregaDelTransportista;
            z.encarregats = v.encarregats;
            // Null en partides v2: el gestor el reconstrueix comptant els que tens.
            z.encarregatsCompratsPerTipus = v.encarregatsCompratsPerTipus;

            return dades;
        }
    }
}
