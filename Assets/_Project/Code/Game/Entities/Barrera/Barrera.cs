using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Una barrera es salta un pantà: costa el que costaria aquell pantà i, a
    // més, s'ha d'esperar. Quan has comprat l'últim pantà del tram surt el
    // botó d'Unlock; pagant-lo comença el temporitzador, que corre sol també
    // amb el joc tancat. En acabar, la barrera desapareix. És un frenazo, però
    // el pantà de darrere produeix com el de la seva posició: en surts amb
    // més empenta.
    //
    // Guardem l'hora de venciment en UTC i no els segons que queden: si
    // guardéssim "queden 240s", tancar l'app congelaria el rellotge i el
    // jugador podria esperar dos dies sense que baixés.
    public class Barrera : MonoBehaviour
    {
        [Header("Temporitzador")]
        [Tooltip("Només si LlistaDePantans no en té per a aquesta barrera (Hores De Cada Barrera)")]
        [SerializeField] private double segonsTotals = 3600;

        [Header("Arrels visuals")]
        [Tooltip("La roca, el cartell... tot el que desapareix en obrir-se. Buit: tots els fills")]
        [SerializeField] private GameObject contingut;

        [SerializeField] private bool iniciada;
        [SerializeField] private bool oberta;
        private DateTime _venciment;

        // El preu el posa LlistaDePantans: el del pantà que es salta.
        private double _preu;

        public void Configurar(double segons, double preu)
        {
            if (segons > 0) segonsTotals = segons;
            _preu = preu;
        }

        public bool EstaIniciada => iniciada;
        public bool EstaOberta => oberta;
        public double SegonsTotals => segonsTotals;

        // Només es pot tocar quan ja has comprat tots els pantans que hi ha
        // davant. Ho decideix LlistaDePantans, igual que l'estat dels pantans.
        private bool _disponible;
        public bool EsPotIniciar => _disponible && !iniciada && !oberta;
        public void AplicarDisponibilitat(bool disponible)
        {
            if (_disponible == disponible) return;
            _disponible = disponible;
            EnCanviDeDisponibilitat?.Invoke();
        }

        // Comprar l'últim pantà del tram fa sortir el botó.
        public event Action EnCanviDeDisponibilitat;

        public double SegonsRestants
        {
            get
            {
                if (oberta) return 0;
                if (!iniciada) return segonsTotals;
                double queden = (_venciment - DateTime.UtcNow).TotalSeconds;
                return queden < 0 ? 0 : queden;
            }
        }

        public double Progres =>
            segonsTotals <= 0 ? 1.0 : 1.0 - (SegonsRestants / segonsTotals);

        public event Action EnCanviDeTemps;

        // Ens subscrivim a Start i no a OnEnable: GameManager.Instancia
        // s'assigna al seu Awake, i l'ordre d'Awake/OnEnable entre objectes
        // de l'escena no està garantit. Start sempre corre després de tots
        // els Awake, així que aquí la instància ja existeix.
        private void Start()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += Comprovar;
            Refrescar();
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Comprovar;
        }

        // El tick no descompta res: només mira el rellotge i avisa la UI.
        private void Comprovar()
        {
            if (oberta || !iniciada) return;
            if (SegonsRestants <= 0) Obrir();
            else EnCanviDeTemps?.Invoke();
        }

        public double Preu => _preu;

        public bool PucPagar =>
            _preu <= 0 || (CurrencyManager.Instancia != null && CurrencyManager.Instancia.doblersActuals >= _preu);

        // Pagar i començar a esperar.
        public bool Iniciar()
        {
            if (!EsPotIniciar) return false;
            if (_preu > 0 && (CurrencyManager.Instancia == null || !CurrencyManager.Instancia.GastarDoblers(_preu)))
                return false;

            iniciada = true;
            _venciment = DateTime.UtcNow.AddSeconds(segonsTotals);
            EnCanviDeTemps?.Invoke();
            return true;
        }

        // Escurçar l'espera: un anunci, gemes...
        public void ReduirTemps(double segons)
        {
            if (oberta || !iniciada || segons <= 0) return;
            _venciment = _venciment.AddSeconds(-segons);
            if (SegonsRestants <= 0) Obrir();
            else EnCanviDeTemps?.Invoke();
        }

        // Obrir-la del tot sense esperar (premium).
        public void ObrirAlInstant()
        {
            if (oberta) return;
            iniciada = true;
            Obrir();
        }

        private void Obrir()
        {
            oberta = true;
            Refrescar();
            EnCanviDeTemps?.Invoke();
        }

        // Oberta, no en queda res. Sense Contingut s'apaguen tots els fills i no
        // l'objecte mateix: LlistaDePantans encara la mira.
        private void Refrescar()
        {
            if (contingut != null)
            {
                contingut.SetActive(!oberta);
                return;
            }
            foreach (Transform fill in transform)
                fill.gameObject.SetActive(!oberta);
        }

        // ---------------------------------------------------------------
        // Guardat
        // ---------------------------------------------------------------

        public BarreraGuardada Guardar() => new BarreraGuardada
        {
            iniciada = iniciada,
            oberta = oberta,
            vencimentUtc = _venciment.ToBinary()
        };

        public void Carregar(BarreraGuardada dades)
        {
            if (dades == null) return;
            iniciada = dades.iniciada;
            oberta = dades.oberta;
            _venciment = DateTime.FromBinary(dades.vencimentUtc);

            // Pot haver vençut mentre el joc estava tancat.
            if (iniciada && !oberta && SegonsRestants <= 0) oberta = true;
            Refrescar();
        }
    }
}
