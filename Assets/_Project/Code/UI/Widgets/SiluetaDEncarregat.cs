using TMPro;
using UnityEngine;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // La silueta d'un encarregat en una ranura. Buida ensenya la silueta
    // fosca; amb encarregat assignat ensenya el seu retrat tal com és, i
    // mentre té l'habilitat encesa deixa anar partícules del color de la seva
    // raresa. Tocar-la obre el panell.
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
        [Tooltip("Es veu quan hi ha un encarregat assignat però no té retrat")]
        [SerializeField] private Sprite spriteAssignat;

        [Tooltip("S'encén mentre l'habilitat està activa. Opcional: hi pots posar una llum, un halo...")]
        [SerializeField] private GameObject indicadorDHabilitat;

        [Header("Temps de l'habilitat")]
        [Tooltip("Opcional: un text del món (TextMeshPro, no UGUI) a sobre del sprite. Només es veu " +
                 "mentre l'habilitat està encesa (el que li queda) o recarregant (el que falta)")]
        [SerializeField] private TMP_Text textDelTemps;
        [Tooltip("Mentre recarrega, el sprite i el text es multipliquen per això: 1 = igual, 0 = negre")]
        [Range(0f, 1f)] [SerializeField] private float clarorEnRecarregar = 0.6f;

        [Header("Partícules de l'habilitat")]
        [Tooltip("Es pinten del color de la raresa i sonen mentre l'habilitat està encesa. " +
                 "Buit: es busca entre els fills")]
        [SerializeField] private ParticleSystem particules;

        private Collider2D _collider;
        private readonly ColorDelTemps _colorDelTemps = new ColorDelTemps();

        private void Awake()
        {
            if (unitat == null) unitat = GetComponentInParent<UnitatMillorable>();
            if (retrat == null) retrat = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            if (particules == null) particules = GetComponentInChildren<ParticleSystem>(true);
            if (particules != null) particules.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
                Particules(false, Color.white);
                PintarTemps(null);
                return;
            }

            EncarregatPosseit assignat = GestorDEncarregats.Instancia != null
                ? GestorDEncarregats.Instancia.ALaRanura(unitat.Ranura)
                : null;

            if (assignat == null || assignat.definicio == null)
            {
                if (spriteBuit != null) retrat.sprite = SpritesDelPais.Substituir(spriteBuit);
                retrat.color = colorBuit;
                if (indicadorDHabilitat != null) indicadorDHabilitat.SetActive(false);
                Particules(false, Color.white);
                PintarTemps(null);
                return;
            }

            // El sprite del món (no el retrat del panell): el seu, el del seu
            // tipus o el de per defecte del catàleg; si no n'hi ha cap, el de
            // "n'hi ha un" d'aquesta silueta.
            Sprite s = GestorDEncarregats.Instancia != null
                ? GestorDEncarregats.Instancia.SpriteAlMonDe(assignat.definicio)
                : assignat.definicio.spriteAlMon;
            if (s == null) s = SpritesDelPais.Substituir(spriteAssignat);
            if (s != null) retrat.sprite = s;
            // Recarregant: el sprite més fosc, com el text del temps.
            float f = assignat.Estat == EstatDHabilitat.Recarregant ? Mathf.Clamp01(clarorEnRecarregar) : 1f;
            retrat.color = new Color(f, f, f, 1f);
            if (indicadorDHabilitat != null) indicadorDHabilitat.SetActive(assignat.HabilitatActiva);
            Particules(assignat.HabilitatActiva, ColorsDeRaresa.Per(assignat.definicio.raresa));
            PintarTemps(assignat);
        }

        // Es crida cada segon amb la resta: així el compte enrere avança.
        private void PintarTemps(EncarregatPosseit e)
        {
            if (textDelTemps == null) return;
            bool cal = e != null && e.definicio != null && e.Estat != EstatDHabilitat.Llesta;
            // El component i no el GameObject: si el text fos al mateix objecte
            // que la silueta, apagar-lo l'aturaria a ella també.
            textDelTemps.enabled = cal;
            if (!cal) return;
            textDelTemps.text = e.TempsDeLHabilitat;
            _colorDelTemps.Pintar(textDelTemps, e.Estat == EstatDHabilitat.Recarregant, clarorEnRecarregar);
        }

        // Es crida cada segon: només s'arrenca o s'atura quan canvia, perquè
        // tornar a fer Play reiniciaria l'emissió. En aturar-se, les que ja
        // volen s'acaben de fondre.
        private void Particules(bool enceses, Color color)
        {
            if (particules == null) return;
            if (enceses)
            {
                ParticleSystem.MainModule main = particules.main;
                main.startColor = color;
                if (!particules.isEmitting) particules.Play();
            }
            else if (particules.isEmitting)
            {
                particules.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
