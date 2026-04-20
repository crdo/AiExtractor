using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Models;
using AiStructuredDataFromImageExtractionDemo.Services;

namespace AiStructuredDataFromImageExtractionDemo.Components;

public partial class CrossYearTable
{
	[Parameter, EditorRequired] public IReadOnlyList<ExtractionResult> Results { get; set; } = default!;

	private List<ExtractionResult> sorted = new();

	protected override void OnParametersSet()
	{
		sorted = Results.OrderBy(r => r.Rok ?? 0).ToList();
	}

	private record Row<T>(string Oznaceni, string Nazev, Func<T, decimal> GetBezne, Func<T, decimal> GetMinule, bool IsTotal);
	private record Consistency(string Css, string Hint);

	/// <summary>
	/// Cell coloring:
	/// - hardError: Rozvaha nebalancuje (na celkovém řádku) → red
	/// - reportedPrev matches actualPrev → green
	/// - same magnitude, sign differs → green with dashed underline (OCR artefact)
	/// - mismatch → amber
	/// - first file or both zero → neutral
	/// </summary>
	private static Consistency Classify(decimal? actualPrev, decimal reportedPrev, bool hardError)
	{
		if (hardError)
			return new("c-bad", "Mezisoučet nebalancuje / VH nesouhlasí");

		if (actualPrev is { } prev)
		{
			if (prev == 0 && reportedPrev == 0) return new("", "");
			if (prev == reportedPrev)           return new("c-ok", $"Y/Y konzistentní: minulé období = {reportedPrev:N0}");
			// Same magnitude, only sign differs — common OCR artefact.
			if (prev != 0 && Math.Abs(prev) == Math.Abs(reportedPrev))
				return new("c-ok-sign", $"Y/Y konzistentní co do velikosti, liší se jen znaménko: {reportedPrev:N0} vs. {prev:N0} (pravděpodobně OCR)");
			return new("c-warn", $"Rozpor Y/Y: tento výkaz uvádí minulé období {reportedPrev:N0}, předchozí výkaz měl {prev:N0}");
		}
		return new("", "");
	}

	private static IEnumerable<Row<RozvahaAktiva>> AktivaRows()
	{
		yield return new("",          "AKTIVA CELKEM",                  a => a.AktivaCelkem.Netto,                        a => a.AktivaCelkem.MinuleNetto,                        true);
		yield return new("A.",        "Pohledávky za upsaný ZK",        a => a.A_PohledavkyZaUpsanyZakladniKapital.Netto, a => a.A_PohledavkyZaUpsanyZakladniKapital.MinuleNetto, false);
		yield return new("B.",        "Stálá aktiva",                   a => a.B_StalaAktiva.Netto,                       a => a.B_StalaAktiva.MinuleNetto,                       true);
		yield return new("B.I.",      "Dlouhodobý nehmotný majetek",    a => a.BI_DlouhodobyNehmotnyMajetek.Netto,        a => a.BI_DlouhodobyNehmotnyMajetek.MinuleNetto,        false);
		yield return new("B.II.",     "Dlouhodobý hmotný majetek",      a => a.BII_DlouhodobyHmotnyMajetek.Netto,         a => a.BII_DlouhodobyHmotnyMajetek.MinuleNetto,         false);
		yield return new("B.III.",    "Dlouhodobý finanční majetek",    a => a.BIII_DlouhodobyFinancniMajetek.Netto,      a => a.BIII_DlouhodobyFinancniMajetek.MinuleNetto,      false);
		yield return new("C.",        "Oběžná aktiva",                  a => a.C_ObeznaAktiva.Netto,                      a => a.C_ObeznaAktiva.MinuleNetto,                      true);
		yield return new("C.I.",      "Zásoby",                         a => a.CI_Zasoby.Netto,                           a => a.CI_Zasoby.MinuleNetto,                           false);
		yield return new("C.II.",     "Pohledávky",                     a => a.CII_Pohledavky.Netto,                      a => a.CII_Pohledavky.MinuleNetto,                      false);
		yield return new("  C.II.1.", "Dlouhodobé pohledávky",          a => a.CII1_DlouhodobePohledavky.Netto,           a => a.CII1_DlouhodobePohledavky.MinuleNetto,           false);
		yield return new("  C.II.2.", "Krátkodobé pohledávky",          a => a.CII2_KratkodobePohledavky.Netto,           a => a.CII2_KratkodobePohledavky.MinuleNetto,           false);
		yield return new("C.III.",    "Krátkodobý finanční majetek",    a => a.CIII_KratkodobyFinancniMajetek.Netto,      a => a.CIII_KratkodobyFinancniMajetek.MinuleNetto,      false);
		yield return new("C.IV.",     "Peněžní prostředky",             a => a.CIV_PenezniProstredky.Netto,               a => a.CIV_PenezniProstredky.MinuleNetto,               false);
		yield return new("D.",        "Časové rozlišení aktiv",         a => a.D_CasoveRozliseniAktiv.Netto,              a => a.D_CasoveRozliseniAktiv.MinuleNetto,              true);
	}

