using AiStructuredDataFromImageExtractionDemo.Models;
using AiStructuredDataFromImageExtractionDemo.Services;

namespace AiStructuredDataFromImageExtractionDemo.Rendering;

/// <summary>
/// Console rendering of UcetniZaverka data.
/// Designed to be replaced later with UI table rendering.
/// </summary>
public static class ConsoleRenderer
{
	// ============================================================
	// ANSI color codes for console output
	// ============================================================
	private const string Red = "\x1b[31m";
	private const string Green = "\x1b[32m";
	private const string Yellow = "\x1b[33m";
	private const string RedBg = "\x1b[41;97m";  // red background + white text
	private const string Reset = "\x1b[0m";

	// ============================================================
	// Single-file detailed output
	// ============================================================

	public static void RenderDetail(UcetniZaverka uz)
	{
		Console.WriteLine("\n========================================");
		Console.WriteLine("  ÚČETNÍ ZÁVĚRKA - EXTRAHOVANÁ DATA");
		Console.WriteLine("========================================");

		Console.WriteLine($"\nSpolečnost: {uz.NazevSpolecnosti}");
		Console.WriteLine($"IČO:        {uz.Ico}");
		Console.WriteLine($"Období:     {uz.ObdobiOd?.ToString("dd.MM.yyyy") ?? "?"} — {uz.ObdobiDo?.ToString("dd.MM.yyyy") ?? "?"}");
		Console.WriteLine($"Jednotka:   {uz.MernaJednotka}");

		RenderAktiva(uz.Aktiva, highlightBalance: !uz.JeRozvahaVRovnovaze);
		RenderPasiva(uz.Pasiva, highlightBalance: !uz.JeRozvahaVRovnovaze, highlightVH: !uz.JeVysledekHospodareniKonzistentni);
		RenderVzz(uz.VykazZiskuAZtraty, highlightVH: !uz.JeVysledekHospodareniKonzistentni);
		RenderValidation(uz);
	}

	/// <summary>
	/// Render detail for a single extraction result including API metrics.
	/// </summary>
	public static void RenderDetail(ExtractionResult result)
	{
		RenderDetail(result.UcetniZaverka);
		RenderMetrics(result.Metrics);
	}

	private static void RenderMetrics(ExtractionMetrics? m)
	{
		if (m is null) return;
		Console.WriteLine("\n--- API METRIKY ---");
		Console.WriteLine($"  Celkový čas:          {m.TotalDuration.TotalSeconds:F1}s");

		if (m.MarkdownFromCache)
			Console.WriteLine($"  Document Intelligence: cache (0 volání)");
		else
			Console.WriteLine($"  Document Intelligence: {m.DocumentIntelligenceDuration.TotalSeconds:F1}s ({m.DocumentIntelligenceCalls} volání)");

		Console.WriteLine($"  GPT-5.1 volání:        {m.GptCalls}x ({m.BalanceRetries} opakování validace)");
		Console.WriteLine($"  GPT-5.1 celkový čas:   {m.GptTotalDuration.TotalSeconds:F1}s");
		Console.WriteLine($"  GPT-5.1 tokeny:        {m.GptInputTokens:N0} vstup + {m.GptOutputTokens:N0} výstup = {m.GptTotalTokens:N0} celkem");

		if (m.GptCallDetails.Count > 0)
		{
			Console.WriteLine($"  {"Volání",-18} {"Čas",6} {"Vstup",8} {"Výstup",8} {"Pokus",5} {"Stav",4}");
			foreach (var d in m.GptCallDetails)
				Console.WriteLine($"  {d.Label,-18} {d.Duration.TotalSeconds,5:F1}s {d.InputTokens,8:N0} {d.OutputTokens,8:N0} {d.Attempt,5} {(d.Success ? "✓" : "✗"),4}");
		}
	}

