namespace IdleFrogs.UI
{
    // Icones que van dins dels textos de TextMeshPro com a sprites en línia.
    //
    // El sprite surt del Sprite Asset per defecte de TMP (Project Settings →
    // TextMesh Pro → Default Sprite Asset), buscat pel nom. Així, quan tinguis
    // la moneda definitiva, només cal substituir la imatge mantenint el nom:
    // no s'ha de tocar cap text.
    public static class Icones
    {
        // Va darrere del número, amb un espai fi entremig.
        public const string Moneda = "<space=0.12em><sprite name=\"moneda\">";

        // La moneda premium. Va en un Sprite Asset propi (gema.asset) penjat
        // dels Fallback Sprite Assets de moneda.asset, que és el per defecte.
        public const string Gema = "<space=0.12em><sprite name=\"gema\">";
    }
}
