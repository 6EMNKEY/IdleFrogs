using UnityEngine;

namespace IdleFrogs.Game
{
    // Abans de carregar la primera escena posa els sistemes que viuen fora de
    // les escenes: Resources/Sistemes (GameManager, monedes, botiga, àudio,
    // països) i Resources/UIGlobal (confirmació, botiga, ajustos...). Així
    // existeixen tant si comences pel país com pel mapa, i no es perden en
    // canviar d'escena.
    public static class Arrencada
    {
        private static readonly string[] Prefabs = { "Sistemes", "UIGlobal" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Crear()
        {
            foreach (string nom in Prefabs)
            {
                GameObject prefab = Resources.Load<GameObject>(nom);
                if (prefab == null)
                {
                    Debug.LogWarning($"Arrencada: no hi ha Resources/{nom}. Els seus sistemes han de ser a l'escena.");
                    continue;
                }
                GameObject instancia = Object.Instantiate(prefab);
                instancia.name = nom;
                Object.DontDestroyOnLoad(instancia);
            }
        }
    }
}
