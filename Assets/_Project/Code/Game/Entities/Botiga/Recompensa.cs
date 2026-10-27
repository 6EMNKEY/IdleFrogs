using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum TipusDeRecompensa
    {
        Gemes,              // quantitat = gemes
        TempsDIngressos,    // quantitat = minuts del ritme de la cadena del país on ets
        Multiplicador,      // multiplicador × durant 'minuts'
        Encarregat,         // un de la raresa, d'un lloc a l'atzar
        Cofre,              // un encarregat amb la raresa tirada amb 'probabilitats'
        Permanent,          // efectePermanent, per sempre
        PaseMensual         // quantitat = gemes cada dia durant 'dies'
    }

    public enum EfectePermanent { X2Ingressos, SenseAnuncis, OfflineX2 }

    // Una cosa que dona la botiga, el calendari o el regal. Els diners van en
    // temps d'ingressos i no en quantitat fixa: així valen el mateix a
    // qualsevol país i en qualsevol moment de la partida.
    [System.Serializable]
    public class Recompensa
    {
        public TipusDeRecompensa tipus;
        [Tooltip("Gemes; minuts d'ingressos; gemes per dia al pase")]
        public double quantitat;

        [Header("Multiplicador")]
        public double multiplicador = 2;
        [Tooltip("Minuts que dura el multiplicador")]
        public double minuts = 240;
        [Tooltip("Si se suma a un del mateix tipus, com a molt fins a tants minuts (0 = sense límit)")]
        public double minutsMaxims;

        [Header("Encarregat")]
        public Raresa raresa = Raresa.Epic;
        [Tooltip("Cofre: probabilitat de cada raresa en %, en ordre Comú, Rar, Èpic, Llegendari")]
        public float[] probabilitats = { 70f, 25f, 4.5f, 0.5f };

        [Header("Permanent i pase")]
        public EfectePermanent efectePermanent;
        [Min(1)] public int dies = 30;

        [Header("Regal a l'atzar")]
        [Tooltip("Pes quan el producte en dona una a l'atzar")]
        [Min(0)] public float pes = 1;

        // La raresa que surt d'un cofre amb una tirada de 0 a 1.
        public Raresa TirarRaresa(double tirada)
        {
            float total = 0;
            if (probabilitats != null) foreach (float p in probabilitats) total += Mathf.Max(0, p);
            if (total <= 0) return Raresa.Comu;

            double objectiu = tirada * total, acumulat = 0;
            for (int i = 0; i < probabilitats.Length && i < 4; i++)
            {
                acumulat += Mathf.Max(0, probabilitats[i]);
                if (objectiu < acumulat) return (Raresa)i;
            }
            for (int i = Mathf.Min(probabilitats.Length, 4) - 1; i >= 0; i--)
                if (probabilitats[i] > 0) return (Raresa)i;
            return Raresa.Comu;
        }

        // Una de la llista segons el pes, amb una tirada de 0 a 1.
        public static Recompensa TriarPerPes(IList<Recompensa> llista, double tirada)
        {
            if (llista == null || llista.Count == 0) return null;
            float total = 0;
            foreach (Recompensa r in llista) if (r != null) total += Mathf.Max(0, r.pes);
            if (total <= 0) return llista[0];

            double objectiu = tirada * total, acumulat = 0;
            foreach (Recompensa r in llista)
            {
                if (r == null) continue;
                acumulat += Mathf.Max(0, r.pes);
                if (objectiu < acumulat) return r;
            }
            for (int i = llista.Count - 1; i >= 0; i--)
                if (llista[i] != null && llista[i].pes > 0) return llista[i];
            return llista[0];
        }
    }

    // Un dia del calendari: pot donar més d'una cosa.
    [System.Serializable]
    public class DiaDelCalendari
    {
        public List<Recompensa> recompenses = new List<Recompensa>();
    }
}