	private static void RenderAktiva(RozvahaAktiva a, bool highlightBalance = false)
	{
		Console.WriteLine("\n--- ROZVAHA - AKTIVA ---");
		Console.WriteLine($"{"",-14} {"",-36} {"Brutto",12} {"Korekce",12} {"Netto",12} {"Minulé",12}");
		PA("", "AKTIVA CELKEM", a.AktivaCelkem, highlight: highlightBalance);
		PA("A.", "Pohledávky za upsaný ZK", a.A_PohledavkyZaUpsanyZakladniKapital);
		PA("B.", "Stálá aktiva", a.B_StalaAktiva);
		PA("B.I.", "Dlouhodobý nehmotný majetek", a.BI_DlouhodobyNehmotnyMajetek);
		PA("  B.I.1.", "Nehmotné výsledky vývoje", a.BI1_NehmotneVysledkyVyvoje);
		PA("  B.I.2.", "Ocenitelná práva", a.BI2_OcenitelnaPrava);
		PA("    B.I.2.1.", "Software", a.BI2_1_Software);
		PA("    B.I.2.2.", "Ostatní ocenitelná práva", a.BI2_2_OstatniOcenitelnaPrava);
		PA("  B.I.3.", "Goodwill", a.BI3_Goodwill);
		PA("  B.I.4.", "Ostatní DNM", a.BI4_OstatniDlouhodobyNehmotnyMajetek);
		PA("  B.I.5.", "Zálohy na DNM a nedokončený DNM", a.BI5_PoskytnuTeZalohyNaDNMaNedokoncenyDNM);
		PA("    B.I.5.1.", "Poskytnuté zálohy na DNM", a.BI5_1_PoskytnuTeZalohyNaDNM);
		PA("    B.I.5.2.", "Nedokončený DNM", a.BI5_2_NedokoncenyDNM);
		PA("B.II.", "Dlouhodobý hmotný majetek", a.BII_DlouhodobyHmotnyMajetek);
		PA("  B.II.1.", "Pozemky a stavby", a.BII1_PozemkyAStavby);
		PA("    B.II.1.1.", "Pozemky", a.BII1_1_Pozemky);
		PA("    B.II.1.2.", "Stavby", a.BII1_2_Stavby);
		PA("  B.II.2.", "Hmotné movité věci", a.BII2_HmotneMoviteVeci);
		PA("  B.II.3.", "Oceňovací rozdíl k nabytému maj.", a.BII3_OcenovaciRozdilKNabytemu);
		PA("  B.II.4.", "Ostatní DHM", a.BII4_OstatniDlouhodobyHmotnyMajetek);
		PA("    B.II.4.1.", "Pěstitelské celky", a.BII4_1_PestitelskeCelky);
		PA("    B.II.4.2.", "Dospělá zvířata", a.BII4_2_DospelaZvirata);
		PA("    B.II.4.3.", "Jiný DHM", a.BII4_3_JinyDlouhodobyHmotnyMajetek);
		PA("  B.II.5.", "Zálohy na DHM a nedokončený DHM", a.BII5_PoskytnuTeZalohyNaDHMaNedokoncenyDHM);
		PA("    B.II.5.1.", "Poskytnuté zálohy na DHM", a.BII5_1_PoskytnuTeZalohyNaDHM);
		PA("    B.II.5.2.", "Nedokončený DHM", a.BII5_2_NedokoncenyDHM);
		PA("B.III.", "Dlouhodobý finanční majetek", a.BIII_DlouhodobyFinancniMajetek);
		PA("  B.III.1.", "Podíly - ovládaná/ovládající", a.BIII1_PodilOvladanaOvladajici);
		PA("  B.III.2.", "Zápůjčky - ovládaná/ovládající", a.BIII2_ZapujckyUveryOvladanaOvladajici);
		PA("  B.III.3.", "Podíly - podstatný vliv", a.BIII3_PodilPodstatnyVliv);
		PA("  B.III.4.", "Zápůjčky - podstatný vliv", a.BIII4_ZapujckyUveryPodstatnyVliv);
		PA("  B.III.5.", "Ostatní dlouhodobé CP", a.BIII5_OstatniDlouhodobeCennePapiry);
		PA("  B.III.6.", "Zápůjčky a úvěry - ostatní", a.BIII6_ZapujckyUveryOstatni);
		PA("  B.III.7.", "Ostatní DFM", a.BIII7_OstatniDlouhodobyFinancniMajetek);
		PA("    B.III.7.1.", "Jiný DFM", a.BIII7_1_JinyDlouhodobyFinancniMajetek);
		PA("    B.III.7.2.", "Zálohy na DFM", a.BIII7_2_PoskytnuTeZalohyNaDFM);
		PA("C.", "Oběžná aktiva", a.C_ObeznaAktiva);
		PA("C.I.", "Zásoby", a.CI_Zasoby);
		PA("  C.I.1.", "Materiál", a.CI1_Material);
		PA("  C.I.2.", "Nedokončená výroba a polotovary", a.CI2_NedokoncenaVyroba);
		PA("  C.I.3.", "Výrobky a zboží", a.CI3_VyrobkyAZbozi);
		PA("    C.I.3.1.", "Výrobky", a.CI3_1_Vyrobky);
		PA("    C.I.3.2.", "Zboží", a.CI3_2_Zbozi);
		PA("  C.I.4.", "Mladá a ostatní zvířata", a.CI4_MladaZvirata);
		PA("  C.I.5.", "Poskytnuté zálohy na zásoby", a.CI5_PoskytnuTeZalohyNaZasoby);
		PA("C.II.", "Pohledávky", a.CII_Pohledavky);
		PA("  C.II.1.", "Dlouhodobé pohledávky", a.CII1_DlouhodobePohledavky);
		PA("    C.II.1.1.", "Z obchodních vztahů", a.CII1_1_PohledavkyZObchodVztahu);
		PA("    C.II.1.2.", "Ovládaná/ovládající", a.CII1_2_PohledavkyOvladanaOvladajici);
		PA("    C.II.1.3.", "Podstatný vliv", a.CII1_3_PohledavkyPodstatnyVliv);
		PA("    C.II.1.4.", "Odložená daňová pohledávka", a.CII1_4_OdlozenaDanovaPohledavka);
		PA("    C.II.1.5.", "Pohledávky - ostatní", a.CII1_5_PohledavkyOstatni);
		PA("      C.II.1.5.1.", "Za společníky", a.CII1_5_1_PohledavkyZaSpolecniky);
		PA("      C.II.1.5.2.", "Dlouhodobé zálohy", a.CII1_5_2_DlouhodobePoskytnuTeZalohy);
		PA("      C.II.1.5.3.", "Dohadné účty aktivní", a.CII1_5_3_DohadneUctyAktivni);
		PA("      C.II.1.5.4.", "Jiné pohledávky", a.CII1_5_4_JinePohledavky);
		PA("  C.II.2.", "Krátkodobé pohledávky", a.CII2_KratkodobePohledavky);
		PA("    C.II.2.1.", "Z obchodních vztahů", a.CII2_1_PohledavkyZObchodVztahuKr);
		PA("    C.II.2.2.", "Ovládaná/ovládající", a.CII2_2_PohledavkyOvladanaOvladajiciKr);
		PA("    C.II.2.3.", "Podstatný vliv", a.CII2_3_PohledavkyPodstatnyVlivKr);
		PA("    C.II.2.4.", "Pohledávky - ostatní", a.CII2_4_PohledavkyOstatniKr);
		PA("      C.II.2.4.1.", "Za společníky", a.CII2_4_1_PohledavkyZaSpolecnikyKr);
		PA("      C.II.2.4.2.", "Sociální zabezpečení", a.CII2_4_2_SocialniZabezpeceni);
		PA("      C.II.2.4.3.", "Stát - daňové pohledávky", a.CII2_4_3_StatDanovePohledavky);
		PA("      C.II.2.4.4.", "Krátkodobé zálohy", a.CII2_4_4_KratkodobePoskytnuTeZalohy);
		PA("      C.II.2.4.5.", "Dohadné účty aktivní", a.CII2_4_5_DohadneUctyAktivniKr);
		PA("      C.II.2.4.6.", "Jiné pohledávky", a.CII2_4_6_JinePohledavkyKr);
		PA("C.III.", "Krátkodobý finanční majetek", a.CIII_KratkodobyFinancniMajetek);
		PA("  C.III.1.", "Podíly - ovládaná/ovládající", a.CIII1_PodilOvladanaOvladajiciKr);
		PA("  C.III.2.", "Ostatní KFM", a.CIII2_OstatniKratkodobyFinancniMajetek);
		PA("C.IV.", "Peněžní prostředky", a.CIV_PenezniProstredky);
		PA("  C.IV.1.", "Peněžní prostředky v pokladně", a.CIV1_PenezniProstredkyVPokladne);
		PA("  C.IV.2.", "Peněžní prostředky na účtech", a.CIV2_PenezniProstredkyNaUctech);
		PA("D.", "Časové rozlišení aktiv", a.D_CasoveRozliseniAktiv);
		PA("  D.1.", "Náklady příštích období", a.D1_NakladyPristichObdobi);
		PA("  D.2.", "Komplexní náklady příštích období", a.D2_KomplexniNakladyPristichObdobi);
		PA("  D.3.", "Příjmy příštích období", a.D3_PrijmyPristichObdobi);
	}

