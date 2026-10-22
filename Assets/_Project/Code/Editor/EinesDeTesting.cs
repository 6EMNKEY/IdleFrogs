using UnityEditor;
using UnityEngine;
using IdleFrogs.Game;
using IdleFrogs.UI;

namespace IdleFrogs.EditorTools
{
    // Menú: IdleFrogs > Testing > ...
    //
    // Doblers i gemes de franc per provar, només amb el joc en marxa: fora
    // de Play no hi ha CurrencyManager i les opcions surten en gris. Passen
    // pel CurrencyManager com qualsevol ingrés, així que els textos i els
    // panells s'actualitzen sols i l'autoguardat se'ls queda. Si hi ha
    // EfecteDeRecompensa a l'escena, volen des del centre de la pantalla.
    public static class EinesDeTesting
    {
        private const string Menu = "IdleFrogs/Testing/";
        private const int Prioritat = 1200;

        [MenuItem(Menu + "+1K doblers", priority = Prioritat)]
        private static void Mil() => Afegir(1e3);

        [MenuItem(Menu + "+1M doblers", priority = Prioritat + 1)]
        private static void Milio() => Afegir(1e6);

        [MenuItem(Menu + "+1B doblers", priority = Prioritat + 2)]
        private static void Bilio() => Afegir(1e9);

        [MenuItem(Menu + "+1T doblers", priority = Prioritat + 3)]
        private static void Trilio() => Afegir(1e12);

        // Per quan ja vas molt amunt i les quantitats fixes es queden curtes.
        [MenuItem(Menu + "Doblers ×10 %#m", priority = Prioritat + 4)]
        private static void PerDeu() => Afegir(System.Math.Max(10, CurrencyManager.Instancia.doblersActuals * 9));

        [MenuItem(Menu + "+100 gemes", priority = Prioritat + 20)]
        private static void Gemes()
        {
            CurrencyManager.Instancia.AfegirGemes(100);
            Efecte(TipusDeMoneda.Gemes, 100);
            Debug.Log($"IdleFrogs: +100 gemes → {CurrencyManager.Instancia.gemesActuals}");
        }

        [MenuItem(Menu + "+1K doblers", true)]
        [MenuItem(Menu + "+1M doblers", true)]
        [MenuItem(Menu + "+1B doblers", true)]
        [MenuItem(Menu + "+1T doblers", true)]
        [MenuItem(Menu + "Doblers ×10 %#m", true)]
        [MenuItem(Menu + "+100 gemes", true)]
        private static bool EnJoc() => Application.isPlaying && CurrencyManager.Instancia != null;

        private static void Efecte(TipusDeMoneda tipus, double quantitat)
        {
            if (EfecteDeRecompensa.Instancia != null)
                EfecteDeRecompensa.Instancia.Llancar(tipus, new Vector2(Screen.width / 2f, Screen.height / 2f), quantitat);
        }

        // ---- Offline sense esperar: com si haguessis tancat el joc ----

        [MenuItem(Menu + "Offline: 10 minuts fora", priority = Prioritat + 60)]
        private static void Offline10m() => Fora(600);

        [MenuItem(Menu + "Offline: 1 hora fora", priority = Prioritat + 61)]
        private static void Offline1h() => Fora(3600);

        [MenuItem(Menu + "Offline: 10 hores fora", priority = Prioritat + 62)]
        private static void Offline10h() => Fora(36000);

        private static void Fora(double segons)
        {
            GameManager.Instancia.SimularTempsFora(segons);
            Debug.Log($"IdleFrogs: simulat {FormatadorDeNombres.Temps(segons)} fora " +
                      $"(idle {FormatadorDeNombres.Ritme(GameManager.Instancia.IngresIdlePerSegon())})");
        }

        [MenuItem(Menu + "Offline: 10 minuts fora", true)]
        [MenuItem(Menu + "Offline: 1 hora fora", true)]
        [MenuItem(Menu + "Offline: 10 hores fora", true)]
        private static bool PotSimularOffline() => Application.isPlaying && GameManager.Instancia != null;

        // ---- Barreres: el temporitzador s'acaba ara ----

        [MenuItem(Menu + "Barreres: acabar les que estan en marxa", priority = Prioritat + 80)]
        private static void AcabarBarreres()
        {
            int obertes = 0;
            foreach (Barrera b in Object.FindObjectsByType<Barrera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b.EstaOberta || !b.EstaIniciada) continue;
                b.ReduirTemps(b.SegonsRestants + 1);
                obertes++;
            }
            Debug.Log(obertes > 0
                ? $"IdleFrogs: {obertes} barrera(es) oberta(es)"
                : "IdleFrogs: cap barrera en marxa. Primer cal iniciar-ne una (tocar-la quan hi tens accés).");
        }

        [MenuItem(Menu + "Barreres: acabar les que estan en marxa", true)]
        private static bool HiHaJoc() => Application.isPlaying;

        // Les tres capacitats de la cadena, el coll d'ampolla i l'idle.
        [MenuItem(Menu + "Mesures de la cadena", priority = Prioritat + 40)]
        private static void Mesures()
        {
            MesuresDeLaCadena m = GameManager.Instancia.Mesures();
            MesuresDeLaCadena sol = GameManager.Instancia.Mesures(nomesAutomatitzat: true);
            Debug.Log("IdleFrogs: mesures de la cadena (entre parèntesis, només el que va amb encarregat)\n" +
                      $"  Pantans: {FormatadorDeNombres.Ritme(m.pantans)} ({FormatadorDeNombres.Ritme(sol.pantans)})\n" +
                      $"  Tren:    {FormatadorDeNombres.Ritme(m.tren)} ({FormatadorDeNombres.Ritme(sol.tren)})\n" +
                      $"  Tenda:   {FormatadorDeNombres.Ritme(m.tenda)} ({FormatadorDeNombres.Ritme(sol.tenda)})\n" +
                      $"  Coll d'ampolla: {m.QuinTram} ({FormatadorDeNombres.Ritme(m.CollDAmpolla)})\n" +
                      $"  Idle (només automàtic): {FormatadorDeNombres.Ritme(sol.Idle)}");
        }

        [MenuItem(Menu + "Mesures de la cadena", true)]
        private static bool HiHaPartida() => Application.isPlaying && GameManager.Instancia != null;

        private static void Afegir(double quantitat)
        {
            CurrencyManager.Instancia.AfegirDoblers(quantitat);
            Efecte(TipusDeMoneda.Doblers, quantitat);
            Debug.Log($"IdleFrogs: +{FormatadorDeNombres.Doblers(quantitat)} doblers → " +
                      FormatadorDeNombres.Doblers(CurrencyManager.Instancia.doblersActuals));
        }
    }
}
