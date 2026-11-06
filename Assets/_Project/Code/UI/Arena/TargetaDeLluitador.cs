using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // Un lluitador en una llista (el panell de lluitadors) o en una carta
    // (el que t'ha sortit al gacha). Totes les peces són opcionals.
    public class TargetaDeLluitador : MonoBehaviour
    {
        [SerializeField] private Image icona;
        [SerializeField] private TMP_Text textNom;
        [Tooltip("La insígnia: es pinta del color de la raresa")]
        [SerializeField] private Image insignia;
        [SerializeField] private TMP_Text textRaresa;
        [SerializeField] private TMP_Text textNivell;
        [SerializeField] private TMP_Text textDany;
        [Tooltip("\"Al pantà 2\"")]
        [SerializeField] private TMP_Text textOn;
        [Tooltip("Tota la targeta: triar-la (per posar-la a un nenúfar)")]
        [SerializeField] private Button boto;
        [SerializeField] private Button botoMillorar;
        [SerializeField] private TMP_Text textMillorar;

        public LluitadorGuardat Lluitador { get; private set; }
        public Button Boto => boto;
        public Button BotoMillorar => botoMillorar;

        public void Omplir(LluitadorGuardat l, int zona)
        {
            Lluitador = l;
            GestorDeLArena g = GestorDeLArena.Instancia;
            if (l == null || g == null) return;

            AparencaDeLArena.Posar(icona, AparencaDeLArena.Sprite(l), l.raresa);
            Posar(textNom, AparencaDeLArena.Nom(l));
            if (insignia != null) insignia.color = AparencaDeLArena.Color(l.raresa);
            Posar(textRaresa, AparencaDeLArena.Raresa(l.raresa));
            Posar(textNivell, Textos.F("arena.nivell", l.nivell));
            Posar(textDany, Textos.F("arena.dany", FormatadorDeNombres.Doblers(g.Dany(zona, l))));
            int panta = g.PantaDe(zona, l.id);
            Posar(textOn, panta >= 0 ? Textos.F("arena.al_panta", panta + 1) : string.Empty);

            if (botoMillorar != null)
            {
                bool maxim = g.AlMaxim(l);
                double cost = g.CostDeMillora(l);
                PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisDeLaZona(zona) : null;
                Posar(textMillorar, maxim ? Textos.T("arena.maxim")
                                          : Textos.F("arena.millorar", FormatadorDeNombres.Doblers(cost) + Icones.MonedaDe(pais)));
                botoMillorar.interactable = !maxim;
                double tens = GameManager.Instancia != null ? GameManager.Instancia.DoblersDeLaZona(zona) : 0;
                CompraAmbGemes.Pintar(botoMillorar, maxim || tens >= cost);
            }
        }

        private static void Posar(TMP_Text t, string text)
        {
            if (t != null) t.text = text ?? string.Empty;
        }
    }
}