	private static void RenderPasiva(RozvahaPasiva p, bool highlightBalance = false, bool highlightVH = false)
	{
		Console.WriteLine("\n--- ROZVAHA - PASIVA ---");
		Console.WriteLine($"{"",-14} {"",-36} {"Běžné",12} {"Minulé",12}");
		PP("", "PASIVA CELKEM", p.PasivaCelkem, highlight: highlightBalance);
		PP("A.", "Vlastní kapitál", p.A_VlastniKapital);
		PP("A.I.", "Základní kapitál", p.AI_ZakladniKapital);
		PP("  A.I.1.", "Základní kapitál", p.AI1_ZakladniKapital);
		PP("  A.I.2.", "Vlastní podíly", p.AI2_VlastniPodily);
		PP("  A.I.3.", "Změny základního kapitálu", p.AI3_ZmenyZakladnihoKapitalu);
		PP("A.II.", "Ážio a kapitálové fondy", p.AII_AzioAKapitaloveFondy);
		PP("  A.II.1.", "Ážio", p.AII1_Azio);
		PP("  A.II.2.", "Kapitálové fondy", p.AII2_KapitaloveFondy);
		PP("    A.II.2.1.", "Ostatní kapitálové fondy", p.AII2_1_OstatniKapitaloveFondy);
		PP("    A.II.2.2.", "Oceňovací rozdíly", p.AII2_2_OcenovaciRozdily);
		PP("A.III.", "Fondy ze zisku", p.AIII_FondyZeZisku);
		PP("  A.III.1.", "Ostatní rezervní fondy", p.AIII1_OstatniRezervniFondy);
		PP("  A.III.2.", "Statutární a ostatní fondy", p.AIII2_StatutarniAOstatniFondy);
		PP("A.IV.", "VH minulých let", p.AIV_VysledekHospodareniMinulychLet);
		PP("  A.IV.1.", "Nerozdělený zisk/neuhrazená ztráta", p.AIV1_NerozdělenyZiskNeboNeuhrazenaZtrata);
		PP("  A.IV.2.", "Jiný VH minulých let", p.AIV2_JinyVysledekHospodareni);
		PP("A.V.", "VH běžného účetního období", p.AV_VysledekHospodareniBeznehoObdobi, highlight: highlightVH);
		PP("A.VI.", "Zálohy na výplatu podílu na zisku", p.AVI_ZalohovaVyplataPodilu);
		PP("B.", "Rezervy", p.B_Rezervy);
		PP("  B.1.", "Rezerva na důchody", p.B1_RezervaNaDuchody);
		PP("  B.2.", "Rezerva na daň z příjmů", p.B2_RezervaNaDanZPrijmu);
		PP("  B.3.", "Rezervy dle zvláštních předpisů", p.B3_RezervyPodleZvlastnichPredpisu);
		PP("  B.4.", "Ostatní rezervy", p.B4_OstatniRezervy);
		PP("B.+C.", "Cizí zdroje", p.BC_CiziZdroje);
		PP("C.", "Závazky", p.C_Zavazky);
		PP("C.I.", "Dlouhodobé závazky", p.CI_DlouhodobeZavazky);
		PP("  C.I.1.", "Vydané dluhopisy", p.CI1_VydaneDluhopisy);
		PP("  C.I.2.", "Závazky k úvěrovým institucím", p.CI2_ZavazkyKUverovymInstitucim);
		PP("  C.I.3.", "Dlouhodobé přijaté zálohy", p.CI3_DlouhodobePrijateZalohy);
		PP("  C.I.4.", "Závazky z obch. vztahů", p.CI4_ZavazkyZObchodVztahu);
		PP("  C.I.5.", "Dlouhodobé směnky k úhradě", p.CI5_DlouhodobeSmenkyKUhrade);
		PP("  C.I.6.", "Závazky - ovládaná/ovládající", p.CI6_ZavazkyOvladanaOvladajici);
		PP("  C.I.7.", "Závazky - podstatný vliv", p.CI7_ZavazkyPodstatnyVliv);
		PP("  C.I.8.", "Odložený daňový závazek", p.CI8_OdlozenyDanovyZavazek);
		PP("  C.I.9.", "Závazky - ostatní", p.CI9_ZavazkyOstatni);
		PP("    C.I.9.1.", "Ke společníkům", p.CI9_1_ZavazkyKeSpolecnikum);
		PP("    C.I.9.2.", "Dohadné účty pasivní", p.CI9_2_DohadneUctyPasivni);
		PP("    C.I.9.3.", "Jiné závazky", p.CI9_3_JineZavazky);
		PP("C.II.", "Krátkodobé závazky", p.CII_KratkodobeZavazky);
		PP("  C.II.1.", "Vydané dluhopisy", p.CII1_VydaneDluhopisyKr);
		PP("  C.II.2.", "Závazky k úvěrovým institucím", p.CII2_ZavazkyKUverovymInstitucimKr);
		PP("  C.II.3.", "Krátkodobé přijaté zálohy", p.CII3_KratkodobePrijateZalohy);
		PP("  C.II.4.", "Závazky z obch. vztahů", p.CII4_ZavazkyZObchodVztahuKr);
		PP("  C.II.5.", "Krátkodobé směnky k úhradě", p.CII5_KratkodobeSmenkyKUhrade);
		PP("  C.II.6.", "Závazky - ovládaná/ovládající", p.CII6_ZavazkyOvladanaOvladajiciKr);
		PP("  C.II.7.", "Závazky - podstatný vliv", p.CII7_ZavazkyPodstatnyVlivKr);
		PP("  C.II.8.", "Závazky - ostatní", p.CII8_ZavazkyOstatniKr);
		PP("    C.II.8.1.", "Ke společníkům", p.CII8_1_ZavazkyKeSpolecnikumKr);
		PP("    C.II.8.2.", "Krátkodobé fin. výpomoci", p.CII8_2_KratkodobeFinancniVypomoci);
		PP("    C.II.8.3.", "Závazky k zaměstnancům", p.CII8_3_ZavazkyKZamestnancum);
		PP("    C.II.8.4.", "Závazky ze soc. zabezpečení", p.CII8_4_ZavazkyZeSocialnihoZabezpeceni);
		PP("    C.II.8.5.", "Stát - daňové závazky", p.CII8_5_StatDanoveZavazky);
		PP("    C.II.8.6.", "Dohadné účty pasivní", p.CII8_6_DohadneUctyPasivniKr);
		PP("    C.II.8.7.", "Jiné závazky", p.CII8_7_JineZavazkyKr);
		PP("D.", "Časové rozlišení pasiv", p.D_CasoveRozliseniPasiv);
		PP("  D.1.", "Výdaje příštích období", p.D1_VydajePristichObdobi);
		PP("  D.2.", "Výnosy příštích období", p.D2_VynosyPristichObdobi);
	}

