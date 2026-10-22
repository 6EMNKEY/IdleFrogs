using System.Collections.Generic;
using UnityEngine;

namespace IdleFrogs.Game
{
    // El tercer coll d'ampolla: el restaurant. El tren descarrega a l'estació
    // (un tub la uneix amb el restaurant; és només dibuix, el dipòsit és un
    // de sol) i els clients venen des de baix, entren, mengen i, en acabar,
    // paguen: els doblers surten de l'estació i entren a la cartera.
    //
    // La classe es diu Tenda perquè el guardat, els encarregats de tenda i el
    // GameManager ja la coneixen així.
    //
    // Sense encarregat, cada client s'espera a la porta fins que el toques a
    // ell (o el restaurant, que deixa passar el primer que espera): un toc,
    // un client.
    public class Tenda : UnitatMillorable, IEsPotTocar
    {
        [Header("Configuració")]
        [SerializeField] private TendaConfigSO config;

        [Header("Referències")]
        [Tooltip("D'on surten els doblers que paguen els clients")]
        [SerializeField] private Estacio estacio;
        [Tooltip("Prefab amb SpriteRenderer, Collider2D i ClientDelRestaurant")]
        [SerializeField] private ClientDelRestaurant prefabDeClient;
        [Tooltip("On es creen els clients. Buit: aquest objecte (vigila que no tingui escala)")]
        [SerializeField] private Transform contenidorDeClients;

        [Header("Camí — carril d'anada i carril de tornada")]
        [Tooltip("D'on apareixen, a baix")]
        [SerializeField] private Transform puntDAparicio;
        [Tooltip("La porta del restaurant")]
        [SerializeField] private Transform puntDEntrada;
        [Tooltip("On desapareixen en tornar, a baix (separat de l'aparició perquè no es creuin)")]
        [SerializeField] private Transform puntDeSortida;

        [Header("Zones — cada punt és el centre d'un rectangle (es veuen en seleccionar)")]
        [Tooltip("Cada client apareix en un lloc a l'atzar d'aquí dins")]
        [SerializeField] private Vector2 midaDeLAparicio = new Vector2(1.5f, 0.6f);
        [Tooltip("Cada client entra per un lloc a l'atzar de la porta")]
        [SerializeField] private Vector2 midaDeLEntrada = new Vector2(0.6f, 0.3f);
        [SerializeField] private Vector2 midaDeLaSortida = new Vector2(1.5f, 0.6f);
        [Header("Aspecte dels clients")]
        [Tooltip("Se'n tria un a l'atzar cada vegada que surt un client. Buit: el del prefab")]
        [SerializeField] private Sprite[] aspectes;
        [SerializeField] private bool elDibuixMiraALaDreta = true;
        [Tooltip("Quant s'aparta cada client del seu lloc a la cua, perquè sigui un grup i no una fila índia")]
        [Min(0)] [SerializeField] private float dispersioDeLaCua = 0.3f;
        [Tooltip("Cada client tarda a caminar entre un X% menys i un X% més que la mitjana, a l'atzar " +
                 "a cada volta. La mitjana no canvia, així que la venda per segon tampoc")]
        [Range(0f, 0.5f)] [SerializeField] private float variacioDeVelocitat = 0.25f;
        [Tooltip("Distància entre els que fan cua a la porta")]
        [Min(0.05f)] [SerializeField] private float espaiALaCua = 0.45f;
        [Tooltip("El de més avall va davant: de l'ordre base cap amunt")]
        [SerializeField] private int ordreBase = 20;

        [Header("Quants se'n dibuixen")]
        [Tooltip("Per sobre d'aquest número cada client dibuixat en representa uns quants")]
        [Range(1, 60)] [SerializeField] private int clientsVisiblesMaxims = 20;

        private readonly List<ClientDelRestaurant> _clients = new List<ClientDelRestaurant>();
        private readonly List<SpriteRenderer> _dibuixos = new List<SpriteRenderer>();
        private readonly List<ClientDelRestaurant> _cua = new List<ClientDelRestaurant>();
        private float[] _alcades = new float[0];
        private int[] _ordres = new int[0];

        // ---------------------------------------------------------------
        // UnitatMillorable
        // ---------------------------------------------------------------

        public override Ranura Ranura => new Ranura(TipusDeLloc.Tenda);
        public override string NomVisible => Textos.T("unitat.restaurant");
        public override string TitolDeLesEstadistiques => Textos.T("titol.restaurant");
        public override double CostBase => config.costBase;
        public override double CreixementDeCost => config.creixementDeCost;
        public override int NivellMaxim => config.nivellMaxim;
        public override FitaDeMultiplicador[] FitesDeMillora => config.fites;

