using UnityEngine;

namespace IdleFrogs.Game
{
    public class Barrera : MonoBehaviour
    {
        [Header("Temporitzador")]
        [SerializeField] private double segonsTotals = 300;
        [SerializeField] private double segonsRestants = 300;

        public bool EstaOberta => segonsRestants <= 0;
        public double SegonsRestants => segonsRestants;
        public double Progres => 1.0 - (segonsRestants / segonsTotals);

        private void Start()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += DescomptarUnSegon;
        }

        private void OnDestroy()
        {
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= DescomptarUnSegon;
        }

        private void DescomptarUnSegon()
        {
            if (EstaOberta) return;
            segonsRestants -= 1;
            if (segonsRestants < 0) segonsRestants = 0;
        }

        // Sortida 1: veure un anunci
        public void ReduirTemps(double segons)
        {
            segonsRestants -= segons;
            if (segonsRestants < 0) segonsRestants = 0;
        }

        // Sortida 2: pagar premium
        public void ObrirAlInstant() => segonsRestants = 0;
    }
}