	private static void RenderVzz(VykazZiskuAZtraty v, bool highlightVH = false)
	{
		Console.WriteLine("\n--- VÝKAZ ZISKU A ZTRÁTY ---");
		Console.WriteLine($"{"",14} {"",36} {"Běžné",12} {"Minulé",12}");
		PV("I.", "Tržby z prodeje výrobků a služeb", v.I_TrzbyZProdejeVyrobkuASluzeb);
		PV("II.", "Tržby za prodej zboží", v.II_TrzbyZaProdejZbozi);
		PV("A.", "Výkonová spotřeba", v.A_VykonovaSpotřeba);
		PV("  A.1.", "Náklady na prodané zboží", v.A1_NakladyVynalozeneNaProdaneZbozi);
		PV("  A.2.", "Spotřeba materiálu a energie", v.A2_SpotrebaMaterialuAEnergie);
		PV("  A.3.", "Služby", v.A3_Sluzby);
		PV("B.", "Změna stavu zásob vlastní činnosti", v.B_ZmenaStavuZasobVlastniCinnosti);
		PV("C.", "Aktivace", v.C_Aktivace);
		PV("D.", "Osobní náklady", v.D_OsobniNaklady);
		PV("  D.1.", "Mzdové náklady", v.D1_MzdoveNaklady);
		PV("  D.2.", "Náklady na soc. zabezpečení", v.D2_NakladyNaSocialniZabezpeceni);
		PV("    D.2.1.", "Soc. a zdravotní pojištění", v.D2_1_SocialniZabezpeceniAZdravotniPojisteni);
		PV("    D.2.2.", "Ostatní náklady", v.D2_2_OstatniNaklady);
		PV("E.", "Úpravy hodnot v provozní oblasti", v.E_UpravyHodnotVProvozniOblasti);
		PV("  E.1.", "Úpravy hodnot DNM a DHM", v.E1_UpravyHodnotDNMaDHM);
		PV("    E.1.1.", "Trvalé", v.E1_1_Trvale);
		PV("    E.1.2.", "Dočasné", v.E1_2_Docasne);
		PV("  E.2.", "Úpravy hodnot zásob", v.E2_UpravyHodnotZasob);
		PV("  E.3.", "Úpravy hodnot pohledávek", v.E3_UpravyHodnotPohledavek);
		PV("III.", "Ostatní provozní výnosy", v.III_OstatniProvozniVynosy);
		PV("  III.1.", "Tržby z prodaného DM", v.III1_TrzbyZProdanehoDM);
		PV("  III.2.", "Tržby z prodaného materiálu", v.III2_TrzbyZProdanehoMaterialu);
		PV("  III.3.", "Jiné provozní výnosy", v.III3_JineProvozniVynosy);
		PV("F.", "Ostatní provozní náklady", v.F_OstatniProvozniNaklady);
		PV("  F.1.", "ZC prodaného DM", v.F1_ZustatkovaCenaProdanehoDM);
		PV("  F.2.", "Prodaný materiál", v.F2_ProdanyMaterial);
		PV("  F.3.", "Daně a poplatky", v.F3_DaneAPoplatky);
		PV("  F.4.", "Rezervy v provozní oblasti", v.F4_RezerveVProvozniOblasti);
		PV("  F.5.", "Jiné provozní náklady", v.F5_JineProvozniNaklady);
		PV("*", "Provozní VH", v.ProvozniVysledekHospodareni);
		PV("IV.", "Výnosy z DFM - podíly", v.IV_VynosyZDFMPodily);
		PV("  IV.1.", "Výnosy z podílů - ovládaná", v.IV1_VynosyZPodiluOvladana);
		PV("  IV.2.", "Ostatní výnosy z podílů", v.IV2_OstatniVynosyZPodilu);
		PV("G.", "Náklady na prodané podíly", v.G_NakladyNaProdanePodily);
		PV("V.", "Výnosy z ostatního DFM", v.V_VynosyZOstatnihoFinancnihoMajetku);
		PV("H.", "Náklady související s ost. DFM", v.H_NakladySouvisejiciSOstatnimDFM);
		PV("VI.", "Výnosové úroky", v.VI_VynosoveUroky);
		PV("  VI.1.", "Výnosové úroky - ovládaná", v.VI1_VynosoveUrokyOvladana);
		PV("  VI.2.", "Ostatní výnosové úroky", v.VI2_OstatniVynosoveUroky);
		PV("I.fin.", "Úpravy hodnot ve fin. oblasti", v.I_UpravyHodnotVeFinancniOblasti);
		PV("J.", "Nákladové úroky", v.J_NakladoveUroky);
		PV("  J.1.", "Nákladové úroky - ovládaná", v.J1_NakladoveUrokyOvladana);
		PV("  J.2.", "Ostatní nákladové úroky", v.J2_OstatniNakladoveUroky);
		PV("VII.", "Ostatní finanční výnosy", v.VII_OstatniFinancniVynosy);
		PV("K.", "Ostatní finanční náklady", v.K_OstatniFinancniNaklady);
		PV("*", "Finanční VH", v.FinancniVysledekHospodareni);
		PV("**", "VH před zdaněním", v.VysledekHospodareniPredZdanenim);
		PV("L.", "Daň z příjmů", v.L_DanZPrijmu);
		PV("  L.1.", "Daň z příjmů splatná", v.L1_DanZPrijmuSplatna);
		PV("  L.2.", "Daň z příjmů odložená", v.L2_DanZPrijmuOdlozena);
		PV("**", "VH po zdanění", v.VysledekHospodareniPoZdaneni);
		PV("M.", "Převod podílu na VH", v.M_PrevodPodiluNaVH);
		PV("***", "VH za účetní období", v.VysledekHospodareniZaObdobi, highlight: highlightVH);
		PV("*", "Čistý obrat", v.CistyObrat);
	}

