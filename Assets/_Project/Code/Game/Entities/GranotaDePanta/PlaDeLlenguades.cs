using UnityEngine;

namespace IdleFrogs.Game
{
    public struct Llenguada
    {
        public float inici;    // segons des que comença a recollir
        public float durada;
    }

    // Quan llança la llengua la granota mentre recull. La recol·lecció es
    // parteix en trams iguals i cada llenguada acaba just al final del seu:
    // l'última acaba quan acaba la recol·lecció i la granota marxa cap a la
    // capsa amb la llengua ja recollida.
    //
    // Només és visual: el temps total és el de sempre (TempsFinsPle) i
    // l'economia no en depèn.
    public static class PlaDeLlenguades
    {
        // Torna quantes n'hi caben i les escriu a 'sortida'.
        //   temps          — el que dura la recol·lecció
        //   quantes        — les que voldríem
        //   duracio        — una llenguada a velocitat normal
        //   duracioMinima  — més ràpid que això no es veu: se'n fan menys
        public static int Calcular(float temps, int quantes, float duracio, float duracioMinima, Llenguada[] sortida)
        {
            if (sortida == null || temps <= 0 || quantes <= 0 || duracio <= 0) return 0;

            // Amb un encarregat que accelera molt no hi caben totes: abans de
            // fer-les esperpèntiques de ràpides, se'n fan menys.
            int n = Mathf.Min(quantes, sortida.Length);
            while (n > 1 && temps / n < duracioMinima) n--;

            float tram = temps / n;
            if (tram < duracioMinima) return 0;

            float d = Mathf.Min(duracio, tram);
            for (int k = 0; k < n; k++)
            {
                float fi = tram * (k + 1);
                sortida[k] = new Llenguada { inici = fi - d, durada = d };
            }
            return n;
        }
    }
}
