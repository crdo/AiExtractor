using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Models;

namespace AiStructuredDataFromImageExtractionDemo.Components;

public partial class PasivaTable
{
	[Parameter, EditorRequired] public RozvahaPasiva Pasiva { get; set; } = default!;

	public sealed record PasivaRow(string Oznaceni, string Nazev, RozvahaPasivaRadek Row, int Indent, bool IsTotal);

	private Task<GridDataProviderResult<PasivaRow>> Provide(GridDataProviderRequest<PasivaRow> req)
	{
		var rows = Rows().ToList();
		return Task.FromResult(new GridDataProviderResult<PasivaRow> { Data = rows, TotalCount = rows.Count });
	}

	private IEnumerable<PasivaRow> Rows()
	{
		var p = Pasiva;
		yield return new("",      "PASIVA CELKEM",                p.PasivaCelkem, 0, true);
		yield return new("A.",    "Vlastní kapitál",              p.A_VlastniKapital, 0, false);
		yield return new("A.I.",  "Základní kapitál",             p.AI_ZakladniKapital, 1, false);
		yield return new("A.II.", "Ážio a kapitálové fondy",      p.AII_AzioAKapitaloveFondy, 1, false);
		yield return new("A.III.","Fondy ze zisku",               p.AIII_FondyZeZisku, 1, false);
		yield return new("A.IV.", "VH minulých let",              p.AIV_VysledekHospodareniMinulychLet, 1, false);
		yield return new("A.V.",  "VH běžného účetního období",   p.AV_VysledekHospodareniBeznehoObdobi, 1, false);
		yield return new("A.VI.", "Zálohy na výplatu podílu",     p.AVI_ZalohovaVyplataPodilu, 1, false);
		yield return new("B.",    "Rezervy",                      p.B_Rezervy, 0, false);
		yield return new("B.+C.", "Cizí zdroje",                  p.BC_CiziZdroje, 0, false);
		yield return new("C.",    "Závazky",                      p.C_Zavazky, 0, false);
		yield return new("C.I.",  "Dlouhodobé závazky",           p.CI_DlouhodobeZavazky, 1, false);
		yield return new("C.II.", "Krátkodobé závazky",           p.CII_KratkodobeZavazky, 1, false);
		yield return new("D.",    "Časové rozlišení pasiv",       p.D_CasoveRozliseniPasiv, 0, false);
	}

	private static string RowCss(PasivaRow r)
	{
		if (r.IsTotal) return "row-total";
		if (r.Indent == 0) return "row-subtotal";
		return "";
	}

	private static string Fmt(decimal v) => v == 0 ? "—" : v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"));
}
