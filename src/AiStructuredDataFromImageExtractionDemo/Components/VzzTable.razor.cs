using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using AiStructuredDataFromImageExtractionDemo.Models;

namespace AiStructuredDataFromImageExtractionDemo.Components;

public partial class VzzTable
{
	[Parameter, EditorRequired] public VykazZiskuAZtraty Vzz { get; set; } = default!;

	public enum RowKind { Normal, Subtotal, Total }
	public sealed record VzzRow(string Oznaceni, string Nazev, VykazRadek Row, RowKind Kind);

	private Task<GridDataProviderResult<VzzRow>> Provide(GridDataProviderRequest<VzzRow> req)
	{
		var rows = Rows().ToList();
		return Task.FromResult(new GridDataProviderResult<VzzRow> { Data = rows, TotalCount = rows.Count });
	}

	private IEnumerable<VzzRow> Rows()
	{
		var v = Vzz;
		yield return new("I.",   "Tržby z prodeje výrobků a služeb",    v.I_TrzbyZProdejeVyrobkuASluzeb, RowKind.Normal);
		yield return new("II.",  "Tržby za prodej zboží",                v.II_TrzbyZaProdejZbozi, RowKind.Normal);
		yield return new("A.",   "Výkonová spotřeba",                    v.A_VykonovaSpotřeba, RowKind.Normal);
		yield return new("B.",   "Změna stavu zásob vlastní činnosti",   v.B_ZmenaStavuZasobVlastniCinnosti, RowKind.Normal);
		yield return new("C.",   "Aktivace",                             v.C_Aktivace, RowKind.Normal);
		yield return new("D.",   "Osobní náklady",                       v.D_OsobniNaklady, RowKind.Normal);
		yield return new("E.",   "Úpravy hodnot v provozní oblasti",     v.E_UpravyHodnotVProvozniOblasti, RowKind.Normal);
		yield return new("III.", "Ostatní provozní výnosy",              v.III_OstatniProvozniVynosy, RowKind.Normal);
		yield return new("F.",   "Ostatní provozní náklady",             v.F_OstatniProvozniNaklady, RowKind.Normal);
		yield return new("*",    "Provozní VH",                          v.ProvozniVysledekHospodareni, RowKind.Subtotal);
		yield return new("IV.",  "Výnosy z DFM – podíly",                v.IV_VynosyZDFMPodily, RowKind.Normal);
		yield return new("G.",   "Náklady na prodané podíly",            v.G_NakladyNaProdanePodily, RowKind.Normal);
		yield return new("V.",   "Výnosy z ostatního DFM",               v.V_VynosyZOstatnihoFinancnihoMajetku, RowKind.Normal);
		yield return new("H.",   "Náklady související s ost. DFM",       v.H_NakladySouvisejiciSOstatnimDFM, RowKind.Normal);
		yield return new("VI.",  "Výnosové úroky",                       v.VI_VynosoveUroky, RowKind.Normal);
		yield return new("J.",   "Nákladové úroky",                      v.J_NakladoveUroky, RowKind.Normal);
		yield return new("VII.", "Ostatní finanční výnosy",              v.VII_OstatniFinancniVynosy, RowKind.Normal);
		yield return new("K.",   "Ostatní finanční náklady",             v.K_OstatniFinancniNaklady, RowKind.Normal);
		yield return new("*",    "Finanční VH",                          v.FinancniVysledekHospodareni, RowKind.Subtotal);
		yield return new("**",   "VH před zdaněním",                     v.VysledekHospodareniPredZdanenim, RowKind.Subtotal);
		yield return new("L.",   "Daň z příjmů",                         v.L_DanZPrijmu, RowKind.Normal);
		yield return new("**",   "VH po zdanění",                        v.VysledekHospodareniPoZdaneni, RowKind.Subtotal);
		yield return new("M.",   "Převod podílu na VH",                  v.M_PrevodPodiluNaVH, RowKind.Normal);
		yield return new("***",  "VH za účetní období",                  v.VysledekHospodareniZaObdobi, RowKind.Total);
		yield return new("*",    "Čistý obrat",                          v.CistyObrat, RowKind.Subtotal);
	}

	private static string RowCss(VzzRow r) => r.Kind switch
	{
		RowKind.Total    => "row-total",
		RowKind.Subtotal => "row-subtotal",
		_                => ""
	};

	private static string Fmt(decimal v) => v == 0 ? "—" : v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("cs-CZ"));
}
