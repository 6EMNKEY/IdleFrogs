using System.Collections;
using UnityEngine;

namespace IdleFrogs.Game
{
    public enum EstatClient { Fora, Arribant, Esperant, Menjant, Sortint }

    // Un client del restaurant. Fa el seu cicle sense parar: puja des de baix
    // fins a la cua de la porta, entra quan pot, menja amagat, paga en acabar
    // i baixa per l'altre carril. Llavors torna a començar.
    //
    // Només és la part visual d'un o més clients reals: el que paga el diu la
    // Tenda (doblers per client × a quants representa).
    [RequireComponent(typeof(SpriteRenderer))]
    public class ClientDelRestaurant : MonoBehaviour, IEsPotTocar
    {
        public EstatClient estatActual = EstatClient.Fora;

        private Tenda _tenda;
        private SpriteRenderer _sr;
        private Collider2D _collider;
        private Vector3 _desviacioALaCua;  // perquè la cua sigui un grup i no una fila índia
        private float _esperantDesDe;      // quan s'ha posat a la cua
        private double _reserva;           // el que pagarà en acabar de menjar
        private Coroutine _cicle;

        public float EsperantDesDe => _esperantDesDe;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
        }

        public void Inicialitzar(Tenda tenda) => _tenda = tenda;

        // Comença el cicle després d'un retard: així els primers no surten
        // tots alhora.
        public void Arrencar(float retard)
        {
            if (_tenda == null) return;
            if (_cicle != null) StopCoroutine(_cicle);
            _cicle = StartCoroutine(Cicle(retard));
        }

        private void OnDisable()
        {
            _cicle = null;
            if (_tenda != null)
            {
                _tenda.SortirDeLaCua(this);
                if (_reserva > 0) _tenda.Alliberar(_reserva);
            }
            _reserva = 0;
            estatActual = EstatClient.Fora;
        }

        // Sense encarregat, tocar un client fa entrar tots els que esperen.
        public void EnTocar()
        {
            if (_tenda != null) _tenda.DeixarEntrar();
        }

        private IEnumerator Cicle(float retard)
        {
            Amagar(true);
            estatActual = EstatClient.Fora;
            for (float t = 0f; t < retard; t += Time.deltaTime) yield return null;

            while (true)
            {
                // 1. Puja fins al seu lloc a la cua.
                // A cada volta, llocs nous de les tres zones.
                _desviacioALaCua = _tenda.DesviacioALaCua();
                Vector3 porta = _tenda.LlocDEntrada();
                Vector3 sortida = _tenda.LlocDeSortida();
                _tenda.SpriteALAtzar(_sr);
                transform.position = _tenda.LlocDAparicio();
                Amagar(false);
                estatActual = EstatClient.Arribant;
                _esperantDesDe = Time.time;
                _tenda.EntrarALaCua(this);

                // 2. Va avançant a mesura que la cua es mou, fins que pot entrar.
                while (true)
                {
                    Vector3 lloc = _tenda.PosicioALaCua(this) + _desviacioALaCua;
                    bool hiEs = Moure(lloc);
                    if (hiEs && estatActual == EstatClient.Arribant) estatActual = EstatClient.Esperant;
                    if (hiEs && _tenda.PotEntrar(this)) break;
                    yield return null;
                }
                _tenda.SortirDeLaCua(this);
                _reserva = _tenda.Reservar();

                // 3. Fins a la porta i cap a dins.
                while (!Moure(porta)) yield return null;

                estatActual = EstatClient.Menjant;
                Amagar(true);
                for (float t = 0f, menjar = _tenda.TempsDeMenjar; t < menjar; t += Time.deltaTime) yield return null;
                _tenda.Cobrar(_reserva);
                _reserva = 0;

                // 4. Surt i baixa pel carril de tornada.
                estatActual = EstatClient.Sortint;
                transform.position = porta;
                Amagar(false);
                while (!Moure(sortida)) yield return null;

                Amagar(true);
                estatActual = EstatClient.Fora;
                yield return null;
            }
        }

        // Un pas cap a 'desti'. Torna true quan hi ha arribat.
        private bool Moure(Vector3 desti)
        {
            Vector3 abans = transform.position;
            transform.position = Vector3.MoveTowards(abans, desti, _tenda.Velocitat * Time.deltaTime);
            float dx = transform.position.x - abans.x;
            if (Mathf.Abs(dx) > 1e-4f) _sr.flipX = _tenda.ElDibuixMiraALaDreta ? dx < 0f : dx > 0f;
            return (transform.position - desti).sqrMagnitude < 0.0025f;
        }

        private void Amagar(bool amagat)
        {
            if (_sr != null) _sr.enabled = !amagat;
            if (_collider != null) _collider.enabled = !amagat;
        }
    }
}
