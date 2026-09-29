using System.IO;
using UnityEngine;

namespace IdleFrogs.Game
{
    public static class ServeiDeGuardat
    {
        private static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");

        public static bool Existeix() => File.Exists(Ruta);

        public static void Guardar(DadesDePartida dades)
        {
            // Escrivim a un temporal i després el movem. Si el mòbil mata
            // l'app a mitja escriptura, el fitxer bo encara és sencer.
            string temporal = Ruta + ".tmp";
            try
            {
                File.WriteAllText(temporal, JsonUtility.ToJson(dades, true));
                if (File.Exists(Ruta)) File.Delete(Ruta);
                File.Move(temporal, Ruta);
            }
            catch (IOException e)
            {
                Debug.LogError($"ServeiDeGuardat: no s'ha pogut guardar — {e.Message}");
            }
        }

        public static DadesDePartida Carregar()
        {
            if (!File.Exists(Ruta)) return null;
            try
            {
                return JsonUtility.FromJson<DadesDePartida>(File.ReadAllText(Ruta));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ServeiDeGuardat: partida corrupta, s'ignora — {e.Message}");
                return null;
            }
        }

        public static void Esborrar()
        {
            if (File.Exists(Ruta)) File.Delete(Ruta);
        }
    }
}
