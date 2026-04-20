namespace AiStructuredDataFromImageExtractionDemo.Models;

/// <summary>
/// Rozvaha - Pasiva (Balance Sheet - Liabilities &amp; Equity) according to Vyhláška č. 500/2002 Sb., Příloha č. 1.
/// Each row has Běžné účetní období and Minulé účetní období values.
/// </summary>
public record RozvahaPasiva
{
    // ==============================
    // A. Vlastní kapitál
    // ==============================

    // A.I. Základní kapitál
    public RozvahaPasivaRadek AI1_ZakladniKapital { get; init; } = new();
    public RozvahaPasivaRadek AI2_VlastniPodily { get; init; } = new();
    public RozvahaPasivaRadek AI3_ZmenyZakladnihoKapitalu { get; init; } = new();

    /// <summary>A.I. Základní kapitál</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek AI_ZakladniKapital => SumRadky(
        AI1_ZakladniKapital, AI2_VlastniPodily, AI3_ZmenyZakladnihoKapitalu);

    // A.II. Ážio a kapitálové fondy
    public RozvahaPasivaRadek AII1_Azio { get; init; } = new();
    public RozvahaPasivaRadek AII2_KapitaloveFondy { get; init; } = new();
    public RozvahaPasivaRadek AII2_1_OstatniKapitaloveFondy { get; init; } = new();
    public RozvahaPasivaRadek AII2_2_OcenovaciRozdily { get; init; } = new();
    public RozvahaPasivaRadek AII2_3_OcenovaciRozdilyPremeny { get; init; } = new();
    public RozvahaPasivaRadek AII2_4_RozdilyZPremen { get; init; } = new();
    public RozvahaPasivaRadek AII2_5_RozdilyZOceneniPremen { get; init; } = new();

    /// <summary>A.II. Ážio a kapitálové fondy</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek AII_AzioAKapitaloveFondy => SumRadky(
        AII1_Azio, AII2_KapitaloveFondy);

    // A.III. Fondy ze zisku
    public RozvahaPasivaRadek AIII1_OstatniRezervniFondy { get; init; } = new();
    public RozvahaPasivaRadek AIII2_StatutarniAOstatniFondy { get; init; } = new();

    /// <summary>A.III. Fondy ze zisku</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek AIII_FondyZeZisku => SumRadky(
        AIII1_OstatniRezervniFondy, AIII2_StatutarniAOstatniFondy);

    // A.IV. Výsledek hospodaření minulých let
    public RozvahaPasivaRadek AIV1_NerozdělenyZiskNeboNeuhrazenaZtrata { get; init; } = new();
    public RozvahaPasivaRadek AIV2_JinyVysledekHospodareni { get; init; } = new();

    /// <summary>A.IV. Výsledek hospodaření minulých let (+/-)</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek AIV_VysledekHospodareniMinulychLet => SumRadky(
        AIV1_NerozdělenyZiskNeboNeuhrazenaZtrata, AIV2_JinyVysledekHospodareni);

    // A.V. Výsledek hospodaření běžného účetního období
    public RozvahaPasivaRadek AV_VysledekHospodareniBeznehoObdobi { get; init; } = new();

    // A.VI. Rozhodnuto o zálohové výplatě podílu na zisku
    public RozvahaPasivaRadek AVI_ZalohovaVyplataPodilu { get; init; } = new();

    /// <summary>A. Vlastní kapitál = A.I + A.II + A.III + A.IV + A.V + A.VI</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek A_VlastniKapital => SumRadky(
        AI_ZakladniKapital, AII_AzioAKapitaloveFondy, AIII_FondyZeZisku,
        AIV_VysledekHospodareniMinulychLet, AV_VysledekHospodareniBeznehoObdobi,
        AVI_ZalohovaVyplataPodilu);

    // ==============================
    // B. Rezervy
    // ==============================
    public RozvahaPasivaRadek B1_RezervaNaDuchody { get; init; } = new();
    public RozvahaPasivaRadek B2_RezervaNaDanZPrijmu { get; init; } = new();
    public RozvahaPasivaRadek B3_RezervyPodleZvlastnichPredpisu { get; init; } = new();
    public RozvahaPasivaRadek B4_OstatniRezervy { get; init; } = new();

    /// <summary>B. Rezervy</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek B_Rezervy => SumRadky(
        B1_RezervaNaDuchody, B2_RezervaNaDanZPrijmu, B3_RezervyPodleZvlastnichPredpisu, B4_OstatniRezervy);

    // ==============================
    // C. Závazky
    // ==============================

    // C.I. Dlouhodobé závazky
    public RozvahaPasivaRadek CI1_VydaneDluhopisy { get; init; } = new();
    public RozvahaPasivaRadek CI1_1_VymenitelneZdluhopisy { get; init; } = new();
    public RozvahaPasivaRadek CI1_2_OstatniDluhopisy { get; init; } = new();
    public RozvahaPasivaRadek CI2_ZavazkyKUverovymInstitucim { get; init; } = new();
    public RozvahaPasivaRadek CI3_DlouhodobePrijateZalohy { get; init; } = new();
    public RozvahaPasivaRadek CI4_ZavazkyZObchodVztahu { get; init; } = new();
    public RozvahaPasivaRadek CI5_DlouhodobeSmenkyKUhrade { get; init; } = new();
    public RozvahaPasivaRadek CI6_ZavazkyOvladanaOvladajici { get; init; } = new();
    public RozvahaPasivaRadek CI7_ZavazkyPodstatnyVliv { get; init; } = new();
    public RozvahaPasivaRadek CI8_OdlozenyDanovyZavazek { get; init; } = new();
    public RozvahaPasivaRadek CI9_ZavazkyOstatni { get; init; } = new();
    public RozvahaPasivaRadek CI9_1_ZavazkyKeSpolecnikum { get; init; } = new();
    public RozvahaPasivaRadek CI9_2_DohadneUctyPasivni { get; init; } = new();
    public RozvahaPasivaRadek CI9_3_JineZavazky { get; init; } = new();

    /// <summary>C.I. Dlouhodobé závazky</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek CI_DlouhodobeZavazky => SumRadky(
        CI1_VydaneDluhopisy, CI2_ZavazkyKUverovymInstitucim, CI3_DlouhodobePrijateZalohy,
        CI4_ZavazkyZObchodVztahu, CI5_DlouhodobeSmenkyKUhrade, CI6_ZavazkyOvladanaOvladajici,
        CI7_ZavazkyPodstatnyVliv, CI8_OdlozenyDanovyZavazek, CI9_ZavazkyOstatni);

    // C.II. Krátkodobé závazky
    public RozvahaPasivaRadek CII1_VydaneDluhopisyKr { get; init; } = new();
    public RozvahaPasivaRadek CII1_1_VymenitelneZdluhopisyKr { get; init; } = new();
    public RozvahaPasivaRadek CII1_2_OstatniDluhopisyKr { get; init; } = new();
    public RozvahaPasivaRadek CII2_ZavazkyKUverovymInstitucimKr { get; init; } = new();
    public RozvahaPasivaRadek CII3_KratkodobePrijateZalohy { get; init; } = new();
    public RozvahaPasivaRadek CII4_ZavazkyZObchodVztahuKr { get; init; } = new();
    public RozvahaPasivaRadek CII5_KratkodobeSmenkyKUhrade { get; init; } = new();
    public RozvahaPasivaRadek CII6_ZavazkyOvladanaOvladajiciKr { get; init; } = new();
    public RozvahaPasivaRadek CII7_ZavazkyPodstatnyVlivKr { get; init; } = new();
    public RozvahaPasivaRadek CII8_ZavazkyOstatniKr { get; init; } = new();
    public RozvahaPasivaRadek CII8_1_ZavazkyKeSpolecnikumKr { get; init; } = new();
    public RozvahaPasivaRadek CII8_2_KratkodobeFinancniVypomoci { get; init; } = new();
    public RozvahaPasivaRadek CII8_3_ZavazkyKZamestnancum { get; init; } = new();
    public RozvahaPasivaRadek CII8_4_ZavazkyZeSocialnihoZabezpeceni { get; init; } = new();
    public RozvahaPasivaRadek CII8_5_StatDanoveZavazky { get; init; } = new();
    public RozvahaPasivaRadek CII8_6_DohadneUctyPasivniKr { get; init; } = new();
    public RozvahaPasivaRadek CII8_7_JineZavazkyKr { get; init; } = new();

    /// <summary>C.II. Krátkodobé závazky</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek CII_KratkodobeZavazky => SumRadky(
        CII1_VydaneDluhopisyKr, CII2_ZavazkyKUverovymInstitucimKr, CII3_KratkodobePrijateZalohy,
        CII4_ZavazkyZObchodVztahuKr, CII5_KratkodobeSmenkyKUhrade, CII6_ZavazkyOvladanaOvladajiciKr,
        CII7_ZavazkyPodstatnyVlivKr, CII8_ZavazkyOstatniKr);

    // C.III. Časové rozlišení pasiv (alternative position)
    public RozvahaPasivaRadek CIII_CasoveRozliseniPasiv { get; init; } = new();
    public RozvahaPasivaRadek CIII1_VydajePristichObdobi { get; init; } = new();
    public RozvahaPasivaRadek CIII2_VynosyPristichObdobi { get; init; } = new();

    /// <summary>C. Závazky = C.I + C.II</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek C_Zavazky => SumRadky(CI_DlouhodobeZavazky, CII_KratkodobeZavazky);

    /// <summary>B.+C. Cizí zdroje = B + C</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek BC_CiziZdroje => SumRadky(B_Rezervy, C_Zavazky);

    // ==============================
    // D. Časové rozlišení pasiv
    // ==============================
    public RozvahaPasivaRadek D1_VydajePristichObdobi { get; init; } = new();
    public RozvahaPasivaRadek D2_VynosyPristichObdobi { get; init; } = new();

    /// <summary>D. Časové rozlišení pasiv</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek D_CasoveRozliseniPasiv => SumRadky(
        D1_VydajePristichObdobi, D2_VynosyPristichObdobi);

    // ==============================
    // PASIVA CELKEM
    // ==============================

    /// <summary>PASIVA CELKEM = A + B+C + D</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaPasivaRadek PasivaCelkem => SumRadky(
        A_VlastniKapital, BC_CiziZdroje, D_CasoveRozliseniPasiv);

    // ==============================
    // Helper
    // ==============================
    private static RozvahaPasivaRadek SumRadky(params RozvahaPasivaRadek?[] radky)
    {
        var items = radky.Where(r => r is not null);
        return new()
        {
            Bezne = items.Sum(r => r!.Bezne),
            Minule = items.Sum(r => r!.Minule)
        };
    }
}

/// <summary>
/// Single row of the Pasiva side of Rozvaha.
/// Contains current period and prior period values.
/// </summary>
public record RozvahaPasivaRadek
{
    /// <summary>Běžné účetní období</summary>
    public decimal Bezne { get; init; }
    /// <summary>Minulé účetní období</summary>
    public decimal Minule { get; init; }
}
