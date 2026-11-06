using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Els punts de recerca (un color per país) i els nivells de l'arbre. Global:
    // va a Resources/Sistemes i sobreviu als canvis d'escena i als prestigis.
    [DefaultExecutionOrder(-180)]
    public class GestorDeRecerca : MonoBehaviour
    {
        public static GestorDeRecerca Instancia { get; private set; }

        private DadesDeRecerca _dades = new DadesDeRecerca();
        // Les sumes es demanen molt (cada granota, cada frame): es guarden
        // fins que canvia algun nivell.
        private readonly System.Collections.Generic.Dictionary<int, double> _sumes = new System.Collections.Generic.Dictionary<int, double>();

        // Punts o nivells: la UI es repinta.
        public event Action EnCanvi;

        public ArbreDeRecercaSO Arbre => ArbreDeRecercaSO.Actual;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        public double Punts(int zona) => _dades.Punts(zona);
        public int Nivell(NodeDeRecerca n) => n != null ? _dades.Nivell(n.id) : 0;
        public EstatDeNode Estat(NodeDeRecerca n) => ModelDeRecerca.Estat(n, _dades);
        public bool LiArriba(NodeDeRecerca n) => n != null && ModelDeRecerca.LiArriba(n, _dades);

        public void AfegirPunts(int zona, double quantitat)
        {
            if (quantitat == 0) return;
            _dades.AfegirPunts(zona, quantitat);
            EnCanvi?.Invoke();
        }

        public bool Comprar(NodeDeRecerca n)
        {
            if (n == null || !ModelDeRecerca.Comprar(n, _dades)) return false;
            _sumes.Clear();
            EnCanvi?.Invoke();
            if (GameManager.Instancia != null) GameManager.Instancia.SaveGameData();
            return true;
        }

        public double Suma(TipusDEfecteDeRecerca tipus, int zona)
        {
            if (Arbre == null) return 0;
            int clau = (int)tipus * 1000 + zona + 1;
            if (!_sumes.TryGetValue(clau, out double s))
            {
                s = ModelDeRecerca.Suma(Arbre.nodes, _dades, tipus, zona);
                _sumes[clau] = s;
            }
            return s;
        }

        public DadesDeRecerca Guardar() => _dades;

        public void Carregar(DadesDeRecerca dades)
        {
            _dades = dades ?? new DadesDeRecerca();
            if (_dades.nivells == null) _dades.nivells = new System.Collections.Generic.List<NivellDeNode>();
            if (_dades.punts == null) _dades.punts = new System.Collections.Generic.List<double>();
            _sumes.Clear();
            EnCanvi?.Invoke();
        }
    }

    // Els efectes de la recerca, des de qualsevol lloc. Al país on ets si no
    // se'n diu cap altre.
    public static class Recerca
    {
        private static int ZonaActual => GameManager.Instancia != null ? GameManager.Instancia.Zona : 0;

        public static double Suma(TipusDEfecteDeRecerca tipus, int zona) =>
            GestorDeRecerca.Instancia != null ? GestorDeRecerca.Instancia.Suma(tipus, zona) : 0;

        public static double Suma(TipusDEfecteDeRecerca tipus) => Suma(tipus, ZonaActual);

        // Els que multipliquen: 1 + la suma (mai per sota del 10 %).
        public static double Factor(TipusDEfecteDeRecerca tipus, int zona) => Math.Max(0.1, 1 + Suma(tipus, zona));
        public static double Factor(TipusDEfecteDeRecerca tipus) => Factor(tipus, ZonaActual);
    }
}
