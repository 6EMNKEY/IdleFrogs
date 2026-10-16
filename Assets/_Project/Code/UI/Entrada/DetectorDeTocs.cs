using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using IdleFrogs.Game;

namespace IdleFrogs.UI
{
    // L'única porta d'entrada dels tocs al món.
    //
    // Prémer i deixar anar sense moure's gaire és un toc: es busca què hi ha
    // sota el dit i se li crida EnTocar(). Si el dit es mou més d'uns
    // mil·límetres és un arrossegament: es desplaça la càmera i, en deixar
    // anar, no es toca res. Per això el toc es decideix en DEIXAR ANAR i no
    // en prémer, que és el que feia OnMouseDown i el que fa impossible
    // arrossegar sense engegar granotes.
    //
    // Funciona amb ratolí i amb dit (Input System nou). Un sol component a
    // l'escena, per exemple a la càmera.
    public class DetectorDeTocs : MonoBehaviour
    {
        [SerializeField] private Camera cameraDelMon;
        [Tooltip("Opcional. Sense, els arrossegaments no fan res.")]
        [SerializeField] private ControladorDeCamera controladorDeCamera;

        [Tooltip("Quants mil·límetres de pantalla s'ha de moure el dit perquè deixi de ser un toc")]
        [SerializeField] private float millimetresPerArrossegar = 3f;

        private bool _premut;
        private bool _arrossegant;
        private bool _vaComencarSobreLaUI;
        private Vector2 _inici;
        private Vector2 _anterior;

        private readonly List<Collider2D> _colliders = new List<Collider2D>();
        private readonly List<RaycastResult> _resultatsUI = new List<RaycastResult>();
        private ContactFilter2D _filtre;
        private PointerEventData _dadesUI;
        private EventSystem _esDeLesDades;

        private void Awake()
        {
            if (cameraDelMon == null) cameraDelMon = Camera.main;
            if (controladorDeCamera == null && cameraDelMon != null)
                controladorDeCamera = cameraDelMon.GetComponent<ControladorDeCamera>();

            _filtre = ContactFilter2D.noFilter;   // inclou triggers: els botons poden ser-ho
        }

        // Dpi a 0 passa en alguns Android i a l'editor: llavors 12 px.
        private float LlindarEnPixels
        {
            get
            {
                float dpi = Screen.dpi;
                return dpi > 0 ? Mathf.Max(8f, dpi / 25.4f * millimetresPerArrossegar) : 12f;
            }
        }

        private void Update()
        {
            Pointer punter = Pointer.current;
            if (punter == null || cameraDelMon == null) return;

            Vector2 pos = punter.position.ReadValue();

            if (punter.press.wasPressedThisFrame)
            {
                _premut = true;
                _arrossegant = false;
                _inici = pos;
                _anterior = pos;
                _vaComencarSobreLaUI = SobreLaUI(pos);
                if (controladorDeCamera != null) controladorDeCamera.Aturar();
            }
            else if (_premut && punter.press.isPressed)
            {
                // Un gest que comença sobre un panell és del panell: ni toca
                // el món ni mou la càmera, encara que el dit surti del panell.
                if (!_arrossegant && !_vaComencarSobreLaUI &&
                    (pos - _inici).sqrMagnitude > LlindarEnPixels * LlindarEnPixels)
                {
                    _arrossegant = true;
                    if (controladorDeCamera != null) controladorDeCamera.ComencarArrossegament();
                }

                if (_arrossegant && controladorDeCamera != null)
                    controladorDeCamera.Arrossegar(pos - _anterior);

                _anterior = pos;
            }

            // A part de l'if de dalt: un toc molt ràpid pot prémer i deixar
            // anar dins del mateix frame.
            if (_premut && punter.press.wasReleasedThisFrame)
            {
                _premut = false;

                if (_arrossegant)
                {
                    if (controladorDeCamera != null) controladorDeCamera.Amollar();
                }
                else if (!_vaComencarSobreLaUI)
                {
                    Tocar(pos);
                }

                _arrossegant = false;
            }
        }

        private bool SobreLaUI(Vector2 posPantalla)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;

            if (_dadesUI == null || _esDeLesDades != es)
            {
                _dadesUI = new PointerEventData(es);
                _esDeLesDades = es;
            }
            _dadesUI.position = posPantalla;

            _resultatsUI.Clear();
            es.RaycastAll(_dadesUI, _resultatsUI);
            return _resultatsUI.Count > 0;
        }

        private void Tocar(Vector2 posPantalla)
        {
            float distancia = -cameraDelMon.transform.position.z;
            Vector3 mon = cameraDelMon.ScreenToWorldPoint(new Vector3(posPantalla.x, posPantalla.y, distancia));

            _colliders.Clear();
            Physics2D.OverlapPoint(mon, _filtre, _colliders);
            if (_colliders.Count == 0) return;

            IEsPotTocar millor = null;
            long millorOrdre = long.MinValue;
            float millorZ = float.PositiveInfinity;

            foreach (Collider2D c in _colliders)
            {
                if (c == null) continue;

                IEsPotTocar candidat = c.GetComponent<IEsPotTocar>() ?? c.GetComponentInParent<IEsPotTocar>();
                if (candidat == null) continue;

                // Un component apagat no rep tocs (com passava amb OnMouseDown).
                if (candidat is Behaviour b && !b.isActiveAndEnabled) continue;

                long ordre = OrdreDeDibuix(c);
                float z = c.transform.position.z;

                // Guanya el que es dibuixa a sobre; a igualtat, el més a prop
                // de la càmera (Z més petita amb la càmera a Z negativa).
                if (ordre > millorOrdre || (ordre == millorOrdre && z < millorZ))
                {
                    millor = candidat;
                    millorOrdre = ordre;
                    millorZ = z;
                }
            }

            millor?.EnTocar();
        }

        // Ordenació igual que la de dibuix: primer la capa, després l'ordre.
        private static long OrdreDeDibuix(Collider2D c)
        {
            SpriteRenderer sr = c.GetComponent<SpriteRenderer>() ?? c.GetComponentInParent<SpriteRenderer>();
            if (sr == null) return 0;
            return (long)SortingLayer.GetLayerValueFromID(sr.sortingLayerID) * 1_000_000L + sr.sortingOrder;
        }
    }
}
