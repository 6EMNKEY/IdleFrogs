using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Els lluitadors del país: millorar-los (amb la seva moneda) i, si s'obre
    // des d'un nenúfar, triar qui hi va. Les files es fan a partir d'una
    // plantilla (una TargetaDeLluitador apagada dins del contingut).
    public class PanellDeLluitadors : MonoBehaviour
    {
        [SerializeField] private GameObject arrel;
        [SerializeField] private TMP_Text textTitol;
        [Tooltip("On van les files (el Content del ScrollRect)")]
        [SerializeField] private RectTransform contingut;
        [Tooltip("La fila de mostra, apagada")]
        [SerializeField] private TargetaDeLluitador plantilla;
        [Tooltip("\"Encara no en tens: invoca'n al gacha\"")]
        [SerializeField] private TMP_Text textBuit;
        [Tooltip("Només quan es tria per a un nenúfar ocupat")]
        [SerializeField] private Button botoTreure;
        [SerializeField] private Button botoTancar;

        private readonly List<TargetaDeLluitador> _files = new List<TargetaDeLluitador>();
        private readonly List<LluitadorGuardat> _ordenats = new List<LluitadorGuardat>();
        private int _zona = -1;
        // El nenúfar per al qual es tria; -1 = només mirar i millorar.
        private int _panta = -1;

        public bool EstaObert => arrel != null && arrel.activeSelf;

        private void Awake()
        {
            if (plantilla != null) plantilla.gameObject.SetActive(false);
            if (botoTancar != null) botoTancar.onClick.AddListener(Tancar);
            if (botoTreure != null) botoTreure.onClick.AddListener(Treure);
            if (arrel != null) arrel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GestorDeLArena.Instancia != null) GestorDeLArena.Instancia.EnCanvi += Refrescar;
        }

        private void OnDisable()
        {
            if (GestorDeLArena.Instancia != null) GestorDeLArena.Instancia.EnCanvi -= Refrescar;
        }

        public void Obrir(int zona) => Obrir(zona, -1);

        public void TriarPer(int zona, int panta) => Obrir(zona, panta);

        private void Obrir(int zona, int panta)
        {
            _zona = zona;
            _panta = panta;
            GestorDAudio.Sonar(So.Obrir);
            if (arrel != null)
            {
                arrel.SetActive(true);
                arrel.transform.SetAsLastSibling();
            }
            Refrescar();
        }

        public void Tancar()
        {
            if (arrel != null && arrel.activeSelf) GestorDAudio.Sonar(So.Tancar);
            if (arrel != null) arrel.SetActive(false);
            _panta = -1;
        }

        private void Refrescar()
        {
            GestorDeLArena g = GestorDeLArena.Instancia;
            if (!EstaObert || g == null || _zona < 0) return;

            if (textTitol != null)
                textTitol.text = _panta >= 0 ? Textos.F("arena.triar_per", _panta + 1) : Textos.T("arena.lluitadors");

            // Els millors primer.
            _ordenats.Clear();
            foreach (LluitadorGuardat l in g.Arena(_zona).lluitadors) if (l != null) _ordenats.Add(l);
            _ordenats.Sort((a, b) => a.raresa != b.raresa ? b.raresa.CompareTo(a.raresa) : b.nivell.CompareTo(a.nivell));

            while (_files.Count < _ordenats.Count) _files.Add(NovaFila());
            for (int i = 0; i < _files.Count; i++)
            {
                bool hi = i < _ordenats.Count;
                if (_files[i].gameObject.activeSelf != hi) _files[i].gameObject.SetActive(hi);
                if (hi) _files[i].Omplir(_ordenats[i], _zona);
            }
            if (textBuit != null) textBuit.gameObject.SetActive(_ordenats.Count == 0);

            BossALArena b = _panta >= 0 ? g.Boss(_zona, _panta) : null;
            if (botoTreure != null) botoTreure.gameObject.SetActive(b != null && b.lluitador != 0);
        }

        private TargetaDeLluitador NovaFila()
        {
            TargetaDeLluitador f = Instantiate(plantilla, contingut);
            f.gameObject.SetActive(true);
            if (f.Boto != null) f.Boto.onClick.AddListener(() => Triar(f));
            if (f.BotoMillorar != null) f.BotoMillorar.onClick.AddListener(() => Millorar(f));
            return f;
        }

        private void Triar(TargetaDeLluitador f)
        {
            if (_panta < 0 || f.Lluitador == null || GestorDeLArena.Instancia == null) return;
            bool fet = GestorDeLArena.Instancia.Assignar(_zona, _panta, f.Lluitador.id);
            GestorDAudio.Sonar(fet ? So.Acceptar : So.Error);
            if (fet) Tancar();
        }

        private void Treure()
        {
            if (_panta < 0 || GestorDeLArena.Instancia == null) return;
            GestorDeLArena.Instancia.Assignar(_zona, _panta, 0);
            GestorDAudio.Sonar(So.Tancar);
            Tancar();
        }

        private void Millorar(TargetaDeLluitador f)
        {
            if (f.Lluitador == null || GestorDeLArena.Instancia == null) return;
            bool fet = GestorDeLArena.Instancia.Millorar(_zona, f.Lluitador.id);
            GestorDAudio.Sonar(fet ? So.Millorar : So.Error);
        }
    }
}
