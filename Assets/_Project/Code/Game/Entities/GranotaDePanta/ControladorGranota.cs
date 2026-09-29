using UnityEngine;
using System.Collections;

namespace IdleFrogs.Game
{
    public enum EstatGranota { Idle, MoventseAlNenufar, Recolectant, MoventseACaixa, Buidant }

    public class ControladorGranota : MonoBehaviour
    {
        [Header("Estat")]
        public EstatGranota estatActual = EstatGranota.Idle;

        private Panta _pantaPare;
        private Vector3 _posicioInicial;
        private Vector3 _posicioPilaDeMosques;
        private Vector3 _posicioCapsa;

        public void Inicialitzar(Panta panta, Vector3 posicioInicial, Vector3 pilaDeMosques, Vector3 capsa)
        {
            _pantaPare = panta;
            _posicioInicial = posicioInicial;
            _posicioPilaDeMosques = pilaDeMosques;
            _posicioCapsa = capsa;

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
                    _pantaPare.VelocitatDeMoviment * Time.deltaTime
                );
                yield return null;

            }
            transform.position = posicioObjectiu;
        }
        private IEnumerator BuidarMosquesALaCapsa(double quantitat)
        {
            _pantaPare.AfegirMosquesALaCapsa(quantitat);
            yield return null;
        }

        private void OnMouseDown() => IntentarEngegar();

        // La crida el dit i, quan hi ha encarregat, també el Panta cada frame.
        public bool IntentarEngegar()
        {
            if (_pantaPare == null) return false;
            if (estatActual != EstatGranota.Idle) return false;

            StartCoroutine(CapturarMosques());
            return true;
        }

        private IEnumerator CapturarMosques()
        {
            // 1. Recolectar Mosca — amb terra dur: un encarregat Llegendari
            //    divideix el temps per 7 i sense això la granota parpellejaria.
            estatActual = EstatGranota.Recolectant;
            yield return new WaitForSeconds(Mathf.Max(0.02f, _pantaPare.TempsFinsPle));

            // 2. Moures cap a la capsa
            estatActual = EstatGranota.MoventseACaixa;
            yield return MouresALaPosicio(_posicioCapsa);

            // 3. Buidar — es llegeix ara, no a Inicialitzar, perquè el
            //    pantà pot haver pujat de nivell mentre la granota corria
            estatActual = EstatGranota.Buidant;
            yield return new WaitForSeconds(Mathf.Max(0.02f, _pantaPare.TempsBuidar));
            yield return BuidarMosquesALaCapsa(_pantaPare.CapacitatPerGranota);

            // 4. Tornar a nenufar
            estatActual = EstatGranota.MoventseAlNenufar;
            yield return MouresALaPosicio(_posicioInicial);
            estatActual = EstatGranota.Idle;
        }



    }
}
