using UnityEngine;

namespace IdleFrogs.Game
{
    // Els pantans van per trams separats per barreres. Un tram no ocupa cap
    // índex de pantà: la barrera n va just després de l'últim pantà del tram n.
    public static class TramsDePantans
    {
        // L'índex de l'últim pantà del tram (comptant des de 0). Si el tram no
        // existeix, el de l'últim que n'hi hagi.
        public static int UltimDelTram(int[] trams, int tram)
        {
            if (trams == null || trams.Length == 0 || tram < 0) return -1;
            int fins = Mathf.Min(tram, trams.Length - 1);
            int suma = 0;
            for (int i = 0; i <= fins; i++) suma += Mathf.Max(0, trams[i]);
            return suma - 1;
        }

        public static int Total(int[] trams) => UltimDelTram(trams, int.MaxValue) + 1;

        // Cada barrera "es menja" una posició: costa el que costaria el pantà
        // que hi aniria, i els que hi ha darrere produeixen i costen com si
        // aquell pantà existís. La posició econòmica d'un pantà és el seu
        // índex més les barreres que té davant.
        public static int BarreresAbans(int[] trams, int index, int barreres)
        {
            int quantes = 0;
            for (int n = 0; n < barreres; n++)
            {
                int ultim = UltimDelTram(trams, n);
                if (ultim >= 0 && ultim < index) quantes++;
            }
            return quantes;
        }

        public static int PosicioDelPanta(int[] trams, int index, int barreres) =>
            index + BarreresAbans(trams, index, barreres);

        // La posició que es menja la barrera n: la del que seria el pantà
        // just després de l'últim del seu tram.
        public static int PosicioDeLaBarrera(int[] trams, int barrera) =>
            UltimDelTram(trams, barrera) + barrera + 1;

        // El preu no segueix la posició del tot: el salt de la barrera al
        // pantà de darrere és petit (×1,5 en lloc del ×20 de sempre) i la
        // resta continua des d'aquí amb els salts normals. Cada barrera
        // passada abarateix tot el que ve darrere pel mateix factor.
        //
        // preuBase(posicio) és el preu de la configuració sense barreres.
        public static double FactorDePreu(int[] trams, int index, int barreres,
                                          System.Func<int, double> preuBase, double saltDespresDeBarrera)
        {
            double factor = 1;
            for (int n = 0; n < barreres; n++)
            {
                int ultim = UltimDelTram(trams, n);
                if (ultim < 0 || ultim >= index) continue;
                factor *= Rebaixa(trams, n, preuBase, saltDespresDeBarrera);
            }
            return factor;
        }

        // La barrera n paga les rebaixes de les d'abans, no la seva.
        public static double FactorDePreuDeLaBarrera(int[] trams, int barrera,
                                                     System.Func<int, double> preuBase, double saltDespresDeBarrera)
        {
            double factor = 1;
            for (int n = 0; n < barrera; n++) factor *= Rebaixa(trams, n, preuBase, saltDespresDeBarrera);
            return factor;
        }

        // Quant més barat surt el pantà de darrere de la barrera n que si
        // seguís el salt normal.
        private static double Rebaixa(int[] trams, int barrera, System.Func<int, double> preuBase, double salt)
        {
            int posicio = PosicioDeLaBarrera(trams, barrera);
            double deLaBarrera = preuBase(posicio);
            double delSeguent = preuBase(posicio + 1);
            if (deLaBarrera <= 0 || delSeguent <= 0 || salt <= 0) return 1;
            return salt * deLaBarrera / delSeguent;
        }
    }

    public class LlistaDePantans : MonoBehaviour
    {
        [Header("Tots els pantans en ordre (0 = el més a prop de la descàrrega)")]
        [SerializeField] private Panta[] pantans;

        [Header("Pantans de cada tram, en ordre. Entre tram i tram hi ha una barrera")]
        [Tooltip("La suma ha de ser el nombre de pantans, i hi ha d'haver una barrera menys que trams")]
        [SerializeField] private int[] pantansPerTram = { 5, 5, 4, 4, 4, 4 };

        [Header("Una barrera entre cada dos trams, en el mateix ordre")]
        [SerializeField] private Barrera[] barreres;