        public double PerClientAlNivell(int n) =>
            System.Math.Max(1, System.Math.Floor(
                config.capacitatBase * System.Math.Pow(config.creixementDeCapacitat, n - 1)
                * Fites.Acumulat(config.fites, n)));

        public float VelocitatAlNivell(int n) =>
            Mathf.Min(config.velocitatMaxima,
                      config.velocitatBase * Mathf.Pow(config.creixementDeVelocitat, n - 1)
                      * (float)Fites.Acumulat(config.fites, n));

        public int ClientsAlNivell(int n) =>
            ModelDelRestaurant.Clients(n, config.clientsInicials, config.clientsPerFita, config.nivellsDeClientsExtra);

        // Amb les bonificacions de l'encarregat: moviment = caminar, treball =
        // menjar més de pressa, capacitat = més doblers per client.
        public int ClientsActius => ClientsAlNivell(nivell);
        public double PerClient => PerClientAlNivell(nivell) * Bonus.capacitat;
        public float Velocitat => VelocitatAlNivell(nivell) * (float)Bonus.velocitat;
        public float TempsDeMenjar => Mathf.Max(0.02f, config.tempsDeMenjar / (float)Bonus.treball);
        public bool ElDibuixMiraALaDreta => elDibuixMiraALaDreta;

        public override FilaEstadistica Resum(int desti) => new FilaEstadistica(TipusDEstadistica.Resum,
            Textos.T("stat.venda_total"),
            FormatadorDeNombres.Ritme(CabalReal),
            FormatadorDeNombres.Ritme(CabalRealAlNivell(desti)));

        public override void OmplirFiles(List<FilaEstadistica> files, int desti)
        {
            files.Add(new FilaEstadistica(TipusDEstadistica.Clients,
                Textos.T("stat.clients"),
                ClientsActius.ToString(),
                ClientsAlNivell(desti).ToString()));

            files.Add(new FilaEstadistica(TipusDEstadistica.PerClient,
                Textos.T("stat.per_client"),
                FormatadorDeNombres.Doblers(PerClient),
                FormatadorDeNombres.Doblers(PerClientAlNivell(desti) * Bonus.capacitat)));

            // No puja amb el nivell: surt sense fletxa.
            string menjar = FormatadorDeNombres.Decimals(TempsDeMenjar, 1) + "s";
            files.Add(new FilaEstadistica(TipusDEstadistica.TempsDeMenjar, Textos.T("stat.temps_menjar"), menjar, menjar));

            files.Add(new FilaEstadistica(TipusDEstadistica.VelocitatDelClient,
                Textos.T("stat.velocitat"),
                FormatadorDeNombres.Decimals(Velocitat, 2),
                FormatadorDeNombres.Decimals(VelocitatAlNivell(desti) * Bonus.velocitat, 2)));
        }

        // ---------------------------------------------------------------
        // Venda — la tercera mesura de la cadena i el que fa servir l'offline
        // ---------------------------------------------------------------

        public double CabalReal => CabalRealAlNivell(nivell);

        public double CabalRealAlNivell(int n)
        {
            if (!TeElCami) return 0;
            return ModelDelRestaurant.Venda(
                ClientsAlNivell(n), PerClientAlNivell(n) * Bonus.capacitat, TempsDeMenjar,
                DistanciaDAnada, DistanciaDeTornada, VelocitatAlNivell(n) * Bonus.velocitat);
        }

        private bool TeElCami => puntDAparicio != null && puntDEntrada != null && puntDeSortida != null;
        private float DistanciaDAnada => Vector3.Distance(puntDAparicio.position, puntDEntrada.position);
        private float DistanciaDeTornada => Vector3.Distance(puntDEntrada.position, puntDeSortida.position);

        private float SegonsDelCicle =>
            Velocitat <= 0 ? 0f : TempsDeMenjar + (DistanciaDAnada + DistanciaDeTornada) / Velocitat;

        // Tot el que s'ha venut en aquesta sessió. No es guarda: només el fa
        // servir la guia inicial per saber si ja has fet la primera venda.
        public double TotalVenut { get; private set; }

        // La crida el client en acabar de menjar. Paga per tots els que
        // representa, i mai més del que hi ha a l'estació.
        // El que ja tenen reservat els clients que són a dins menjant: encara
        // és a l'estació, però ja no hi ha ningú més que ho pugui menjar.
        private double _reservat;

