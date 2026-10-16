using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La silueta d'un encarregat en una ranura. Buida ensenya la silueta
    // fosca; amb encarregat assignat ensenya el seu retrat amb el color de
    // la raresa. Tocar-la obre el panell.
    //
    // No es veu fins que els encarregats es desbloquegen (primer pantà a
    // nivell 5). No n'apaguem el GameObject, perquè llavors aquest mateix
    // script deixaria de córrer i no se sabria tornar a encendre: apaguem
    // el dibuix i el collider.
    //
    // Mateix patró que BotoDeMillora: dins del prefab del pantà es busca la
    // unitat sola, però a l'estació i a la tenda el botó no penja de la
    // unitat que representa i cal omplir el camp a mà.
    public class SiluetaDEncarregat : MonoBehaviour, IEsPotTocar
    {
        [Tooltip("Buit dins d'un prefab (es busca sol pujant per la jerarquia). " +
                 "Omple'l quan la silueta no penja de la unitat: l'Estació és del Tren.")]
        [SerializeField] private UnitatMillorable unitat;

        [Header("Aspecte")]
        [SerializeField] private SpriteRenderer retrat;
        [Tooltip("El dibuix que es veu quan la ranura és buida")]
        [SerializeField] private Sprite spriteBuit;
        [SerializeField] private Color colorBuit = new Color(0.15f, 0.15f, 0.2f, 0.85f);

        [Tooltip("S'encén mentre l'habilitat està activa. Opcional: hi pots posar una llum, un halo...")]
        [SerializeField] private GameObject indicadorDHabilitat;

        private Collider2D _collider;

        private void Awake()
        {
            if (unitat == null) unitat = GetComponentInParent<UnitatMillorable>();
            if (retrat == null) retrat = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            if (unitat == null)
            {
                Debug.LogError("SiluetaDEncarregat: necessita una UnitatMillorable (al pare o al camp)", this);
                enabled = false;
                return;
            }

            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.EnCanviDEncarregats += Refrescar;

            // El tick de segon: l'habilitat s'apaga sola i l'indicador ha de
            // seguir-la encara que no canviï cap assignació.
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon += Refrescar;

            // Comprar el pantà també la pot fer aparèixer.
            unitat.EnCanviDeNivell += Refrescar;

            Refrescar();
        }

        private void OnDestroy()
        {
            if (GestorDEncarregats.Instancia != null)
                GestorDEncarregats.Instancia.EnCanviDEncarregats -= Refrescar;
            if (GameManager.Instancia != null)
                GameManager.Instancia.EnTickDeSegon -= Refrescar;
            if (unitat != null) unitat.EnCanviDeNivell -= Refrescar;
        }

        public void EnTocar()
        {
            if (!enabled) return;
            if (PanellDEncarregats.Instancia != null)
                PanellDEncarregats.Instancia.Obrir(unitat);
        }

        private void Refrescar()
        {
            if (retrat == null || unitat == null) return;

            bool visible = unitat.EstaComprat &&
                           GestorDEncarregats.Instancia != null &&
                           GestorDEncarregats.Instancia.EstanDesbloquejats;

            retrat.enabled = visible;
            if (_collider != null) _collider.enabled = visible;
            if (!visible)
            {
                if (indicadorDHabilitat != null) indicadorDHabilitat.SetActive(false);
                return;
            }

            EncarregatPosseit assignat = GestorDEncarregats.Instancia != null
                ? GestorDEncarregats.Instancia.ALaRanura(unitat.Ranura)
                : null;

            if (assignat == null || assignat.definicio == null)
            {
                if (spriteBuit != null) retrat.sprite = spriteBuit;
                retrat.color = colorBuit;
                if (indicadorDHabilitat != null) indicadorDHabilitat.SetActive(false);
                return;
            }

            if (assignat.definicio.retrat != null) retrat.sprite = assignat.definicio.retrat;
            retrat.color = ColorsDeRaresa.Per(assignat.definicio.raresa);
            if (indicadorDHabilitat != null) indicadorDHabilitat.SetActive(assignat.HabilitatActiva);
        }
    }
}
