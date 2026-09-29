using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Una fila de la llista d'encarregats. Prefab petit que el panell
    // instancia tantes vegades com en tinguis d'aquell tipus.
    public class FitxaDEncarregat : MonoBehaviour
    {
        [SerializeField] private Image retrat;
        [SerializeField] private Image marcDeRaresa;
        [SerializeField] private TMP_Text nom;
        [SerializeField] private TMP_Text efecte;
        [SerializeField] private TMP_Text estat;

        [SerializeField] private Button botoAssignar;
        [SerializeField] private TMP_Text textBotoAssignar;
        [SerializeField] private Button botoHabilitat;
        [SerializeField] private TMP_Text textBotoHabilitat;

        private EncarregatPosseit _encarregat;
        private Ranura _ranura;
        private System.Action _refrescarPanell;

        private void Awake()
        {
            if (botoAssignar != null) botoAssignar.onClick.AddListener(AlternarAssignacio);
            if (botoHabilitat != null) botoHabilitat.onClick.AddListener(ActivarHabilitat);
        }

        public void Omplir(EncarregatPosseit encarregat, Ranura ranura, System.Action refrescarPanell)
        {
            _encarregat = encarregat;
            _ranura = ranura;
            _refrescarPanell = refrescarPanell;
            Refrescar();
        }

        public void Refrescar()
        {
            if (_encarregat == null || _encarregat.definicio == null) return;
            EncarregatSO d = _encarregat.definicio;

            if (retrat != null && d.retrat != null) retrat.sprite = d.retrat;
            if (marcDeRaresa != null) marcDeRaresa.color = ColorsDeRaresa.Per(d.raresa);
            if (nom != null) nom.text = $"{d.nomVisible}";
            if (efecte != null) efecte.text = d.DescripcioDeLEfecte;

            bool aquestaRanura = _encarregat.assignat && _encarregat.ranura.Equals(_ranura);

            if (estat != null)
            {
                if (aquestaRanura) estat.text = "Aquí";
                else if (_encarregat.assignat) estat.text = $"A {_encarregat.ranura}";
                else estat.text = "Lliure";
            }

            // Tres casos: el que ja hi és, el que està en una altra ranura
            // (es mou) i el que no fa res.
            if (textBotoAssignar != null)
                textBotoAssignar.text = aquestaRanura ? "Treure"
                                      : _encarregat.assignat ? "Moure aquí"
                                      : "Assignar";

            RefrescarHabilitat(aquestaRanura);
        }

        private void RefrescarHabilitat(bool aquestaRanura)
        {
            if (botoHabilitat == null) return;

            // Només té sentit engegar-la si l'encarregat està treballant.
            botoHabilitat.gameObject.SetActive(_encarregat.assignat);
            if (!_encarregat.assignat) return;

            bool activa = _encarregat.HabilitatActiva;
            bool llesta = _encarregat.HabilitatLlesta;

            botoHabilitat.interactable = llesta;
            if (textBotoHabilitat == null) return;

            if (activa)
                textBotoHabilitat.text = $"Actiu · {_encarregat.definicio.DescripcioDeLHabilitat}";
            else if (llesta)
                textBotoHabilitat.text = $"Activar · {_encarregat.definicio.DescripcioDeLHabilitat}";
            else
                textBotoHabilitat.text = FormatadorDeNombres.Temps(_encarregat.SegonsFinsLlesta);
        }

        private void AlternarAssignacio()
        {
            if (_encarregat == null || GestorDEncarregats.Instancia == null) return;

            bool aquestaRanura = _encarregat.assignat && _encarregat.ranura.Equals(_ranura);
            if (aquestaRanura) GestorDEncarregats.Instancia.Desassignar(_encarregat);
            else GestorDEncarregats.Instancia.Assignar(_encarregat, _ranura);

            _refrescarPanell?.Invoke();
        }

        private void ActivarHabilitat()
        {
            if (_encarregat == null || GestorDEncarregats.Instancia == null) return;
            GestorDEncarregats.Instancia.ActivarHabilitat(_encarregat);
            _refrescarPanell?.Invoke();
        }
    }
}
