using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdleFrogs.Game
{
    // Els sons de les accions de la interfície: obrir, tancar, comprar...
    public enum So { Toc, Obrir, Tancar, Acceptar, Cancellar, Error, Comprar, Millorar, Vendre, Desbloquejar, Moneda, Gema, Recompensa, Potenciador }

    [Serializable]
    public class EfecteDeSo
    {
        public So so;
        [Tooltip("Si n'hi ha més d'un, se'n tria un a l'atzar")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volum = 1f;
        [Tooltip("Variació de to a l'atzar, perquè els sons repetits no cansin")]
        [Range(0f, 0.3f)] public float variacioDeTo = 0.04f;
        [Tooltip("Segons mínims entre dos d'aquest (les monedes que arriben al comptador)")]
        [Min(0f)] public float intervalMinim;
    }

    // La música i els efectes. Viu fora de l'escena (DontDestroyOnLoad):
    // viatjar entre països recarrega l'escena i la música no s'ha de tallar
    // si és la mateixa. Cada país pot tenir la seva (PaisSO.musica).
    //
    // Des de qualsevol lloc: GestorDAudio.Sonar(So.Comprar).
    [DefaultExecutionOrder(-170)]
    public class GestorDAudio : MonoBehaviour
    {
        public static GestorDAudio Instancia { get; private set; }

        private const string ClauMusica = "IdleFrogs.VolumMusica";
        private const string ClauEfectes = "IdleFrogs.VolumEfectes";

        [Header("Música")]
        [Tooltip("La d'un país sense música pròpia")]
        [SerializeField] private AudioClip musicaPerDefecte;
        [Tooltip("El volum de la música amb el control al màxim")]
        [Range(0f, 1f)] [SerializeField] private float volumMaximDeLaMusica = 0.5f;
        [SerializeField] private float segonsDeFosa = 1.2f;

        [Header("Efectes")]
        [SerializeField] private EfecteDeSo[] efectes;
        [Tooltip("Quants sons a la vegada (cadascun amb el seu to)")]
        [Range(2, 16)] [SerializeField] private int veus = 8;

        private AudioSource _musica;
        private AudioSource[] _veus;
        private int _seguentVeu;
        private readonly float[] _ultimaVegada = new float[Enum.GetValues(typeof(So)).Length];
        private Coroutine _fosa;

        public float VolumMusica { get; private set; } = 0.8f;
        public float VolumEfectes { get; private set; } = 1f;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            // DontDestroyOnLoad només funciona amb objectes arrel.
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _musica = gameObject.AddComponent<AudioSource>();
            _musica.loop = true;
            _musica.playOnAwake = false;
            _veus = new AudioSource[veus];
            for (int i = 0; i < veus; i++)
            {
                _veus[i] = gameObject.AddComponent<AudioSource>();
                _veus[i].playOnAwake = false;
            }

            VolumMusica = PlayerPrefs.GetFloat(ClauMusica, 0.8f);
            VolumEfectes = PlayerPrefs.GetFloat(ClauEfectes, 1f);
            SceneManager.sceneLoaded += EnCarregarEscena;
            PosarLaMusicaDelPais();
        }

        private void OnDestroy()
        {
            if (Instancia != this) return;
            Instancia = null;
            SceneManager.sceneLoaded -= EnCarregarEscena;
        }

        private void EnCarregarEscena(Scene _, LoadSceneMode __) => PosarLaMusicaDelPais();

        // ---------------------------------------------------------------
        // Efectes
        // ---------------------------------------------------------------

        public static void Sonar(So so)
        {
            if (Instancia != null) Instancia.SonarAra(so);
        }

        private void SonarAra(So so)
        {
            if (VolumEfectes <= 0f || efectes == null) return;
            EfecteDeSo e = Array.Find(efectes, x => x != null && x.so == so);
            if (e == null || e.clips == null || e.clips.Length == 0) return;

            int i = (int)so;
            if (e.intervalMinim > 0 && Time.unscaledTime - _ultimaVegada[i] < e.intervalMinim) return;
            _ultimaVegada[i] = Time.unscaledTime;

            AudioClip clip = e.clips[UnityEngine.Random.Range(0, e.clips.Length)];
            if (clip == null) return;

            AudioSource veu = _veus[_seguentVeu];
            _seguentVeu = (_seguentVeu + 1) % _veus.Length;
            veu.pitch = 1f + UnityEngine.Random.Range(-e.variacioDeTo, e.variacioDeTo);
            veu.PlayOneShot(clip, e.volum * VolumEfectes);
        }

        // ---------------------------------------------------------------
        // Música
        // ---------------------------------------------------------------

        private void PosarLaMusicaDelPais()
        {
            PaisSO pais = GestorDePaisos.Instancia != null ? GestorDePaisos.Instancia.PaisActual : null;
            AudioClip clip = pais != null && pais.musica != null ? pais.musica : musicaPerDefecte;
            PosarMusica(clip);
        }

        // Si ja sona la mateixa, no la torna a començar.
        public void PosarMusica(AudioClip clip)
        {
            if (_musica == null) return;
            if (clip == _musica.clip && _musica.isPlaying) { AplicarVolumDeLaMusica(); return; }
            if (_fosa != null) StopCoroutine(_fosa);
            _fosa = StartCoroutine(Canviar(clip));
        }

        private IEnumerator Canviar(AudioClip clip)
        {
            float inici = _musica.volume;
            if (_musica.isPlaying)
                for (float t = 0; t < segonsDeFosa / 2; t += Time.unscaledDeltaTime)
                {
                    _musica.volume = Mathf.Lerp(inici, 0f, t / (segonsDeFosa / 2));
                    yield return null;
                }

            _musica.Stop();
            _musica.clip = clip;
            if (clip == null) yield break;
            _musica.volume = 0f;
            _musica.Play();

            float desti = volumMaximDeLaMusica * VolumMusica;
            for (float t = 0; t < segonsDeFosa; t += Time.unscaledDeltaTime)
            {
                _musica.volume = Mathf.Lerp(0f, desti, t / segonsDeFosa);
                yield return null;
            }
            _musica.volume = desti;
            _fosa = null;
        }

        private void AplicarVolumDeLaMusica()
        {
            if (_musica != null && _fosa == null) _musica.volume = volumMaximDeLaMusica * VolumMusica;
        }

        // ---------------------------------------------------------------
        // Volums (Ajustos). Es recorden entre partides.
        // ---------------------------------------------------------------

        public void CanviarVolumDeLaMusica(float v)
        {
            VolumMusica = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(ClauMusica, VolumMusica);
            AplicarVolumDeLaMusica();
        }

        public void CanviarVolumDelsEfectes(float v)
        {
            VolumEfectes = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(ClauEfectes, VolumEfectes);
        }
    }
}
