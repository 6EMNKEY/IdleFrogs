using System.Collections;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum EstatTransportista { Idle, Recollint, AnantALaTenda, Venent, Tornant }

    // Fa el tram estació → tenda. Mateix patró que ControladorGranota: es
    // toca per engegar-la i tot el que depèn del nivell es llegeix en viu
    // de la tenda, perquè pot pujar de nivell mentre la granota camina.
    public class GranotaTransportista : MonoBehaviour
    {
        [Header("Estat")]
        public EstatTransportista estatActual = EstatTransportista.Idle;

        private Tenda _tenda;
        private Estacio _estacio;
        private Vector3 _puntDeRecollida;
        private Vector3 _puntDeVenda;
        private double _carrega;

        public double Carrega => _carrega;
        public event System.Action<double> EnCanviDeCarrega;

        public void Inicialitzar(Tenda tenda, Estacio estacio, Vector3 recollida, Vector3 venda)
        {
            _tenda = tenda;
            _estacio = estacio;
            _puntDeRecollida = recollida;
            _puntDeVenda = venda;

            transform.position = _puntDeRecollida;
            estatActual = EstatTransportista.Idle;
            AvisarCarrega();
        }

        private void OnMouseDown()
        {
            // L'avís va aquí i no a Start: l'ordre de Start entre objectes no
            // està garantit, així que allà donaria falsos positius. Aquí només
            // salta quan toques la granota i no passa res.
            if (_tenda == null || _estacio == null)
            {
                Debug.LogError("GranotaTransportista: sense inicialitzar. Assigna la Tenda al GameManager.", this);
                return;
            }

            IntentarEngegar();
        }

        // La crida el dit i, quan hi ha encarregat, també la Tenda cada frame.
        public bool IntentarEngegar()
        {
            if (_tenda == null || _estacio == null) return false;
            if (estatActual != EstatTransportista.Idle) return false;

            // Sense res al magatzem no val la pena ni començar: automatitzada,
            // si no, faria el temps de càrrega en vaig un cop i un altre.
            if (_estacio.DoblersEmmagatzemats <= 0) return false;

            StartCoroutine(Transportar());
            return true;
        }

        private IEnumerator MouresA(Vector3 objectiu)
        {
            while (Vector3.Distance(transform.position, objectiu) >= 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, objectiu, _tenda.Velocitat * Time.deltaTime);
                yield return null;
            }
            transform.position = objectiu;
        }

        private IEnumerator Transportar()
        {
            // 1. Recollir — s'espera primer i es retira després, perquè si
            //    es retirés abans, tancar el joc enmig de la càrrega deixaria
            //    els doblers ni a l'estació ni a la granota.
            estatActual = EstatTransportista.Recollint;
            yield return new WaitForSeconds(_tenda.TempsDeCarrega);

            _carrega = _estacio.Retirar(_tenda.CapacitatPerViatge);
            AvisarCarrega();

            if (_carrega <= 0)
            {
                // Magatzem buit: no val la pena fer el viatge
                estatActual = EstatTransportista.Idle;
                yield break;
            }

            // 2. Anar a la tenda
            estatActual = EstatTransportista.AnantALaTenda;
            yield return MouresA(_puntDeVenda);

            // 3. Vendre
            estatActual = EstatTransportista.Venent;
            yield return new WaitForSeconds(_tenda.TempsDeDescarrega);
            _tenda.Vendre(_carrega);
            _carrega = 0;
            AvisarCarrega();

            // 4. Tornar a l'estació
            estatActual = EstatTransportista.Tornant;
            yield return MouresA(_puntDeRecollida);
            estatActual = EstatTransportista.Idle;
        }

        private void AvisarCarrega() => EnCanviDeCarrega?.Invoke(_carrega);
    }
}