	private static void RenderValidation(UcetniZaverka uz)
	{
		Console.WriteLine("\n--- VALIDACE ---");
		var balColor = uz.JeRozvahaVRovnovaze ? Green : Red;
		var vhColor = uz.JeVysledekHospodareniKonzistentni ? Green : Red;
		Console.WriteLine($"{balColor}Rozvaha v rovnováze: {Fmt(uz.JeRozvahaVRovnovaze)}  (A={uz.Aktiva.AktivaCelkem.Netto:N0}, P={uz.Pasiva.PasivaCelkem.Bezne:N0}){Reset}");
		Console.WriteLine($"{vhColor}VH konzistentní:    {Fmt(uz.JeVysledekHospodareniKonzistentni)}  (VZZ={uz.VykazZiskuAZtraty.VysledekHospodareniZaObdobi.Bezne:N0}, A.V.={uz.Pasiva.AV_VysledekHospodareniBeznehoObdobi?.Bezne:N0}){Reset}");
	}

	// ============================================================
	// Cross-year summary table
	// ============================================================

	public static void RenderSummary(List<ExtractionResult> results)
	{
		if (results.Count == 0) return;

		var sorted = results.OrderBy(r => r.Rok).ToList();
		var years = sorted.Select(r =>
		{
			var fileName = Path.GetFileNameWithoutExtension(r.SourceFile);
			var year = r.Rok?.ToString() ?? "?";
			return $"{year} ({fileName})";
		}).ToArray();
		var colWidth = Math.Max(14, years.Max(y => y.Length) + 2);

		Console.WriteLine("\n");
		Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
		Console.WriteLine("║                  PŘEHLEDOVÁ TABULKA ACROSS YEARS                             ║");
		Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
		Console.WriteLine($"\nSpolečnost: {sorted[0].UcetniZaverka.NazevSpolecnosti}");
		Console.WriteLine($"IČO:        {sorted[0].UcetniZaverka.Ico}");
		Console.WriteLine($"Jednotka:   tis. Kč");

		// --- ROZVAHA AKTIVA summary (Brutto, Korekce, Netto) ---
		RenderRozvahaAktivaSummary(sorted, years, colWidth);

		// --- ROZVAHA PASIVA summary (Běžné) ---
		RenderRozvahaPasivaSummary(sorted, years, colWidth);

		// --- VZZ summary (Běžné) ---
		RenderVzzSummary(sorted, years, colWidth);

		// --- Validation summary ---
		RenderValidationSummary(sorted, years, colWidth);

		// --- Metrics summary ---
		RenderMetricsSummary(sorted, years, colWidth);
	}

