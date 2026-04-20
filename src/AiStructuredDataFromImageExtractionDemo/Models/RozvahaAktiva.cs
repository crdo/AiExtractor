namespace AiStructuredDataFromImageExtractionDemo.Models;

/// <summary>
/// Rozvaha - Aktiva (Balance Sheet - Assets) according to Vyhláška č. 500/2002 Sb., Příloha č. 1.
/// Each row has Brutto, Korekce, Netto (běžné období) and Netto (minulé období).
/// </summary>
public record RozvahaAktiva
{
    // ==============================
    // A. Pohledávky za upsaný základní kapitál
    // ==============================
    public RozvahaAktivaRadek A_PohledavkyZaUpsanyZakladniKapital { get; init; } = new();

    // ==============================
    // B. Stálá aktiva
    // ==============================

    // B.I. Dlouhodobý nehmotný majetek
    public RozvahaAktivaRadek BI1_NehmotneVysledkyVyvoje { get; init; } = new();
    public RozvahaAktivaRadek BI2_OcenitelnaPrava { get; init; } = new();
    public RozvahaAktivaRadek BI2_1_Software { get; init; } = new();
    public RozvahaAktivaRadek BI2_2_OstatniOcenitelnaPrava { get; init; } = new();
    public RozvahaAktivaRadek BI3_Goodwill { get; init; } = new();
    public RozvahaAktivaRadek BI4_OstatniDlouhodobyNehmotnyMajetek { get; init; } = new();
    public RozvahaAktivaRadek BI5_PoskytnuTeZalohyNaDNMaNedokoncenyDNM { get; init; } = new();
    public RozvahaAktivaRadek BI5_1_PoskytnuTeZalohyNaDNM { get; init; } = new();
    public RozvahaAktivaRadek BI5_2_NedokoncenyDNM { get; init; } = new();