        [Header("Hores que tarda cada barrera a obrir-se, en el mateix ordre")]
        [Tooltip("El preu no es posa: és el del pantà que la barrera es salta")]
        [SerializeField] private float[] horesDeCadaBarrera = { 1, 3, 8, 16, 24 };

        [Tooltip("El pantà de darrere d'una barrera costa el preu de la barrera per això " +
                 "(en lloc del salt normal). Els següents continuen des d'aquí. Produeixen igual")]
        [Min(0.01f)] [SerializeField] private float saltDePreuDespresDeBarrera = 1.5f;

        public int Quantitat => pantans != null ? pantans.Length : 0;
        public int QuantitatDeBarreres => barreres != null ? barreres.Length : 0;
        public Panta Obtenir(int index) => pantans[index];
        public Barrera ObtenirBarrera(int index) => barreres[index];

        // El prefab és idèntic per a tots: el que distingeix un pantà d'un
        // altre és la seva posició aquí. Ho repartim a Awake perquè tothom
        // ho tingui abans que GameManager carregui la partida al seu Start.
        private void Awake()
        {
            for (int i = 0; i < Quantitat; i++)
            {
                if (pantans[i] == null) continue;
                pantans[i].AssignarIndex(i, TramsDePantans.PosicioDelPanta(pantansPerTram, i, QuantitatDeBarreres),
                    TramsDePantans.FactorDePreu(pantansPerTram, i, QuantitatDeBarreres, pantans[i].PreuALaPosicio, saltDePreuDespresDeBarrera));
                // Comprar un pantà desbloqueja el següent, i obrir una
                // barrera desbloqueja tot un tram: ho recalculem tot.
                // Els prefabs no poden referenciar-nos, així que som
                // nosaltres qui els escoltem a ells.
                pantans[i].EnCanviDeNivell += ActualitzarEstats;
            }

            for (int n = 0; n < QuantitatDeBarreres; n++)
            {
                if (barreres[n] == null) continue;
                barreres[n].Configurar(SegonsDeLaBarrera(n), PreuDeLaBarrera(n));
                barreres[n].EnCanviDeTemps += ActualitzarEstats;
            }
        }

        // Si la llista d'hores és més curta, la barrera es queda amb el seu temps.
        private double SegonsDeLaBarrera(int n) =>
            horesDeCadaBarrera != null && n < horesDeCadaBarrera.Length ? horesDeCadaBarrera[n] * 3600.0 : -1;

        // El preu del pantà que es salta, amb la configuració del de davant.
        private double PreuDeLaBarrera(int n)
        {
            int ultim = TramsDePantans.UltimDelTram(pantansPerTram, n);
            if (ultim < 0 || ultim >= Quantitat || pantans[ultim] == null) return 0;
            Panta p = pantans[ultim];
            return p.PreuALaPosicio(TramsDePantans.PosicioDeLaBarrera(pantansPerTram, n)) *
                   TramsDePantans.FactorDePreuDeLaBarrera(pantansPerTram, n, p.PreuALaPosicio, saltDePreuDespresDeBarrera);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < Quantitat; i++)
                if (pantans[i] != null) pantans[i].EnCanviDeNivell -= ActualitzarEstats;

            for (int n = 0; n < QuantitatDeBarreres; n++)
                if (barreres[n] != null) barreres[n].EnCanviDeTemps -= ActualitzarEstats;
        }

        // La primera barrera tancada talla la fila: no es pot passar de
        // l'últim pantà del seu tram.
        public int IndexMaximAccessible()
        {
            if (barreres != null)
            {
                for (int n = 0; n < barreres.Length; n++)
                {
                    if (barreres[n] != null && !barreres[n].EstaOberta)
                        return Mathf.Min(TramsDePantans.UltimDelTram(pantansPerTram, n), Quantitat - 1);
                }
            }
            return Quantitat - 1;
        }

        public int IndexMesLlunyaAmbCarrega()
        {
            int maxim = Mathf.Min(IndexMaximAccessible(), Quantitat - 1);
            for (int i = maxim; i >= 0; i--)
            {
                if (pantans[i] != null && pantans[i].DoblersALaCapsa > 0) return i;
            }
            return -1;
        }

        // ---------------------------------------------------------------
        // Estats — es compren en ordre, i una barrera tancada talla la fila.
        // Cal recalcular-ho tot cada vegada que es compra un pantà o s'obre
        // una barrera, perquè cada compra desbloqueja el següent.
        // ---------------------------------------------------------------