	private static void RenderRozvahaAktivaSummary(List<ExtractionResult> sorted, string[] years, int w)
	{
		Console.WriteLine("\n--- ROZVAHA - AKTIVA (Netto) ---");
		var header = $"{"Označení",-10} {"Položka",-36}";
		foreach (var y in years) header += $" {y.PadLeft(w)}";
		Console.WriteLine(header);
		Console.WriteLine(new string('─', header.Length));

		var rows = new (string oz, string nazev, Func<RozvahaAktiva, decimal> getValue)[]
		{
			("", "AKTIVA CELKEM", a => a.AktivaCelkem.Netto),
			("A.", "Pohledávky za upsaný ZK", a => a.A_PohledavkyZaUpsanyZakladniKapital.Netto),
			("B.", "Stálá aktiva", a => a.B_StalaAktiva.Netto),
			("B.I.", "Dlouhodobý nehmotný majetek", a => a.BI_DlouhodobyNehmotnyMajetek.Netto),
			("B.II.", "Dlouhodobý hmotný majetek", a => a.BII_DlouhodobyHmotnyMajetek.Netto),
			("B.III.", "Dlouhodobý finanční majetek", a => a.BIII_DlouhodobyFinancniMajetek.Netto),
			("C.", "Oběžná aktiva", a => a.C_ObeznaAktiva.Netto),
			("C.I.", "Zásoby", a => a.CI_Zasoby.Netto),
			("C.II.", "Pohledávky", a => a.CII_Pohledavky.Netto),
			("  C.II.1.", "Dlouhodobé pohledávky", a => a.CII1_DlouhodobePohledavky.Netto),
			("  C.II.2.", "Krátkodobé pohledávky", a => a.CII2_KratkodobePohledavky.Netto),
			("C.III.", "Krátkodobý finanční majetek", a => a.CIII_KratkodobyFinancniMajetek.Netto),
			("C.IV.", "Peněžní prostředky", a => a.CIV_PenezniProstredky.Netto),
			("D.", "Časové rozlišení aktiv", a => a.D_CasoveRozliseniAktiv.Netto),
		};

		foreach (var (oz, nazev, getValue) in rows)
		{
			var line = $"{oz,-10} {nazev,-36}";
			foreach (var r in sorted) line += $" {getValue(r.UcetniZaverka.Aktiva).ToString("N0").PadLeft(w)}";
			Console.WriteLine(line);
		}
	}