        private double Lliure => estacio != null ? System.Math.Max(0, estacio.DoblersEmmagatzemats - _reservat) : 0;

        // Hi ha diners a l'estació que cap client no s'ha reservat encara.
        public bool HiHaMenjar => Lliure > 0;

        // En entrar: el que pagarà en acabar, o el que quedi si n'hi ha menys.
        public double Reservar()
        {
            int visibles = ModelDelRestaurant.Visibles(ClientsActius, clientsVisiblesMaxims);
            double import = PerClient * ModelDelRestaurant.Representa(ClientsActius, visibles);
            double reserva = System.Math.Min(import, Lliure);
            _reservat += reserva;
            return reserva;
        }

        // Si un client desapareix a mitges (s'apaga), el que tenia reservat torna a ser lliure.
        public void Alliberar(double reserva) => _reservat = System.Math.Max(0, _reservat - reserva);

        // En acabar de menjar paga just el que havia reservat.
        public void Cobrar(double reserva)
        {
            if (estacio == null) return;
            Alliberar(reserva);
            double cobrat = estacio.Retirar(reserva);
            if (cobrat <= 0) return;

            TotalVenut += cobrat;
            if (CurrencyManager.Instancia != null)
                CurrencyManager.Instancia.AfegirDoblers(cobrat);
        }

        // ---------------------------------------------------------------
        // Arrencada — la dirigeix GameManager, com els pantans, perquè ha
        // de passar després de carregar el nivell guardat.
        // ---------------------------------------------------------------

        public void InicialitzarTenda()
        {
            if (estacio == null || prefabDeClient == null || !TeElCami)
            {
                Debug.LogError("Tenda: falten referències (estació, prefab de client o punts del camí)", this);
                return;
            }
            ActualitzarClients();
        }

        protected override void DespresDeCanviDeNivell() => ActualitzarClients();

        // Tants clients dibuixats com toquin (fins al màxim). Els nous surten
        // repartits al llarg d'un cicle perquè no arribin tots alhora.
        private void ActualitzarClients()
        {
            if (prefabDeClient == null || !TeElCami) return;

            int visibles = EstaComprat ? ModelDelRestaurant.Visibles(ClientsActius, clientsVisiblesMaxims) : 0;
            Transform pare = contenidorDeClients != null ? contenidorDeClients : transform;

            while (_clients.Count < visibles)
            {
                ClientDelRestaurant c = Instantiate(prefabDeClient, pare);
                c.name = $"Client {_clients.Count + 1}";
                c.Inicialitzar(this);
                c.gameObject.SetActive(false);
                _clients.Add(c);
                _dibuixos.Add(c.GetComponent<SpriteRenderer>());
            }

            int nous = 0;
            for (int i = 0; i < _clients.Count; i++)
                if (i < visibles && !_clients[i].gameObject.activeSelf) nous++;

            int k = 0;
            for (int i = 0; i < _clients.Count; i++)
            {
                ClientDelRestaurant c = _clients[i];
                bool cal = i < visibles;
                if (cal && !c.gameObject.activeSelf)
                {
                    c.gameObject.SetActive(true);
                    c.Arrencar(ModelDelRestaurant.RetardDeSortida(k++, nous, SegonsDelCicle));
                }
                else if (!cal && c.gameObject.activeSelf)
                {
                    c.gameObject.SetActive(false);
                }
            }

            if (_alcades.Length != _clients.Count)
            {
                _alcades = new float[_clients.Count];
                _ordres = new int[_clients.Count];
            }
        }

        // ---------------------------------------------------------------
        // La cua de la porta
        // ---------------------------------------------------------------

        public Vector3 PuntDAparicio => puntDAparicio.position;
        public Vector3 PuntDEntrada => puntDEntrada.position;
        public Vector3 PuntDeSortida => puntDeSortida.position;

        // Un lloc a l'atzar de cada zona: cada client en tria un a cada cicle.
        public Vector3 LlocDAparicio() => ALaZona(puntDAparicio.position, midaDeLAparicio);
        public Vector3 LlocDEntrada() => ALaZona(puntDEntrada.position, midaDeLEntrada);
        public Vector3 LlocDeSortida() => ALaZona(puntDeSortida.position, midaDeLaSortida);

        private static Vector3 ALaZona(Vector3 centre, Vector2 mida) => centre + new Vector3(
            (Random.value - 0.5f) * mida.x, (Random.value - 0.5f) * mida.y, 0f);

