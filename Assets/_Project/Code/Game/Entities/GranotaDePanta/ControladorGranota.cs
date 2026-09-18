using UnityEngine;
using System.Collections;

public enum EstatGranota { Idle, MoventseAlNenufar, Recolectant, MoventseACaixa, Buidant }

namespace IdleFrogs.Game
{
    public class ControladorGranota : MonoBehaviour
    {
        [Header("Estat")]
        public EstatGranota estatActual = EstatGranota.Idle;

        private Panta _pantaPare;
        private PantaConfigSO _config;
        private Vector3 _posicioInicial;
        private Vector3 _posicioPilaDeMosques;
        private Vector3 _posicioCapsa;
        private double _doblersPerPle;

        public void Inicialitzar(Panta panta, PantaConfigSO config, Vector3 posicioInicial, Vector3 pilaDeMosques, Vector3 capsa)
        {
            _pantaPare = panta;
            _config = config;
            _posicioInicial = posicioInicial;
            _posicioPilaDeMosques = pilaDeMosques;
            _posicioCapsa = capsa;
            _doblersPerPle = config.doblersPerPle;

            transform.position = _posicioInicial;
            estatActual = EstatGranota.Idle;
        }

        private IEnumerator MouresALaPosicio(Vector3 posicioObjectiu)
        {
            while (Vector3.Distance(transform.position, posicioObjectiu) >= 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    posicioObjectiu,
                    _config.velocitatDeMoviment * Time.deltaTime
                );
                yield return null;

            }
            transform.position = posicioObjectiu;
        }
        private IEnumerator BuidarMosquesALaCapsa()
        {

        }

        private void OnMouseDown()
        {
            if (estatActual == EstatGranota.Idle)
            {
                StartCoroutine(CapturarMosques());
            }
        }

        private IEnumerator CapturarMosques()
        {
            // 1. Recolectar Mosca 
            estatActual = EstatGranota.Recolectant;
            yield return new WaitForSeconds(_config.tempsFinsPle);

            // 2. Moures cap a la capsa 
            estatActual = EstatGranota.MoventseACaixa;
            yield return MouresALaPosicio(_posicioCapsa);

            // 3. Buidar
            estatActual = EstatGranota.Buidant;
            yield return new WaitForSeconds(_config.tempsBuidar);
            // 4. Tornar a nenufar 
            estatActual = EstatGranota.MoventseAlNenufar;
            yield return MouresALaPosicio(_posicioInicial);
            estatActual = EstatGranota.Idle;
        }



    }
}