	private static void RenderRozvahaPasivaSummary(List<ExtractionResult> sorted, string[] years, int w)
	{
		Console.WriteLine("\n--- ROZVAHA - PASIVA (Běžné) ---");
		var header = $"{"Označení",-10} {"Položka",-36}";
		foreach (var y in years) header += $" {y.PadLeft(w)}";
		Console.WriteLine(header);
		Console.WriteLine(new string('─', header.Length));

		var rows = new (string oz, string nazev, Func<RozvahaPasiva, decimal> getValue)[]
		{
			("", "PASIVA CELKEM", p => p.PasivaCelkem.Bezne),
			("A.", "Vlastní kapitál", p => p.A_VlastniKapital.Bezne),
			("A.I.", "Základní kapitál", p => p.AI_ZakladniKapital.Bezne),
			("A.II.", "Ážio a kapitálové fondy", p => p.AII_AzioAKapitaloveFondy.Bezne),
			("A.III.", "Fondy ze zisku", p => p.AIII_FondyZeZisku.Bezne),
			("A.IV.", "VH minulých let", p => p.AIV_VysledekHospodareniMinulychLet.Bezne),
			("A.V.", "VH běžného účetního období", p => p.AV_VysledekHospodareniBeznehoObdobi?.Bezne ?? 0),
			("A.VI.", "Zálohy na výplatu podílu", p => p.AVI_ZalohovaVyplataPodilu?.Bezne ?? 0),
			("B.", "Rezervy", p => p.B_Rezervy.Bezne),
			("B.+C.", "Cizí zdroje", p => p.BC_CiziZdroje.Bezne),
			("C.", "Závazky", p => p.C_Zavazky.Bezne),
			("C.I.", "Dlouhodobé závazky", p => p.CI_DlouhodobeZavazky.Bezne),
			("C.II.", "Krátkodobé závazky", p => p.CII_KratkodobeZavazky.Bezne),
			("D.", "Časové rozlišení pasiv", p => p.D_CasoveRozliseniPasiv.Bezne),
		};

		foreach (var (oz, nazev, getValue) in rows)
		{
			var line = $"{oz,-10} {nazev,-36}";
			foreach (var r in sorted) line += $" {getValue(r.UcetniZaverka.Pasiva).ToString("N0").PadLeft(w)}";
			Console.WriteLine(line);
		}
	}

	private static void RenderVzzSummary(List<ExtractionResult> sorted, string[] years, int w)
	{
		Console.WriteLine("\n--- VÝKAZ ZISKU A ZTRÁTY (Běžné) ---");
		var header = $"{"Označení",-10} {"Položka",-36}";
		foreach (var y in years) header += $" {y.PadLeft(w)}";
		Console.WriteLine(header);
		Console.WriteLine(new string('─', header.Length));

		var rows = new (string oz, string nazev, Func<VykazZiskuAZtraty, decimal> getValue)[]
		{
			("I.", "Tržby z prodeje výrobků a služeb", v => v.I_TrzbyZProdejeVyrobkuASluzeb.Bezne),
			("II.", "Tržby za prodej zboží", v => v.II_TrzbyZaProdejZbozi.Bezne),
			("A.", "Výkonová spotřeba", v => v.A_VykonovaSpotřeba.Bezne),
			("  A.1.", "Náklady na prodané zboží", v => v.A1_NakladyVynalozeneNaProdaneZbozi.Bezne),
			("  A.2.", "Spotřeba materiálu a energie", v => v.A2_SpotrebaMaterialuAEnergie.Bezne),
			("  A.3.", "Služby", v => v.A3_Sluzby.Bezne),
			("D.", "Osobní náklady", v => v.D_OsobniNaklady.Bezne),
			("E.", "Úpravy hodnot v provozní oblasti", v => v.E_UpravyHodnotVProvozniOblasti.Bezne),
			("*", "Provozní VH", v => v.ProvozniVysledekHospodareni.Bezne),
			("*", "Finanční VH", v => v.FinancniVysledekHospodareni.Bezne),
			("**", "VH před zdaněním", v => v.VysledekHospodareniPredZdanenim.Bezne),
			("L.", "Daň z příjmů", v => v.L_DanZPrijmu.Bezne),
			("***", "VH za účetní období", v => v.VysledekHospodareniZaObdobi.Bezne),
			("*", "Čistý obrat", v => v.CistyObrat.Bezne),
		};

		foreach (var (oz, nazev, getValue) in rows)
		{
			var line = $"{oz,-10} {nazev,-36}";
			foreach (var r in sorted) line += $" {getValue(r.UcetniZaverka.VykazZiskuAZtraty).ToString("N0").PadLeft(w)}";
			Console.WriteLine(line);
		}
	}

