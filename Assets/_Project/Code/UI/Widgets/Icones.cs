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
        public static string Moneda => $"<space=0.12em><sprite name=\"{SpriteDeLaMoneda}\">";

        // La d'un país concret (al mapa n'hi ha de tots). Sense país, la d'ara.
        public static string MonedaDe(IdleFrogs.Game.PaisSO pais) =>
            pais != null && !string.IsNullOrEmpty(pais.spriteDeMoneda)
                ? $"<space=0.12em><sprite name=\"{pais.spriteDeMoneda}\">" : Moneda;

        // Cada país té la seva moneda: el posa AspecteDelPais en arrencar.
        public static string SpriteDeLaMoneda = "moneda";

        // La moneda premium. Va en un Sprite Asset propi (gema.asset) penjat
        // dels Fallback Sprite Assets de moneda.asset, que és el per defecte.
        public const string Gema = "<space=0.12em><sprite name=\"gema\">";

        // La lupa dels punts de recerca, tenyida del color del text (el del
        // país): un sol sprite "lupa" al Sprite Asset serveix per a tots.
        public const string Lupa = "<space=0.12em><sprite name=\"lupa\" tint=1>";

        // "12🔍" del color del país.
        public static string Punts(double punts, UnityEngine.Color color) =>
            $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGB(color)}>{IdleFrogs.Game.FormatadorDeNombres.Doblers(punts)}{Lupa}</color>";
    }
}