        public void EntrarALaCua(ClientDelRestaurant c)
        {
            if (!_cua.Contains(c)) _cua.Add(c);
        }

        public void SortirDeLaCua(ClientDelRestaurant c) => _cua.Remove(c);

        // El primer, a un pas de la porta; la resta, enrere cap a d'on venen.
        public Vector3 PosicioALaCua(ClientDelRestaurant c)
        {
            int lloc = Mathf.Max(0, _cua.IndexOf(c));
            Vector3 enrere = (PuntDAparicio - PuntDEntrada).normalized;
            return PuntDEntrada + enrere * espaiALaCua * (lloc + 1);
        }

        // Només si queda menjar que no s'hagi reservat ja: si a l'estació n'hi
        // ha per a un, n'entra un. Amb encarregat, d'un en un i per ordre de
        // cua; sense, el que has tocat, sigui on sigui de la cua.
        public bool PotEntrar(ClientDelRestaurant c)
        {
            if (Lliure <= 0) return false;
            if (EstaAutomatitzada) return _cua.Count > 0 && _cua[0] == c;
            return c.TePermis;
        }

        // Tocar el restaurant deixa passar el primer de la cua que encara no
        // tingui permís: també és un toc per client.
        public void EnTocar()
        {
            if (!HiHaMenjar) return;
            foreach (ClientDelRestaurant c in _cua)
                if (c != null && !c.TePermis) { c.DonarPermis(); return; }
        }

        // El factor de temps de caminar d'una volta: uniforme al voltant d'1,
        // perquè el temps mitjà (el que compta per a la venda) no canviï.
        public float FactorDeTempsALAtzar() => 1f + Random.Range(-variacioDeVelocitat, variacioDeVelocitat);

        // Quant s'aparta un client del seu lloc a la cua (en qualsevol direcció).
        public Vector3 DesviacioALaCua() => (Vector3)(Random.insideUnitCircle * dispersioDeLaCua);

        public void SpriteALAtzar(SpriteRenderer sr)
        {
            if (aspectes == null || aspectes.Length == 0 || sr == null) return;
            Sprite s = aspectes[Random.Range(0, aspectes.Length)];
            if (s != null) sr.sprite = s;
        }

        // Per a la guia inicial: el primer que espera a la porta sense encarregat.
        public ClientDelRestaurant ClientQueEspera
        {
            get
            {
                if (EstaAutomatitzada) return null;
                foreach (ClientDelRestaurant c in _cua)
                    if (c != null && c.estatActual == EstatClient.Esperant) return c;
                return null;
            }
        }

        public bool AlgunClientMenja
        {
            get
            {
                foreach (ClientDelRestaurant c in _clients)
                    if (c != null && c.gameObject.activeSelf && c.estatActual == EstatClient.Menjant) return true;
                return false;
            }
        }

        // ---------------------------------------------------------------
        // Ordre de dibuix: el de més avall, davant
        // ---------------------------------------------------------------

        private void LateUpdate()
        {
            int n = _clients.Count;
            if (n == 0) return;
            for (int i = 0; i < n; i++)
                _alcades[i] = _clients[i] != null && _clients[i].gameObject.activeSelf
                    ? _clients[i].transform.position.y
                    : float.MaxValue;   // els apagats, al fons: no es veuen igualment

            OrdreDeMosques.Calcular(_alcades, ordreBase, _ordres);
            for (int i = 0; i < n; i++)
                if (_dibuixos[i] != null) _dibuixos[i].sortingOrder = _ordres[i];
        }

#if UNITY_EDITOR
        // Els dos carrils (verd: anada, taronja: tornada) i les tres zones.
        private void OnDrawGizmosSelected()
        {
            if (!TeElCami) return;
            Gizmos.color = new Color(0.3f, 1f, 0.4f);
            Gizmos.DrawLine(puntDAparicio.position, puntDEntrada.position);
            Gizmos.DrawWireCube(puntDAparicio.position, midaDeLAparicio);
            Gizmos.color = new Color(1f, 0.6f, 0.2f);
            Gizmos.DrawLine(puntDEntrada.position, puntDeSortida.position);
            Gizmos.DrawWireCube(puntDeSortida.position, midaDeLaSortida);
            Gizmos.color = new Color(1f, 1f, 0.3f);
            Gizmos.DrawWireCube(puntDEntrada.position, midaDeLEntrada);
        }
#endif
    }
}
