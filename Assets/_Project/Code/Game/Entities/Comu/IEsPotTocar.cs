namespace IdleFrogs.Game
{
    // Tot el que es pot tocar al món: granotes, tren, botons, siluetes.
    //
    // Abans cadascú escoltava OnMouseDown pel seu compte, però amb el
    // desplaçament de càmera això no val: OnMouseDown salta en prémer, i
    // arrossegar la pantalla engegaria la granota que tinguessis a sota.
    // Ara un sol DetectorDeTocs decideix si ha estat un toc o un arrossegament
    // i, si ha estat un toc, crida EnTocar() al que hi hagi sota el dit.
    //
    // Cal un Collider2D al mateix GameObject (o a un pare) perquè el trobi.
    public interface IEsPotTocar
    {
        void EnTocar();
    }
}
