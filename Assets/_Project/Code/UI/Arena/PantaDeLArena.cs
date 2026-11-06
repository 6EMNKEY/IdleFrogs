using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un dels tres pantans de l'arena: el boss, la seva vida i regeneració,
    // quant falta per matar-lo i el nenúfar amb el lluitador (tocar-lo obre
    // el panell per triar-ne un).
    public class PantaDeLArena : MonoBehaviour
    {
        [Tooltip("0, 1 o 2")]
        [SerializeField] private int index;

        [Header("Pantà")]
        [Tooltip("El pantà del país (el fons de l'aspecte dels seus pantans)")]
        [SerializeField] private Image dibuixDelPanta;

        [Header("Boss")]
        [SerializeField] private Image boss;
        [SerializeField] private TMP_Text textNomDelBoss;
        [SerializeField] private Image insignia;
        [SerializeField] private TMP_Text textRaresa;
        [Tooltip("La barra plena: se li canvia l'amplada (àncora dreta) segons la vida")]
        [SerializeField] private RectTransform barraDeVida;
        [SerializeField] private TMP_Text textVida;
        [SerializeField] private TMP_Text textRegeneracio;
        [Tooltip("\"Mata en 5h 20m\", \"Impossible\", \"Derrotat!\"...")]
        [SerializeField] private TMP_Text textEstat;
        [SerializeField] private Color colorImpossible = new Color(0.9f, 0.25f, 0.2f);
        [SerializeField] private Color colorNormal = Color.white;

        [Header("Nenúfar")]
        [SerializeField] private Button nenufar;
        [SerializeField] private Image lluitador;
        [SerializeField] private TMP_Text textLluitador;

        private PantallaDeLArena _pantalla;
        private Color _colorDelNenufar = Color.white;

        public int Index => index;

        private void Awake()
        {
            if (nenufar != null) nenufar.onClick.AddListener(() => { if (_pantalla != null) _pantalla.TriarLluitador(index); });
            if (lluitador != null) _colorDelNenufar = lluitador.color;
        }

        public void Inicialitzar(PantallaDeLArena pantalla) => _pantalla = pantalla;

        public void Refrescar(int zona, PaisSO pais)
        {
            GestorDeLArena g = GestorDeLArena.Instancia;
            if (g == null) return;

            PrestigiConfigSO prestigi = PrestigiConfigSO.Actual;
            AspectePantaSO aspecte = prestigi != null ? prestigi.AspecteDe(pais) : null;
            if (dibuixDelPanta != null && aspecte != null && aspecte.fons != null)
            {
                dibuixDelPanta.sprite = SpritesDelPais.DelPais(aspecte.fons, pais);
                dibuixDelPanta.color = aspecte.tintDelFons;
            }

            BossALArena b = g.Boss(zona, index);
            bool hiHaBoss = b != null && !string.IsNullOrEmpty(b.idBoss);
            if (boss != null)
            {
                boss.gameObject.SetActive(hiHaBoss && !b.mort);
                if (hiHaBoss) AparencaDeLArena.Posar(boss, AparencaDeLArena.Sprite(b), b.raresa);
            }
            Posar(textNomDelBoss, hiHaBoss ? AparencaDeLArena.Nom(b) : string.Empty);
            if (insignia != null) insignia.color = hiHaBoss ? AparencaDeLArena.Color(b.raresa) : Color.clear;
            Posar(textRaresa, hiHaBoss ? AparencaDeLArena.Raresa(b.raresa) : string.Empty);

            double fraccio = hiHaBoss && b.vidaMaxima > 0 ? b.vida / b.vidaMaxima : 0;
            if (barraDeVida != null) barraDeVida.anchorMax = new Vector2(Mathf.Clamp01((float)fraccio), barraDeVida.anchorMax.y);
            Posar(textVida, hiHaBoss ? $"{FormatadorDeNombres.Doblers(b.vida)} / {FormatadorDeNombres.Doblers(b.vidaMaxima)}" : string.Empty);
            Posar(textRegeneracio, hiHaBoss ? Textos.F("arena.regen", FormatadorDeNombres.Doblers(g.Regeneracio(b))) : string.Empty);

            LluitadorGuardat l = hiHaBoss ? g.Exemplar(zona, b.lluitador) : null;
            if (textEstat != null)
            {
                double segons = g.SegonsPerMatar(zona, index);
                bool impossible = l != null && double.IsPositiveInfinity(segons);
                bool noArriba = !impossible && l != null && segons > g.SegonsDelCicle(zona);
                textEstat.text = !hiHaBoss ? string.Empty
                               : b.mort ? Textos.T("arena.mort")
                               : l == null ? Textos.T("arena.sense_lluitador")
                               : impossible ? Textos.T("arena.impossible")
                               : Textos.F(noArriba ? "arena.no_arriba" : "arena.mata_en", FormatadorDeNombres.Temps(segons));
                textEstat.color = impossible || noArriba ? colorImpossible : colorNormal;
            }

            if (lluitador != null)
            {
                if (l != null) AparencaDeLArena.Posar(lluitador, AparencaDeLArena.Sprite(l), l.raresa);
                else { lluitador.sprite = null; lluitador.color = _colorDelNenufar; }
            }
            Posar(textLluitador, l != null ? $"{AparencaDeLArena.Nom(l)} · {Textos.F("arena.nivell", l.nivell)}" : Textos.T("arena.assignar"));
        }

        private static void Posar(TMP_Text t, string text)
        {
            if (t != null) t.text = text ?? string.Empty;
        }
    }
}
