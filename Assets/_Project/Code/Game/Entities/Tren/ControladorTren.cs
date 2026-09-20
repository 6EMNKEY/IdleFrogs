using UnityEngine;
using System.Collections;


namespace IdleFrogs.Game
{
    public enum EstatTren { Esperant, Anant, Carregant, Tornant, Descarregant }

    public class ControladorTren : MonoBehaviour
    {
        [Header("Configuració")]
        [SerializeField] private TrenConfigSO config;

        [Header("Referències")]
        [SerializeField] private LlistaDePantans nivell;
        [SerializeField] private Transform puntDeDescarrega;

        [Header("Estat")]
        public EstatTren estatActual = EstatTren.Esperant;
        public double carregaActual;
        private void Start()
        {
            transform.position = PosicioSobreLaVia(puntDeDescarrega.position);
            StartCoroutine(BucleDelTren());
        }

        // El tren només es mou en X: conserva sempre la Y i la Z de la via.
        private Vector3 PosicioSobreLaVia(Vector3 objectiu)
        {
            return new Vector3(objectiu.x, transform.position.y, transform.position.z);
        }

        private IEnumerator MouresA(Vector3 objectiu)
        {
            Vector3 desti = PosicioSobreLaVia(objectiu);
            while (Vector3.Distance(transform.position, desti) >= 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, desti, config.velocitatDeMoviment * Time.deltaTime);
                yield return null;
            }
            transform.position = desti;
        }

        private IEnumerator BucleDelTren()
        {
            while (true)
            {
                estatActual = EstatTren.Esperant;

                int mesLlunya = nivell.IndexMesLlunyaAmbCarrega();
                if (mesLlunya < 0)
                {
                    yield return null;
                    continue;
                }

                // 1. Anada: para i carrega a cada pantà que tingui doblers
                for (int i = 0; i <= mesLlunya; i++)
                {
                    if (carregaActual >= config.capacitat) break;

                    Panta panta = nivell.Obtenir(i);
                    if (panta == null || panta.doblersALaCapsa <= 0) continue;

                    estatActual = EstatTren.Anant;
                    yield return MouresA(panta.capsaMosques.position);

                    estatActual = EstatTren.Carregant;
                    yield return new WaitForSeconds(config.tempsDeCarrega);
                    carregaActual += panta.RecollirMosquesDeLaCapsa(config.capacitat - carregaActual);
                }

                // 2. Tornada directa, sense parar enlloc
                estatActual = EstatTren.Tornant;
                yield return MouresA(puntDeDescarrega.position);

                // 3. Descarregar
                estatActual = EstatTren.Descarregant;
                yield return new WaitForSeconds(config.tempsDeDescarrega);
                if (CurrencyManager.Instancia != null)
                    CurrencyManager.Instancia.AfegirDoblers(carregaActual);
                carregaActual = 0;
            }
        }



    }
}

