using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // L'aspecte de tots els encarregats d'un tipus: d'un lloc i, si es vol,
    // d'un efecte concret. Dues imatges: el retrat de les cartes del panell i
    // el sprite que es veu al món, a la silueta del pantà, el tren o el
    // restaurant. Si en falta una, es busca la següent opció per a aquella.
    [System.Serializable]
    public class RetratPerTipus
    {
        public TipusDeLloc lloc;
        [Tooltip("Marcat: val per a tots els encarregats del lloc, sigui quin sigui l'efecte")]
        public bool qualsevolEfecte;
        public TipusDEfecte efecte;
        [Tooltip("El de les cartes del panell d'encarregats")]
        public Sprite retrat;
        [Tooltip("El que es veu al món, a la silueta")]
        public Sprite spriteAlMon;
    }

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

        // Pesos de partida, per ajustar quan es provi l'economia.
        [Header("Probabilitat de raresa")]
        public PesDeRaresa[] pesos =
        {
            new PesDeRaresa { raresa = Raresa.Comu,       pes = 80f },
            new PesDeRaresa { raresa = Raresa.Rar,        pes = 15f },
            new PesDeRaresa { raresa = Raresa.Epic,       pes = 4.8f },
            new PesDeRaresa { raresa = Raresa.Llegendari, pes = 0.2f },
        };

        // Els encarregats apareixen quan el primer pantà arriba al nivell 5.
        // Abans toques tu: és la fase activa que fa que l'automatisme valgui.
        [Header("Desbloqueig")]
        [Min(1)] public int nivellDelPrimerPantaPerDesbloquejar = 5;

        [Header("Preu de compra")]
        public double preuBase = 100;
        [Tooltip("El primer costa preuBase, el segon preuBase×creixement, etc.")]
        public double creixementDePreu = 4;

        public double PreuDeLaCompra(int jaComprats) =>
            preuBase * System.Math.Pow(creixementDePreu, Mathf.Max(0, jaComprats));

        [Header("Aspecte per tipus")]
        [Tooltip("Per a cada imatge mana la pròpia de l'encarregat si en té; si no, la més concreta " +
                 "d'aquí (lloc + efecte abans que tot el lloc); si no, la de per defecte")]
        public RetratPerTipus[] retratsPerTipus;
        [Tooltip("Retrat del panell quan no n'hi ha cap més")]
        public Sprite retratPerDefecte;
        [Tooltip("Sprite del món quan no n'hi ha cap més")]
        public Sprite spriteAlMonPerDefecte;

        // El de les cartes del panell.
        public Sprite RetratDe(EncarregatSO e) =>
            Triar(e, e != null ? e.retrat : null, r => r.retrat, retratPerDefecte);

        // El de la silueta, al món.
        public Sprite SpriteAlMonDe(EncarregatSO e) =>
            Triar(e, e != null ? e.spriteAlMon : null, r => r.spriteAlMon, spriteAlMonPerDefecte);

        private Sprite Triar(EncarregatSO e, Sprite propi, System.Func<RetratPerTipus, Sprite> imatge, Sprite perDefecte)
        {
            if (e == null) return perDefecte;
            if (propi != null) return propi;

            Sprite delLloc = null;
            if (retratsPerTipus != null)
                foreach (RetratPerTipus r in retratsPerTipus)
                {
                    if (r == null || r.lloc != e.lloc) continue;
                    Sprite s = imatge(r);
                    if (s == null) continue;
                    if (!r.qualsevolEfecte && r.efecte == e.efecte) return s;
                    if (r.qualsevolEfecte && delLloc == null) delLloc = s;
                }
            return delLloc != null ? delLloc : perDefecte;
        }

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
