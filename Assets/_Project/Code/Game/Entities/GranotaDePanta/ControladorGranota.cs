using UnityEngine;
using System.Collections;

namespace IdleFrogs.Game
{
    public enum EstatGranota { Idle, MoventseAlNenufar, Recolectant, MoventseACaixa, Buidant }

    public class ControladorGranota : MonoBehaviour, IEsPotTocar
    {
        [Header("Estat")]
        public EstatGranota estatActual = EstatGranota.Idle;

        [Header("Llengua (visual)")]
        [Tooltip("Quantes vegades llança la llengua cada recol·lecció. L'última acaba just quan " +
                 "la granota marxa cap a la capsa")]
        [Range(1, 4)] [SerializeField] private int llenguades = 2;
        [Tooltip("Una llenguada no es fa més curta que això: si la recol·lecció és massa ràpida, " +
                 "se'n fan menys, i si ni una hi cap, cap")]
        [Min(0.05f)] [SerializeField] private float duracioMinima = 0.2f;
        [Tooltip("1 = totes atrapen la mosca, 2 = una de cada dos (les altres la mosca l'esquiva). " +
                 "Es compta des del final: l'última sempre l'atrapa")]
        [Range(1, 4)] [SerializeField] private int llenguadesPerMosca = 2;

        [Header("Orientació")]
        [Tooltip("Marca-ho si el dibuix de la granota mira cap a la dreta. Les granotes que queden " +
                 "a la dreta de les mosques es giren per mirar-les")]
        [SerializeField] private bool elDibuixMiraALaDreta = true;

        private LlenguaDeGranota _llengua;
        private SpriteRenderer _cos;
        private readonly Llenguada[] _pla = new Llenguada[4];

        private Panta _pantaPare;
        private Vector3 _posicioInicial;
        private Vector3 _posicioPilaDeMosques;
        private Vector3 _posicioCapsa;

        private void Awake() => BuscarPeces();

        // També des d'Inicialitzar: un pantà sense comprar té les granotes
        // apagades i el seu Awake encara no ha corregut.
        private void BuscarPeces()
        {
            if (_llengua == null) _llengua = GetComponentInChildren<LlenguaDeGranota>(true);
            if (_cos == null) _cos = GetComponent<SpriteRenderer>();
        }

        public void Inicialitzar(Panta panta, Vector3 posicioInicial, Vector3 pilaDeMosques, Vector3 capsa)
        {
            _pantaPare = panta;
            _posicioInicial = posicioInicial;
            _posicioPilaDeMosques = pilaDeMosques;
            _posicioCapsa = capsa;

            transform.position = _posicioInicial;
            estatActual = EstatGranota.Idle;

            // El centre de l'eixam i no la pila: la pila és la mosca model de
            // l'eixam i a aquestes alçades ja vola per on vol.
            Vector3 mosques = panta != null && panta.Eixam != null ? panta.Eixam.Centre : pilaDeMosques;
            MirarCap(mosques.x);
        }

        // Es gira el dibuix (flipX), no l'escala: amb escala negativa la
        // llengua, que s'orienta en coordenades del món, apuntaria al revés.
        private void MirarCap(float x)
        {
            BuscarPeces();
            bool alaEsquerra = x < _posicioInicial.x;
            if (_cos != null) _cos.flipX = elDibuixMiraALaDreta ? alaEsquerra : !alaEsquerra;
            if (_llengua != null) _llengua.Girar(alaEsquerra == elDibuixMiraALaDreta);
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
        // Dura exactament 'temps', amb llengua o sense: les llenguades només
        // omplen l'espera, no la canvien.
        private IEnumerator Recollir(float temps)
        {
            int n = _llengua != null
                ? PlaDeLlenguades.Calcular(temps, llenguades, _llengua.Duracio, duracioMinima, _pla)
                : 0;

            float inici = Time.time;
            for (int i = 0; i < n; i++)
            {
                while (Time.time - inici < _pla[i].inici) yield return null;
                bool atrapa = (n - 1 - i) % llenguadesPerMosca == 0;
                _llengua.Llancar(_pla[i].durada, _pantaPare.Eixam, atrapa);
            }
            while (Time.time - inici < temps) yield return null;
        }

        private IEnumerator BuidarMosquesALaCapsa(double quantitat)
        {
            _pantaPare.AfegirMosquesALaCapsa(quantitat);
            yield return null;
        }

        public void EnTocar() => IntentarEngegar();

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
            yield return Recollir(Mathf.Max(0.02f, _pantaPare.TempsFinsPle));

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
