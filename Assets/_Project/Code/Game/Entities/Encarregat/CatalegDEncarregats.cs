using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    [System.Serializable]
    public class PesDeRaresa
    {
        public Raresa raresa;
        [Min(0)] public float pes;
    }

    // Tots els encarregats que existeixen, en un sol asset. El guardat desa
    // l'id de text i el resol aquí, així que reordenar l'array no trenca res.
    [CreateAssetMenu(fileName = "CatalegDEncarregats", menuName = "IdleFrogs/Cataleg d'Encarregats")]
    public class CatalegDEncarregats : ScriptableObject
    {
        [Header("Tots els encarregats")]
        public EncarregatSO[] encarregats;

        // AVÍS: aquests pesos són una proposta, NO són els d'Idle Miner.
        // El teardown no documenta cap taula de probabilitats de managers;
        // l'única que hi surt (80/15/4.8/0.2) és la d'Expedicions, un altre
        // sistema. Es parteix d'aquella perquè és l'únic referent que hi ha.
        [Header("Probabilitat de raresa (proposta, no copiada d'IMT)")]
        public PesDeRaresa[] pesos =
        {
            new PesDeRaresa { raresa = Raresa.Comu,       pes = 80f },
            new PesDeRaresa { raresa = Raresa.Rar,        pes = 15f },
            new PesDeRaresa { raresa = Raresa.Epic,       pes = 4.8f },
            new PesDeRaresa { raresa = Raresa.Llegendari, pes = 0.2f },
        };

        [Header("Preu de compra")]
        public double preuBase = 100;
        [Tooltip("El primer costa preuBase, el segon preuBase×creixement, etc.")]
        public double creixementDePreu = 4;

        public double PreuDeLaCompra(int jaComprats) =>
            preuBase * System.Math.Pow(creixementDePreu, Mathf.Max(0, jaComprats));

        public EncarregatSO PerId(string id)
        {
            if (encarregats == null || string.IsNullOrEmpty(id)) return null;
            foreach (EncarregatSO e in encarregats)
                if (e != null && e.id == id) return e;
            return null;
        }

        // El tipus de lloc el decideix la silueta des d'on compres; aquí
        // només es tira la raresa.
        public Raresa TirarRaresa()
        {
            float total = 0;
            if (pesos != null)
                foreach (PesDeRaresa p in pesos) total += Mathf.Max(0, p.pes);

            if (total <= 0) return Raresa.Comu;

            float tirada = Random.value * total;
            foreach (PesDeRaresa p in pesos)
            {
                tirada -= Mathf.Max(0, p.pes);
                if (tirada <= 0) return p.raresa;
            }
            return Raresa.Comu;
        }

        // Tira la raresa i tria un encarregat d'aquell lloc. Si per a la
        // raresa que ha sortit no n'hi ha cap d'aquell lloc, baixa de raresa
        // fins a trobar-ne un: val més donar un Comú que no donar res havent cobrat.
        public EncarregatSO Sortejar(TipusDeLloc lloc)
        {
            if (encarregats == null || encarregats.Length == 0) return null;

            Raresa objectiu = TirarRaresa();
            for (int r = (int)objectiu; r >= 0; r--)
            {
                EncarregatSO triat = TriarAlAtzar(lloc, (Raresa)r);
                if (triat != null) return triat;
            }

            // Cap d'aquesta raresa cap avall: prova qualsevol d'aquest lloc.
            return TriarAlAtzar(lloc, null);
        }

        private readonly List<EncarregatSO> _candidats = new List<EncarregatSO>();

        private EncarregatSO TriarAlAtzar(TipusDeLloc lloc, Raresa? raresa)
        {
            _candidats.Clear();
            foreach (EncarregatSO e in encarregats)
            {
                if (e == null || e.lloc != lloc) continue;
                if (raresa.HasValue && e.raresa != raresa.Value) continue;
                _candidats.Add(e);
            }
            if (_candidats.Count == 0) return null;
            return _candidats[Random.Range(0, _candidats.Count)];
        }
    }
}
