using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Models;

namespace AiStructuredDataFromImageExtractionDemo.Components;

public partial class AktivaTable
{
	[Parameter, EditorRequired] public RozvahaAktiva Aktiva { get; set; } = default!;

	public sealed record AktivaRow(string Oznaceni, string Nazev, RozvahaAktivaRadek Row, int Indent, bool IsTotal);

	private Task<GridDataProviderResult<AktivaRow>> Provide(GridDataProviderRequest<AktivaRow> req)
	{
		var rows = Rows().ToList();
		return Task.FromResult(new GridDataProviderResult<AktivaRow> { Data = rows, TotalCount = rows.Count });
	}

	private IEnumerable<AktivaRow> Rows()
	{
		var a = Aktiva;
		yield return new("",        "AKTIVA CELKEM",                    a.AktivaCelkem, 0, true);
		yield return new("A.",      "Pohledávky za upsaný ZK",          a.A_PohledavkyZaUpsanyZakladniKapital, 0, false);
		yield return new("B.",      "Stálá aktiva",                     a.B_StalaAktiva, 0, false);
		yield return new("B.I.",    "Dlouhodobý nehmotný majetek",      a.BI_DlouhodobyNehmotnyMajetek, 1, false);
		yield return new("B.II.",   "Dlouhodobý hmotný majetek",        a.BII_DlouhodobyHmotnyMajetek, 1, false);
		yield return new("B.III.",  "Dlouhodobý finanční majetek",      a.BIII_DlouhodobyFinancniMajetek, 1, false);
		yield return new("C.",      "Oběžná aktiva",                    a.C_ObeznaAktiva, 0, false);
		yield return new("C.I.",    "Zásoby",                           a.CI_Zasoby, 1, false);
		yield return new("C.II.",   "Pohledávky",                       a.CII_Pohledavky, 1, false);
		yield return new("C.II.1.", "Dlouhodobé pohledávky",            a.CII1_DlouhodobePohledavky, 2, false);
		yield return new("C.II.2.", "Krátkodobé pohledávky",            a.CII2_KratkodobePohledavky, 2, false);
		yield return new("C.III.",  "Krátkodobý finanční majetek",      a.CIII_KratkodobyFinancniMajetek, 1, false);
		yield return new("C.IV.",   "Peněžní prostředky",               a.CIV_PenezniProstredky, 1, false);
		yield return new("D.",      "Časové rozlišení aktiv",           a.D_CasoveRozliseniAktiv, 0, false);
	}

	private static string RowCss(AktivaRow r)
	{
		if (r.IsTotal) return "row-total";
		var nonZero = r.Row.Brutto != 0 || r.Row.Korekce != 0 || r.Row.Netto != 0;
		var ocr = nonZero && (r.Row.Brutto + r.Row.Korekce) != r.Row.Netto;
		if (ocr) return "c-warn";
		if (r.Indent == 0) return "row-subtotal";
		return "";
	}

	private static string Fmt(decimal v) => v == 0 ? "—" : v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"));
}
