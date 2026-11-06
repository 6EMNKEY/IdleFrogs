using UnityEngine;

namespace IdleFrogs.UI
{
    // El que el DetectorDeTocs demana a una càmera que es pot arrossegar: la
    // dels pantans (només en horitzontal) o la del mapa (en tots dos eixos i
    // amb zoom).
    public interface ICameraArrossegable
    {
        void Aturar();
        void ComencarArrossegament();
        void Arrossegar(Vector2 deltaPantalla);
        void Amollar();
        // Dins del gest d'ara s'ha fet zoom (pessic): en deixar anar no és un toc.
        bool HaFetZoom { get; }
    }
}