    /// <summary>B.I. Dlouhodobý nehmotný majetek = sum of B.I.1 through B.I.5</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek BI_DlouhodobyNehmotnyMajetek => SumRadky(
        BI1_NehmotneVysledkyVyvoje, BI2_OcenitelnaPrava, BI3_Goodwill,
        BI4_OstatniDlouhodobyNehmotnyMajetek, BI5_PoskytnuTeZalohyNaDNMaNedokoncenyDNM);

    // B.II. Dlouhodobý hmotný majetek
    public RozvahaAktivaRadek BII1_PozemkyAStavby { get; init; } = new();
    public RozvahaAktivaRadek BII1_1_Pozemky { get; init; } = new();
    public RozvahaAktivaRadek BII1_2_Stavby { get; init; } = new();
    public RozvahaAktivaRadek BII2_HmotneMoviteVeci { get; init; } = new();
    public RozvahaAktivaRadek BII3_OcenovaciRozdilKNabytemu { get; init; } = new();
    public RozvahaAktivaRadek BII4_OstatniDlouhodobyHmotnyMajetek { get; init; } = new();
    public RozvahaAktivaRadek BII4_1_PestitelskeCelky { get; init; } = new();
    public RozvahaAktivaRadek BII4_2_DospelaZvirata { get; init; } = new();
    public RozvahaAktivaRadek BII4_3_JinyDlouhodobyHmotnyMajetek { get; init; } = new();
    public RozvahaAktivaRadek BII5_PoskytnuTeZalohyNaDHMaNedokoncenyDHM { get; init; } = new();
    public RozvahaAktivaRadek BII5_1_PoskytnuTeZalohyNaDHM { get; init; } = new();
    public RozvahaAktivaRadek BII5_2_NedokoncenyDHM { get; init; } = new();

    /// <summary>B.II. Dlouhodobý hmotný majetek = sum of B.II.1 through B.II.5</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek BII_DlouhodobyHmotnyMajetek => SumRadky(
        BII1_PozemkyAStavby, BII2_HmotneMoviteVeci, BII3_OcenovaciRozdilKNabytemu,
        BII4_OstatniDlouhodobyHmotnyMajetek, BII5_PoskytnuTeZalohyNaDHMaNedokoncenyDHM);

    // B.III. Dlouhodobý finanční majetek
    public RozvahaAktivaRadek BIII1_PodilOvladanaOvladajici { get; init; } = new();
    public RozvahaAktivaRadek BIII2_ZapujckyUveryOvladanaOvladajici { get; init; } = new();
    public RozvahaAktivaRadek BIII3_PodilPodstatnyVliv { get; init; } = new();
    public RozvahaAktivaRadek BIII4_ZapujckyUveryPodstatnyVliv { get; init; } = new();
    public RozvahaAktivaRadek BIII5_OstatniDlouhodobeCennePapiry { get; init; } = new();
    public RozvahaAktivaRadek BIII6_ZapujckyUveryOstatni { get; init; } = new();
    public RozvahaAktivaRadek BIII7_OstatniDlouhodobyFinancniMajetek { get; init; } = new();
    public RozvahaAktivaRadek BIII7_1_JinyDlouhodobyFinancniMajetek { get; init; } = new();
    public RozvahaAktivaRadek BIII7_2_PoskytnuTeZalohyNaDFM { get; init; } = new();

    /// <summary>B.III. Dlouhodobý finanční majetek = sum of B.III.1 through B.III.7</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek BIII_DlouhodobyFinancniMajetek => SumRadky(
        BIII1_PodilOvladanaOvladajici, BIII2_ZapujckyUveryOvladanaOvladajici,
        BIII3_PodilPodstatnyVliv, BIII4_ZapujckyUveryPodstatnyVliv,
        BIII5_OstatniDlouhodobeCennePapiry, BIII6_ZapujckyUveryOstatni,
        BIII7_OstatniDlouhodobyFinancniMajetek);

    /// <summary>B. Stálá aktiva = B.I + B.II + B.III</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek B_StalaAktiva => SumRadky(
        BI_DlouhodobyNehmotnyMajetek, BII_DlouhodobyHmotnyMajetek, BIII_DlouhodobyFinancniMajetek);

    // ==============================
    // C. Oběžná aktiva
    // ==============================

    // C.I. Zásoby
    public RozvahaAktivaRadek CI1_Material { get; init; } = new();
    public RozvahaAktivaRadek CI2_NedokoncenaVyroba { get; init; } = new();
    public RozvahaAktivaRadek CI3_VyrobkyAZbozi { get; init; } = new();
    public RozvahaAktivaRadek CI3_1_Vyrobky { get; init; } = new();
    public RozvahaAktivaRadek CI3_2_Zbozi { get; init; } = new();
    public RozvahaAktivaRadek CI4_MladaZvirata { get; init; } = new();
    public RozvahaAktivaRadek CI5_PoskytnuTeZalohyNaZasoby { get; init; } = new();

    /// <summary>C.I. Zásoby = sum of C.I.1 through C.I.5</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek CI_Zasoby => SumRadky(
        CI1_Material, CI2_NedokoncenaVyroba, CI3_VyrobkyAZbozi,
        CI4_MladaZvirata, CI5_PoskytnuTeZalohyNaZasoby);

    // C.II. Pohledávky
    // C.II.1. Dlouhodobé pohledávky
    public RozvahaAktivaRadek CII1_1_PohledavkyZObchodVztahu { get; init; } = new();
    public RozvahaAktivaRadek CII1_2_PohledavkyOvladanaOvladajici { get; init; } = new();
    public RozvahaAktivaRadek CII1_3_PohledavkyPodstatnyVliv { get; init; } = new();
    public RozvahaAktivaRadek CII1_4_OdlozenaDanovaPohledavka { get; init; } = new();
    public RozvahaAktivaRadek CII1_5_PohledavkyOstatni { get; init; } = new();
    public RozvahaAktivaRadek CII1_5_1_PohledavkyZaSpolecniky { get; init; } = new();
    public RozvahaAktivaRadek CII1_5_2_DlouhodobePoskytnuTeZalohy { get; init; } = new();
    public RozvahaAktivaRadek CII1_5_3_DohadneUctyAktivni { get; init; } = new();
    public RozvahaAktivaRadek CII1_5_4_JinePohledavky { get; init; } = new();

    /// <summary>C.II.1. Dlouhodobé pohledávky</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek CII1_DlouhodobePohledavky => SumRadky(
        CII1_1_PohledavkyZObchodVztahu, CII1_2_PohledavkyOvladanaOvladajici,
        CII1_3_PohledavkyPodstatnyVliv, CII1_4_OdlozenaDanovaPohledavka,
        CII1_5_PohledavkyOstatni);

    // C.II.2. Krátkodobé pohledávky
    public RozvahaAktivaRadek CII2_1_PohledavkyZObchodVztahuKr { get; init; } = new();
    public RozvahaAktivaRadek CII2_2_PohledavkyOvladanaOvladajiciKr { get; init; } = new();
    public RozvahaAktivaRadek CII2_3_PohledavkyPodstatnyVlivKr { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_PohledavkyOstatniKr { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_1_PohledavkyZaSpolecnikyKr { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_2_SocialniZabezpeceni { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_3_StatDanovePohledavky { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_4_KratkodobePoskytnuTeZalohy { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_5_DohadneUctyAktivniKr { get; init; } = new();
    public RozvahaAktivaRadek CII2_4_6_JinePohledavkyKr { get; init; } = new();

    /// <summary>C.II.2. Krátkodobé pohledávky</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek CII2_KratkodobePohledavky => SumRadky(
        CII2_1_PohledavkyZObchodVztahuKr, CII2_2_PohledavkyOvladanaOvladajiciKr,
        CII2_3_PohledavkyPodstatnyVlivKr, CII2_4_PohledavkyOstatniKr);

    /// <summary>C.II. Pohledávky = C.II.1 + C.II.2</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek CII_Pohledavky => SumRadky(
        CII1_DlouhodobePohledavky, CII2_KratkodobePohledavky);

    // C.II.3. Časové rozlišení aktiv (alternative position)
    public RozvahaAktivaRadek CII3_CasoveRozliseniAktiv { get; init; } = new();
    public RozvahaAktivaRadek CII3_1_NakladyPristichObdobi { get; init; } = new();
    public RozvahaAktivaRadek CII3_2_KomplexniNakladyPristichObdobi { get; init; } = new();
    public RozvahaAktivaRadek CII3_3_PrijmyPristichObdobi { get; init; } = new();

    // C.III. Krátkodobý finanční majetek
    public RozvahaAktivaRadek CIII1_PodilOvladanaOvladajiciKr { get; init; } = new();
    public RozvahaAktivaRadek CIII2_OstatniKratkodobyFinancniMajetek { get; init; } = new();

    /// <summary>C.III. Krátkodobý finanční majetek</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek CIII_KratkodobyFinancniMajetek => SumRadky(
        CIII1_PodilOvladanaOvladajiciKr, CIII2_OstatniKratkodobyFinancniMajetek);

    // C.IV. Peněžní prostředky
    public RozvahaAktivaRadek CIV1_PenezniProstredkyVPokladne { get; init; } = new();
    public RozvahaAktivaRadek CIV2_PenezniProstredkyNaUctech { get; init; } = new();

    /// <summary>C.IV. Peněžní prostředky</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek CIV_PenezniProstredky => SumRadky(
        CIV1_PenezniProstredkyVPokladne, CIV2_PenezniProstredkyNaUctech);

    /// <summary>C. Oběžná aktiva = C.I + C.II + C.III + C.IV</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek C_ObeznaAktiva => SumRadky(
        CI_Zasoby, CII_Pohledavky, CIII_KratkodobyFinancniMajetek, CIV_PenezniProstredky);

    // ==============================
    // D. Časové rozlišení aktiv
    // ==============================
    public RozvahaAktivaRadek D1_NakladyPristichObdobi { get; init; } = new();
    public RozvahaAktivaRadek D2_KomplexniNakladyPristichObdobi { get; init; } = new();
    public RozvahaAktivaRadek D3_PrijmyPristichObdobi { get; init; } = new();

    /// <summary>D. Časové rozlišení aktiv</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek D_CasoveRozliseniAktiv => SumRadky(
        D1_NakladyPristichObdobi, D2_KomplexniNakladyPristichObdobi, D3_PrijmyPristichObdobi);

    // ==============================
    // AKTIVA CELKEM
    // ==============================

    /// <summary>AKTIVA CELKEM = A + B + C + D</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public RozvahaAktivaRadek AktivaCelkem => SumRadky(
        A_PohledavkyZaUpsanyZakladniKapital, B_StalaAktiva, C_ObeznaAktiva, D_CasoveRozliseniAktiv);

    // ==============================
    // Helper
    // ==============================
    private static RozvahaAktivaRadek SumRadky(params RozvahaAktivaRadek?[] radky)
    {
        var items = radky.Where(r => r is not null);
        return new()
        {
            Brutto = items.Sum(r => r!.Brutto),
            Korekce = items.Sum(r => r!.Korekce),
            Netto = items.Sum(r => r!.Netto),
            MinuleNetto = items.Sum(r => r!.MinuleNetto)
        };
    }
}

/// <summary>
/// Single row of the Aktiva side of Rozvaha.
/// Brutto = gross value, Korekce = adjustments (opravné položky + oprávky),
/// Netto = Brutto + Korekce (korekce is typically negative), MinuleNetto = prior year net.
/// </summary>
public record RozvahaAktivaRadek
{
    public decimal Brutto { get; init; }
    public decimal Korekce { get; init; }
    public decimal Netto { get; init; }
    public decimal MinuleNetto { get; init; }

    /// <summary>Computed Netto = Brutto + Korekce (validation check)</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal VypocteneNetto => Brutto + Korekce;
}