	private static IEnumerable<Row<RozvahaPasiva>> PasivaRows()
	{
		yield return new("",       "PASIVA CELKEM",              p => p.PasivaCelkem.Bezne,                       p => p.PasivaCelkem.Minule,                       true);
		yield return new("A.",     "Vlastní kapitál",            p => p.A_VlastniKapital.Bezne,                   p => p.A_VlastniKapital.Minule,                   true);
		yield return new("A.I.",   "Základní kapitál",           p => p.AI_ZakladniKapital.Bezne,                 p => p.AI_ZakladniKapital.Minule,                 false);
		yield return new("A.II.",  "Ážio a kapitálové fondy",    p => p.AII_AzioAKapitaloveFondy.Bezne,           p => p.AII_AzioAKapitaloveFondy.Minule,           false);
		yield return new("A.III.", "Fondy ze zisku",             p => p.AIII_FondyZeZisku.Bezne,                  p => p.AIII_FondyZeZisku.Minule,                  false);
		yield return new("A.IV.",  "VH minulých let",            p => p.AIV_VysledekHospodareniMinulychLet.Bezne, p => p.AIV_VysledekHospodareniMinulychLet.Minule, false);
		yield return new("A.V.",   "VH běžného účetního období", p => p.AV_VysledekHospodareniBeznehoObdobi.Bezne, p => p.AV_VysledekHospodareniBeznehoObdobi.Minule, false);
		yield return new("B.",     "Rezervy",                    p => p.B_Rezervy.Bezne,                          p => p.B_Rezervy.Minule,                          false);
		yield return new("B.+C.",  "Cizí zdroje",                p => p.BC_CiziZdroje.Bezne,                      p => p.BC_CiziZdroje.Minule,                      true);
		yield return new("C.",     "Závazky",                    p => p.C_Zavazky.Bezne,                          p => p.C_Zavazky.Minule,                          true);
		yield return new("C.I.",   "Dlouhodobé závazky",         p => p.CI_DlouhodobeZavazky.Bezne,               p => p.CI_DlouhodobeZavazky.Minule,               false);
		yield return new("C.II.",  "Krátkodobé závazky",         p => p.CII_KratkodobeZavazky.Bezne,              p => p.CII_KratkodobeZavazky.Minule,              false);
		yield return new("D.",     "Časové rozlišení pasiv",     p => p.D_CasoveRozliseniPasiv.Bezne,             p => p.D_CasoveRozliseniPasiv.Minule,             false);
	}

	private static IEnumerable<Row<VykazZiskuAZtraty>> VzzRows()
	{
		yield return new("I.",   "Tržby z prodeje výrobků a služeb", v => v.I_TrzbyZProdejeVyrobkuASluzeb.Bezne,     v => v.I_TrzbyZProdejeVyrobkuASluzeb.Minule,    false);
		yield return new("II.",  "Tržby za prodej zboží",            v => v.II_TrzbyZaProdejZbozi.Bezne,            v => v.II_TrzbyZaProdejZbozi.Minule,            false);
		yield return new("A.",   "Výkonová spotřeba",                v => v.A_VykonovaSpotřeba.Bezne,               v => v.A_VykonovaSpotřeba.Minule,               false);
		yield return new("D.",   "Osobní náklady",                   v => v.D_OsobniNaklady.Bezne,                  v => v.D_OsobniNaklady.Minule,                  false);
		yield return new("E.",   "Úpravy hodnot v provozní oblasti", v => v.E_UpravyHodnotVProvozniOblasti.Bezne,   v => v.E_UpravyHodnotVProvozniOblasti.Minule,   false);
		yield return new("*",    "Provozní VH",                      v => v.ProvozniVysledekHospodareni.Bezne,      v => v.ProvozniVysledekHospodareni.Minule,      true);
		yield return new("*",    "Finanční VH",                      v => v.FinancniVysledekHospodareni.Bezne,      v => v.FinancniVysledekHospodareni.Minule,      true);
		yield return new("**",   "VH před zdaněním",                 v => v.VysledekHospodareniPredZdanenim.Bezne,  v => v.VysledekHospodareniPredZdanenim.Minule,  true);
		yield return new("L.",   "Daň z příjmů",                     v => v.L_DanZPrijmu.Bezne,                     v => v.L_DanZPrijmu.Minule,                     false);
		yield return new("***",  "VH za účetní období",              v => v.VysledekHospodareniZaObdobi.Bezne,      v => v.VysledekHospodareniZaObdobi.Minule,      true);
		yield return new("*",    "Čistý obrat",                      v => v.CistyObrat.Bezne,                       v => v.CistyObrat.Minule,                       true);
	}

	private static string Fmt(decimal v) => v == 0 ? "—" : v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"));
}
