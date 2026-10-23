using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum Idioma { English, Espanol, Catala }

    // Un text en els tres idiomes, per a les dades (el nom d'un país...).
    [Serializable]
    public struct TextTraduit
    {
        public string angles;
        public string castella;
        public string catala;

        // Si falta la traducció, l'anglès; si tampoc n'hi ha, el que hi hagi.
        public string Text
        {
            get
            {
                string t = Textos.Actual == Idioma.Espanol ? castella
                         : Textos.Actual == Idioma.Catala ? catala : angles;
                if (!string.IsNullOrEmpty(t)) return t;
                if (!string.IsNullOrEmpty(angles)) return angles;
                return !string.IsNullOrEmpty(catala) ? catala : castella ?? string.Empty;
            }
        }
    }

    // Tots els textos que el codi ensenya al jugador, en anglès (la base),
    // castellà i català. L'idioma es desa a PlayerPrefs: és de qui juga, no
    // de la partida, i no s'esborra en reiniciar.
    //
    // Els textos fixos de l'escena (UPGRADE, Hire Boss...) també surten
    // d'aquí, amb el component TextLocalitzat i la seva clau.
    public static class Textos
    {
        private const string ClauDePreferencies = "IdleFrogs.Idioma";
        private static bool _carregat;
        private static Idioma _actual = Idioma.English;

        public static event Action EnCanviDIdioma;

        public static Idioma Actual
        {
            get
            {
                if (!_carregat)
                {
                    _carregat = true;
                    int i = PlayerPrefs.GetInt(ClauDePreferencies, (int)Idioma.English);
                    _actual = Enum.IsDefined(typeof(Idioma), i) ? (Idioma)i : Idioma.English;
                }
                return _actual;
            }
        }

        // Com es diu cada idioma en el seu propi idioma (per al desplegable).
        public static readonly string[] NomsDelsIdiomes = { "English", "Español", "Català" };

        public static void Canviar(Idioma idioma)
        {
            if (_carregat && idioma == _actual) return;
            _carregat = true;
            _actual = idioma;
            PlayerPrefs.SetInt(ClauDePreferencies, (int)idioma);
            PlayerPrefs.Save();
            EnCanviDIdioma?.Invoke();
        }

        // Per als tests: canvia l'idioma sense desar-lo a les preferències.
        public static void EstablirPerAProves(Idioma idioma)
        {
            _carregat = true;
            _actual = idioma;
        }

        // El text d'una clau. Si la clau no existeix, la mateixa clau: es veu
        // de seguida que falta.
        public static string T(string clau)
        {
            if (clau != null && Taula.TryGetValue(clau, out string[] t))
            {
                string s = t[(int)Actual];
                return string.IsNullOrEmpty(s) ? t[0] : s;
            }
            return clau ?? string.Empty;
        }

        public static string F(string clau, params object[] valors) => string.Format(T(clau), valors);

        public static bool Existeix(string clau) => clau != null && Taula.ContainsKey(clau);
        public static IEnumerable<KeyValuePair<string, string[]>> Totes => Taula;

        //                                    English                     Español                       Català
        private static readonly Dictionary<string, string[]> Taula = new Dictionary<string, string[]>
        {
            // ---- Unitats ----
            ["unitat.panta"]          = new[] { "Swamp {0}",                "Pantano {0}",                "Pantà {0}" },
            ["unitat.tren"]           = new[] { "Train & station",          "Tren y estación",            "Tren i estació" },
            ["unitat.restaurant"]     = new[] { "Restaurant",               "Restaurante",                "Restaurant" },
            ["titol.panta"]           = new[] { "Swamp stats",              "Estadísticas del pantano",   "Estadístiques del pantà" },
            ["titol.tren"]            = new[] { "Train stats",              "Estadísticas del tren",      "Estadístiques del tren" },
            ["titol.restaurant"]      = new[] { "Restaurant stats",         "Estadísticas del restaurante", "Estadístiques del restaurant" },

            // ---- Estadístiques ----
            ["stat.produccio_total"]  = new[] { "Total production",         "Producción total",           "Producció total" },
            ["stat.transport_total"]  = new[] { "Total transport",          "Transporte total",           "Transport total" },
            ["stat.venda_total"]      = new[] { "Total sales",              "Ventas totales",             "Venda total" },
            ["stat.velocitat"]        = new[] { "Speed",                    "Velocidad",                  "Velocitat" },
            ["stat.granotes"]         = new[] { "Frogs",                    "Ranas",                      "Nombre de granotes" },
            ["stat.recollecio"]       = new[] { "Catching speed",           "Velocidad de captura",       "Velocitat recol·lecció" },
            ["stat.capacitat"]        = new[] { "Capacity",                 "Capacidad",                  "Capacitat" },
            ["stat.carrega"]          = new[] { "Loading & unloading",      "Carga y descarga",           "Càrrega i descàrrega" },
            ["stat.clients"]          = new[] { "Customers",                "Clientes",                   "Clients" },
            ["stat.per_client"]       = new[] { "Per customer",             "Por cliente",                "Per client" },
            ["stat.temps_menjar"]     = new[] { "Eating time",              "Tiempo de comida",           "Temps de menjar" },

            // ---- Panell de millora ----
            ["millora.diners"]        = new[] { "Cash: {0}",                "Dinero: {0}",                "Diners: {0}" },
            ["millora.max"]           = new[] { "MAX",                      "MÁX",                        "MÀX" },
            ["millora.nivell"]        = new[] { "Level {0}/{1}",            "Nivel {0}/{1}",              "Nivell {0}/{1}" },
            ["millora.boost"]         = new[] { "Boost at lvl {0}",         "Mejora al nivel {0}",        "Millora al nivell {0}" },
            ["millora.tots_boosts"]   = new[] { "All boosts reached",       "Todas las mejoras logradas", "Totes les millores assolides" },

            // ---- Encarregats ----
            ["enc.cap"]               = new[] { "No boss",                  "Sin encargado",              "Sense encarregat" },
            ["enc.durada"]            = new[] { "Effect: {0}",              "Efecto: {0}",                "Efecte: {0}" },
            ["enc.recarrega"]         = new[] { "Recharge: {0}",            "Recarga: {0}",               "Recàrrega: {0}" },
            ["enc.passiu"]            = new[] { "Passive: {0}",             "Pasiva: {0}",                "Passiva: {0}" },
            ["enc.actiu"]             = new[] { "Active: {0}",              "Activa: {0}",                "Activa: {0}" },
            ["enc.contractat"]        = new[] { "You hired {0} · {1}",      "Has contratado a {0} · {1}", "Has contractat {0} · {1}" },
            ["raresa.comu"]           = new[] { "Common",                   "Común",                      "Comú" },
            ["raresa.rar"]            = new[] { "Rare",                     "Raro",                       "Rar" },
            ["raresa.epic"]           = new[] { "Epic",                     "Épico",                      "Èpic" },
            ["raresa.llegendari"]     = new[] { "Legendary",                "Legendario",                 "Llegendari" },
            ["eix.velocitat"]         = new[] { "Speed",                    "Velocidad",                  "Velocitat" },
            ["eix.velocitat.curt"]    = new[] { "Spd",                      "Vel",                        "Vel" },
            ["eix.recollir"]          = new[] { "Catching speed",           "Velocidad de captura",       "Velocitat de recol·lecció" },
            ["eix.recollir.curt"]     = new[] { "Catch",                    "Captura",                    "Recol" },
            ["eix.carregar"]          = new[] { "Loading speed",            "Velocidad de carga",         "Velocitat de càrrega" },
            ["eix.carregar.curt"]     = new[] { "Load",                     "Carga",                      "Càrrega" },
            ["eix.menjar"]            = new[] { "Eating speed",             "Velocidad al comer",         "Velocitat de menjar" },
            ["eix.menjar.curt"]       = new[] { "Eat",                      "Comer",                      "Menjar" },
            ["eix.capacitat"]         = new[] { "Capacity",                 "Capacidad",                  "Capacitat" },
            ["eix.capacitat.curt"]    = new[] { "Cap",                      "Cap",                        "Cap" },
            ["eix.per_client"]        = new[] { "Cash per customer",        "Dinero por cliente",         "Diners per client" },
            ["eix.per_client.curt"]   = new[] { "Cust",                     "Cliente",                    "Client" },
            ["eix.cost"]              = new[] { "Upgrade cost",             "Coste de mejora",            "Cost de millora" },
            ["eix.cost.curt"]         = new[] { "cost",                     "coste",                      "cost" },

            // ---- Mapa ----
            ["mapa.abans"]            = new[] { "Unlock {0} first",         "Desbloquea {0} antes",       "Desbloqueja {0} abans" },
            ["mapa.anterior"]         = new[] { "the previous one",         "el anterior",                "l'anterior" },
            ["mapa.falten"]           = new[] { "You need {0} more",        "Te faltan {0}",              "Et falten {0}" },
            ["mapa.confirmar"]        = new[] { "Unlock {0} for {1}?",      "¿Desbloquear {0} por {1}?",  "Vols desbloquejar {0} per {1}?" },

            // ---- Món ----
            ["mon.nivell"]            = new[] { "Lvl {0}",                  "Nv. {0}",                    "Nivell {0}" },
            ["mon.desbloquejar"]      = new[] { "Unlock",                   "Desbloquear",                "Desbloquejar" },
            ["mon.comprar"]           = new[] { "Buy\n{0}",                 "Comprar\n{0}",               "Comprar\n{0}" },

            // ---- Guia inicial ----
            ["guia.comprar"]          = new[] { "Tap the sign to buy your first swamp.", "Toca el cartel para comprar tu primer pantano.", "Toca el cartell per comprar el teu primer pantà." },
            ["guia.granota"]          = new[] { "Tap a frog to catch flies.",            "Toca una rana para cazar moscas.",              "Toca una granota perquè caci mosques." },
            ["guia.tren"]             = new[] { "Tap the train to collect the flies.",   "Toca el tren para recoger las moscas.",         "Toca el tren per recollir les mosques." },
            ["guia.client"]           = new[] { "Tap a customer to let them in.",        "Toca un cliente para que entre.",               "Toca un client perquè entri." },
            ["guia.estalvia"]         = new[] { "Save {0} to {1}.",                      "Ahorra {0} para {1}.",                          "Estalvia {0} per {1}." },
            ["guia.per_millorar"]     = new[] { "upgrade the swamp",                     "mejorar el pantano",                            "millorar el pantà" },
            ["guia.per_contractar"]   = new[] { "hire a manager",                        "contratar un encargado",                        "contractar un encarregat" },
            ["guia.millora"]          = new[] { "Tap the level button to upgrade.",      "Toca el botón de nivel para mejorar.",          "Toca el botó de nivell per millorar." },
            ["guia.millora.espera"]   = new[] { "Tap Upgrade.",                          "Toca Mejorar.",                                 "Toca Millorar." },
            ["guia.nivell"]           = new[] { "Reach level {0} to unlock managers.",   "Llega al nivel {0} para desbloquear encargados.", "Arriba al nivell {0} per desbloquejar encarregats." },
            ["guia.nivell.espera"]    = new[] { "Tap Upgrade. Try x10!",                 "Toca Mejorar. ¡Prueba x10!",                    "Toca Millorar. Prova x10!" },
            ["guia.contractar"]       = new[] { "Tap the silhouette to hire a manager.", "Toca la silueta para contratar un encargado.",  "Toca la silueta per contractar un encarregat." },
            ["guia.contractar.espera"]= new[] { "Tap Hire.",                             "Toca Contratar.",                               "Toca Contractar." },
            ["guia.assignar"]         = new[] { "Tap the silhouette to assign your manager.", "Toca la silueta para asignar tu encargado.", "Toca la silueta per assignar el teu encarregat." },
            ["guia.assignar.espera"]  = new[] { "Select your manager and tap Assign.",   "Elige tu encargado y toca Asignar.",            "Tria el teu encarregat i toca Assignar." },
            ["guia.final"]            = new[] { "It runs by itself now! Managers keep earning while you're away.", "¡Ya va solo! Los encargados siguen ganando aunque no estés.", "Ja va sol! Els encarregats guanyen fins i tot quan no hi ets." },

            // ---- Configuració ----
            ["config.titol"]          = new[] { "Settings",                 "Ajustes",                    "Configuració" },
            ["config.idioma"]         = new[] { "Language",                 "Idioma",                     "Idioma" },

            // ---- Textos fixos de l'escena (TextLocalitzat) ----
            ["ui.upgrade"]            = new[] { "UPGRADE",                  "MEJORAR",                    "MILLORAR" },
            ["ui.millora"]            = new[] { "Upgrade",                  "Mejorar",                    "Millorar" },
            ["ui.bosses"]             = new[] { "Bosses",                   "Encargados",                 "Encarregats" },
            ["ui.boss_actual"]        = new[] { "Current boss:",            "Encargado actual:",          "Encarregat actual:" },
            ["ui.no_assignats"]       = new[] { "Unassigned managers",      "Encargados sin asignar",     "Encarregats no assignats" },
            ["ui.contractar"]         = new[] { "Hire Boss",                "Contratar",                  "Contractar" },
            ["ui.assignar"]           = new[] { "ASSIGN SELECTED BOSS",     "ASIGNAR ENCARGADO",          "ASSIGNAR ENCARREGAT" },
            ["ui.treure"]             = new[] { "UNASSIGN",                 "QUITAR",                     "TREURE" },
            ["ui.offline"]            = new[] { "Offline Progress!!",       "¡¡Progreso offline!!",       "Progrés offline!!" },
            ["ui.guanyat"]            = new[] { "Total money earned",       "Dinero total ganado",        "Diners totals guanyats" },
            ["ui.acceptar"]           = new[] { "ACCEPT",                   "ACEPTAR",                    "ACCEPTAR" },
            ["ui.comprar"]            = new[] { "Buy",                      "Comprar",                    "Comprar" },
            ["ui.si"]                 = new[] { "Yes",                      "Sí",                         "Sí" },
            ["ui.cancellar"]          = new[] { "Cancel",                   "Cancelar",                   "Cancel·lar" },
            ["ui.saltar"]             = new[] { "Skip",                     "Saltar",                     "Saltar" },
            ["ui.mapa"]               = new[] { "Map",                      "Mapa",                       "Mapa" },
        };
    }
}