	private static void RenderValidationSummary(List<ExtractionResult> sorted, string[] years, int w)
	{
		Console.WriteLine("\n--- VALIDACE ---");
		var header = $"{"",46}";
		foreach (var y in years) header += $" {y.PadLeft(w)}";
		Console.WriteLine(header);

		var line1 = $"{"Rozvaha v rovnováze",-46}";
		foreach (var r in sorted)
		{
			var color = r.JeRozvahaVRovnovaze ? Green : Red;
			line1 += $" {color}{Fmt(r.JeRozvahaVRovnovaze).PadLeft(w)}{Reset}";
		}
		Console.WriteLine(line1);

		var line2 = $"{"VH konzistentní",-46}";
		foreach (var r in sorted)
		{
			var color = r.JeVHKonzistentni ? Green : Red;
			line2 += $" {color}{Fmt(r.JeVHKonzistentni).PadLeft(w)}{Reset}";
		}
		Console.WriteLine(line2);
	}

	private static void RenderMetricsSummary(List<ExtractionResult> sorted, string[] years, int w)
	{
		Console.WriteLine("\n--- API METRIKY ---");
		var header = $"{"",46}";
		foreach (var y in years) header += $" {y.PadLeft(w)}";
		Console.WriteLine(header);

		void MetricRow(string label, Func<ExtractionMetrics, string> fmt)
		{
			var line = $"{label,-46}";
			foreach (var r in sorted)
			{
				var val = r.Metrics is not null ? fmt(r.Metrics) : "?";
				line += $" {val.PadLeft(w)}";
			}
			Console.WriteLine(line);
		}

		MetricRow("Celkový čas (s)", m => m.TotalDuration.TotalSeconds.ToString("F1"));
		MetricRow("Doc Intelligence (s)", m => m.MarkdownFromCache ? "cache" : m.DocumentIntelligenceDuration.TotalSeconds.ToString("F1"));
		MetricRow("GPT-5.1 volání", m => m.GptCalls.ToString());
		MetricRow("GPT-5.1 čas (s)", m => m.GptTotalDuration.TotalSeconds.ToString("F1"));
		MetricRow("GPT-5.1 vstupní tokeny", m => m.GptInputTokens.ToString("N0"));
		MetricRow("GPT-5.1 výstupní tokeny", m => m.GptOutputTokens.ToString("N0"));
		MetricRow("GPT-5.1 tokeny celkem", m => m.GptTotalTokens.ToString("N0"));
		MetricRow("Opakování validace", m => m.BalanceRetries.ToString());

		// Totals row
		var allMetrics = sorted.Select(r => r.Metrics).Where(m => m is not null).ToList();
		if (allMetrics.Count > 0)
		{
			Console.WriteLine(new string('─', 46 + sorted.Count * (w + 1)));
			Console.WriteLine($"{"CELKEM GPT-5.1 volání",-46}  {allMetrics.Sum(m => m!.GptCalls)}");
			Console.WriteLine($"{"CELKEM GPT-5.1 tokeny",-46}  {allMetrics.Sum(m => m!.GptTotalTokens):N0} ({allMetrics.Sum(m => m!.GptInputTokens):N0} vstup + {allMetrics.Sum(m => m!.GptOutputTokens):N0} výstup)");
		}
	}

	// ============================================================
	// Helpers
	// ============================================================

	private static void PA(string oz, string nazev, RozvahaAktivaRadek? r, bool highlight = false)
	{
		r ??= new();
		// Auto-detect OCR errors: if Brutto+Korekce ≠ Netto on a non-zero row
		var hasOcrIssue = !highlight
			&& (r.Brutto != 0 || r.Korekce != 0 || r.Netto != 0)
			&& r.Brutto + r.Korekce != r.Netto;
		var line = $"{oz,-14} {nazev,-36} {r.Brutto,12:N0} {r.Korekce,12:N0} {r.Netto,12:N0} {r.MinuleNetto,12:N0}";
		if (hasOcrIssue) Console.WriteLine($"{Yellow}{line}  ← B+K≠N (diff={r.Netto - r.Brutto - r.Korekce:N0}){Reset}");
		else if (highlight) Console.WriteLine($"{RedBg}{line}{Reset}");
		else Console.WriteLine(line);
	}

	private static void PP(string oz, string nazev, RozvahaPasivaRadek? r, bool highlight = false)
	{
		r ??= new();
		var line = $"{oz,-14} {nazev,-36} {r.Bezne,12:N0} {r.Minule,12:N0}";
		if (highlight) Console.WriteLine($"{RedBg}{line}{Reset}");
		else Console.WriteLine(line);
	}

	private static void PV(string oz, string nazev, VykazRadek? r, bool highlight = false)
	{
		r ??= new();
		var line = $"{oz,-14} {nazev,-36} {r.Bezne,12:N0} {r.Minule,12:N0}";
		if (highlight) Console.WriteLine($"{RedBg}{line}{Reset}");
		else Console.WriteLine(line);
	}

	private static string Fmt(bool v) => v ? $"{Green}ANO ✓{Reset}" : $"{Red}NE ✗{Reset}";
}
