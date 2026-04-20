namespace AiStructuredDataFromImageExtractionDemo.Models;

/// <summary>
/// Účetní závěrka (Annual Financial Statements) according to Czech accounting regulation
/// Vyhláška č. 500/2002 Sb. Contains the main financial statements:
/// Rozvaha (Balance Sheet) and Výkaz zisku a ztráty (Income Statement).
/// </summary>
public record UcetniZaverka
{
    /// <summary>Company name (Obchodní jméno)</summary>
    public string NazevSpolecnosti { get; init; } = "";

    /// <summary>Company ID (IČO)</summary>
    public string Ico { get; init; } = "";

    /// <summary>Accounting period start date</summary>
    public DateOnly? ObdobiOd { get; init; }

    /// <summary>Accounting period end date (rozvahový den)</summary>
    public DateOnly? ObdobiDo { get; init; }

    /// <summary>Currency unit description (e.g., "v tis. Kč")</summary>
    public string MernaJednotka { get; init; } = "v tis. Kč";

    /// <summary>Date when statements were prepared (Sestaveno dne)</summary>
    public DateOnly? SestavenoDne { get; init; }

    /// <summary>Rozvaha - Aktiva (Balance Sheet - Assets)</summary>
    public RozvahaAktiva Aktiva { get; init; } = new();

    /// <summary>Rozvaha - Pasiva (Balance Sheet - Liabilities &amp; Equity)</summary>
    public RozvahaPasiva Pasiva { get; init; } = new();

    /// <summary>Výkaz zisku a ztráty (Income Statement - nature-based classification)</summary>
    public VykazZiskuAZtraty VykazZiskuAZtraty { get; init; } = new();

    // ==============================
    // Cross-statement validations
    // ==============================

    /// <summary>Validates AKTIVA CELKEM (netto) == PASIVA CELKEM (§ 4 odst. 10)</summary>
    public bool JeRozvahaVRovnovaze =>
        Aktiva.AktivaCelkem.Netto == Pasiva.PasivaCelkem.Bezne;

    /// <summary>Validates VH za účetní období in VZZ == A.V. VH běžného účetního období in Rozvaze</summary>
    public bool JeVysledekHospodareniKonzistentni =>
        VykazZiskuAZtraty.VysledekHospodareniZaObdobi.Bezne == Pasiva.AV_VysledekHospodareniBeznehoObdobi.Bezne;
}
