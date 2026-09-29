using System;
using UnityEngine;

namespace IdleFrogs.Game
{
    // Una barrera no es "compra": es trenca esperant. Es toca per començar
    // el temporitzador i a partir d'aquí corre sola, també amb el joc tancat.
    //
    // Guardem l'hora de venciment en UTC i no els segons que queden: si
    // guardéssim "queden 240s", tancar l'app congelaria el rellotge i el
    // jugador podria esperar dos dies sense que baixés.
    public class Barrera : MonoBehaviour
    {
        [Header("Temporitzador")]
        [SerializeField] private double segonsTotals = 300;

        [Header("Preu per trencar-la a l'instant")]
        [Tooltip("Entra a la mateixa seqüència ×20 que els pantans. 0 = només s'obre esperant.")]
        [SerializeField] private double preu;

        [Header("Arrels visuals")]
        [Tooltip("La roca, el cartell... tot el que desapareix en obrir-se")]
        [SerializeField] private GameObject contingut;

        [SerializeField] private bool iniciada;
        [SerializeField] private bool oberta;
        private DateTime _venciment;

        public bool EstaIniciada => iniciada;
        public bool EstaOberta => oberta;
        public double SegonsTotals => segonsTotals;

        // Només es pot tocar quan ja has comprat tots els pantans que hi ha
        // davant. Ho decideix LlistaDePantans, igual que l'estat dels pantans.
        private bool _disponible;
        public bool EsPotIniciar => _disponible && !iniciada && !oberta;
        public void AplicarDisponibilitat(bool disponible) => _disponible = disponible;

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

        public void Iniciar()
        {
            if (oberta || iniciada) return;
            iniciada = true;
            _venciment = DateTime.UtcNow.AddSeconds(segonsTotals);
            EnCanviDeTemps?.Invoke();
        }

        // Sortida 1: veure un anunci
        public void ReduirTemps(double segons)
        {
            if (oberta || !iniciada || segons <= 0) return;
            _venciment = _venciment.AddSeconds(-segons);
            if (SegonsRestants <= 0) Obrir();
            else EnCanviDeTemps?.Invoke();
        }

        // Sortida 2: pagar doblers. No cal haver iniciat el temporitzador.
        public double Preu => preu;

        public bool PucPagarLaTrencada =>
            preu > 0 && CurrencyManager.Instancia != null &&
            CurrencyManager.Instancia.doblersActuals >= preu;

        public bool TrencarPagant()
        {
            if (oberta || preu <= 0) return false;
            if (CurrencyManager.Instancia == null) return false;
            if (!CurrencyManager.Instancia.GastarDoblers(preu)) return false;

            iniciada = true;
            Obrir();
            return true;
        }

        // Sortida 3: premium, sense pagar doblers
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

        private void Refrescar()
        {
            if (contingut != null) contingut.SetActive(!oberta);
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