        public void ActualitzarEstats()
        {
            int maxim = IndexMaximAccessible();

            for (int i = 0; i < Quantitat; i++)
            {
                if (pantans[i] == null) continue;

                EstatDePanta estat;
                if (pantans[i].EstaComprat) estat = EstatDePanta.Comprat;
                else if (i > maxim) estat = EstatDePanta.Bloquejat;
                else if (i == 0 || (pantans[i - 1] != null && pantans[i - 1].EstaComprat))
                    estat = EstatDePanta.EnVenda;
                else estat = EstatDePanta.Bloquejat;

                pantans[i].AplicarEstat(estat);
            }

            for (int n = 0; n < QuantitatDeBarreres; n++)
                if (barreres[n] != null) barreres[n].AplicarDisponibilitat(EsPotIniciarBarrera(n));
        }

        // Només té sentit tocar la barrera quan ja has comprat tot el que hi ha davant.
        public bool EsPotIniciarBarrera(int index)
        {
            if (barreres == null || index < 0 || index >= barreres.Length) return false;
            if (barreres[index] == null || barreres[index].EstaOberta) return false;

            int ultimAbans = TramsDePantans.UltimDelTram(pantansPerTram, index);
            if (ultimAbans < 0 || ultimAbans >= Quantitat) return false;
            return pantans[ultimAbans] != null && pantans[ultimAbans].EstaComprat;
        }

        // ---------------------------------------------------------------
        // Guardat: l'índex de l'array és l'identificador estable de cada
        // pantà, el mateix ordre del qual depèn el tren.
        // ---------------------------------------------------------------

        public int[] ObtenirNivells()
        {
            int[] nivells = new int[Quantitat];
            for (int i = 0; i < nivells.Length; i++)
                nivells[i] = pantans[i] != null ? pantans[i].Nivell : 0;
            return nivells;
        }

        public double[] ObtenirCapses()
        {
            double[] capses = new double[Quantitat];
            for (int i = 0; i < capses.Length; i++)
                capses[i] = pantans[i] != null ? pantans[i].DoblersALaCapsa : 0;
            return capses;
        }

        public BarreraGuardada[] ObtenirBarreres()
        {
            var dades = new BarreraGuardada[QuantitatDeBarreres];
            for (int i = 0; i < dades.Length; i++)
                dades[i] = barreres[i] != null ? barreres[i].Guardar() : new BarreraGuardada();
            return dades;
        }

        public void AplicarNivells(int[] nivells)
        {
            if (nivells == null) return;
            // Min() perquè una partida vella pot tenir menys pantans
            // dels que hi ha ara a l'escena.
            int n = Mathf.Min(nivells.Length, Quantitat);
            for (int i = 0; i < n; i++)
                if (pantans[i] != null) pantans[i].AplicarNivell(nivells[i]);
        }

        public void AplicarCapses(double[] capses)
        {
            if (capses == null) return;
            int n = Mathf.Min(capses.Length, Quantitat);
            for (int i = 0; i < n; i++)
                if (pantans[i] != null) pantans[i].AplicarCapsa(capses[i]);
        }

        public void AplicarBarreres(BarreraGuardada[] dades)
        {
            if (dades == null) return;
            int n = Mathf.Min(dades.Length, QuantitatDeBarreres);
            for (int i = 0; i < n; i++)
                if (barreres[i] != null) barreres[i].Carregar(dades[i]);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            int total = TramsDePantans.Total(pantansPerTram);
            if (pantans != null && total != pantans.Length)
                Debug.LogWarning($"LlistaDePantans: els trams sumen {total} pantans i n'hi ha {pantans.Length}", this);
            int calen = pantansPerTram != null ? Mathf.Max(0, pantansPerTram.Length - 1) : 0;
            if (barreres != null && barreres.Length != calen)
                Debug.LogWarning($"LlistaDePantans: amb {pantansPerTram?.Length ?? 0} trams calen {calen} barreres i n'hi ha {barreres.Length}", this);
        }
#endif

        public void InicialitzarPantans()
        {
            if (pantans == null) return;
            foreach (Panta p in pantans)
                if (p != null) p.InicialitzarPanta();
            ActualitzarEstats();
        }
    }
}
