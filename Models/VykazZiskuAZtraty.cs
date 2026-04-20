namespace AiStructuredDataFromImageExtractionDemo.Models;

/// <summary>
/// Výkaz zisku a ztráty - druhové členění (Income Statement - nature-based classification)
/// according to Vyhláška č. 500/2002 Sb., Příloha č. 2.
/// Values in thousands CZK. Computed subtotals marked with * in the regulation.
/// </summary>
public record VykazZiskuAZtraty
{
    // ==============================
    // Provozní oblast
    // ==============================

    /// <summary>I. Tržby z prodeje výrobků a služeb</summary>
    public VykazRadek I_TrzbyZProdejeVyrobkuASluzeb { get; init; } = new();

    /// <summary>II. Tržby za prodej zboží</summary>
    public VykazRadek II_TrzbyZaProdejZbozi { get; init; } = new();

    // A. Výkonová spotřeba
    /// <summary>A.1. Náklady vynaložené na prodané zboží</summary>
    public VykazRadek A1_NakladyVynalozeneNaProdaneZbozi { get; init; } = new();
    /// <summary>A.2. Spotřeba materiálu a energie</summary>
    public VykazRadek A2_SpotrebaMaterialuAEnergie { get; init; } = new();
    /// <summary>A.3. Služby</summary>
    public VykazRadek A3_Sluzby { get; init; } = new();

    /// <summary>A. Výkonová spotřeba = A.1 + A.2 + A.3</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek A_VykonovaSpotřeba => SumRadky(
        A1_NakladyVynalozeneNaProdaneZbozi, A2_SpotrebaMaterialuAEnergie, A3_Sluzby);

    /// <summary>B. Změna stavu zásob vlastní činnosti (+/-)</summary>
    public VykazRadek B_ZmenaStavuZasobVlastniCinnosti { get; init; } = new();

    /// <summary>C. Aktivace (-)</summary>
    public VykazRadek C_Aktivace { get; init; } = new();

    // D. Osobní náklady
    /// <summary>D.1. Mzdové náklady</summary>
    public VykazRadek D1_MzdoveNaklady { get; init; } = new();
    /// <summary>D.2. Náklady na sociální zabezpečení, zdravotní pojištění a ostatní náklady</summary>
    public VykazRadek D2_NakladyNaSocialniZabezpeceni { get; init; } = new();
    /// <summary>D.2.1. Náklady na sociální zabezpečení a zdravotní pojištění</summary>
    public VykazRadek D2_1_SocialniZabezpeceniAZdravotniPojisteni { get; init; } = new();
    /// <summary>D.2.2. Ostatní náklady</summary>
    public VykazRadek D2_2_OstatniNaklady { get; init; } = new();

    /// <summary>D. Osobní náklady = D.1 + D.2</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek D_OsobniNaklady => SumRadky(D1_MzdoveNaklady, D2_NakladyNaSocialniZabezpeceni);

    // E. Úpravy hodnot v provozní oblasti
    /// <summary>E.1. Úpravy hodnot dlouhodobého nehmotného a hmotného majetku</summary>
    public VykazRadek E1_UpravyHodnotDNMaDHM { get; init; } = new();
    /// <summary>E.1.1. Úpravy hodnot DNM a DHM - trvalé</summary>
    public VykazRadek E1_1_Trvale { get; init; } = new();
    /// <summary>E.1.2. Úpravy hodnot DNM a DHM - dočasné</summary>
    public VykazRadek E1_2_Docasne { get; init; } = new();
    /// <summary>E.2. Úpravy hodnot zásob</summary>
    public VykazRadek E2_UpravyHodnotZasob { get; init; } = new();
    /// <summary>E.3. Úpravy hodnot pohledávek</summary>
    public VykazRadek E3_UpravyHodnotPohledavek { get; init; } = new();

    /// <summary>E. Úpravy hodnot v provozní oblasti = E.1 + E.2 + E.3</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek E_UpravyHodnotVProvozniOblasti => SumRadky(
        E1_UpravyHodnotDNMaDHM, E2_UpravyHodnotZasob, E3_UpravyHodnotPohledavek);

    // III. Ostatní provozní výnosy
    /// <summary>III.1. Tržby z prodaného dlouhodobého majetku</summary>
    public VykazRadek III1_TrzbyZProdanehoDM { get; init; } = new();
    /// <summary>III.2. Tržby z prodaného materiálu</summary>
    public VykazRadek III2_TrzbyZProdanehoMaterialu { get; init; } = new();
    /// <summary>III.3. Jiné provozní výnosy</summary>
    public VykazRadek III3_JineProvozniVynosy { get; init; } = new();

    /// <summary>III. Ostatní provozní výnosy = III.1 + III.2 + III.3</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek III_OstatniProvozniVynosy => SumRadky(
        III1_TrzbyZProdanehoDM, III2_TrzbyZProdanehoMaterialu, III3_JineProvozniVynosy);

    // F. Ostatní provozní náklady
    /// <summary>F.1. Zůstatková cena prodaného dlouhodobého majetku</summary>
    public VykazRadek F1_ZustatkovaCenaProdanehoDM { get; init; } = new();
    /// <summary>F.2. Prodaný materiál</summary>
    public VykazRadek F2_ProdanyMaterial { get; init; } = new();
    /// <summary>F.3. Daně a poplatky</summary>
    public VykazRadek F3_DaneAPoplatky { get; init; } = new();
    /// <summary>F.4. Rezervy v provozní oblasti a komplexní náklady příštích období</summary>
    public VykazRadek F4_RezerveVProvozniOblasti { get; init; } = new();
    /// <summary>F.5. Jiné provozní náklady</summary>
    public VykazRadek F5_JineProvozniNaklady { get; init; } = new();

    /// <summary>F. Ostatní provozní náklady = F.1 + F.2 + F.3 + F.4 + F.5</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek F_OstatniProvozniNaklady => SumRadky(
        F1_ZustatkovaCenaProdanehoDM, F2_ProdanyMaterial, F3_DaneAPoplatky,
        F4_RezerveVProvozniOblasti, F5_JineProvozniNaklady);

    /// <summary>* Provozní výsledek hospodaření (extracted directly from the asterisk row in VZZ table)</summary>
    public VykazRadek ProvozniVysledekHospodareni { get; init; } = new();

    /// <summary>Provozní VH computed from sub-items for validation: I + II - A + B - C - D - E + III - F</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek ProvozniVHVypoctem => new()
    {
        Bezne = I_TrzbyZProdejeVyrobkuASluzeb.Bezne + II_TrzbyZaProdejZbozi.Bezne
            - A_VykonovaSpotřeba.Bezne + B_ZmenaStavuZasobVlastniCinnosti.Bezne
            - C_Aktivace.Bezne - D_OsobniNaklady.Bezne - E_UpravyHodnotVProvozniOblasti.Bezne
            + III_OstatniProvozniVynosy.Bezne - F_OstatniProvozniNaklady.Bezne,
        Minule = I_TrzbyZProdejeVyrobkuASluzeb.Minule + II_TrzbyZaProdejZbozi.Minule
            - A_VykonovaSpotřeba.Minule + B_ZmenaStavuZasobVlastniCinnosti.Minule
            - C_Aktivace.Minule - D_OsobniNaklady.Minule - E_UpravyHodnotVProvozniOblasti.Minule
            + III_OstatniProvozniVynosy.Minule - F_OstatniProvozniNaklady.Minule
    };

    // ==============================
    // Finanční oblast
    // ==============================

    // IV. Výnosy z dlouhodobého finančního majetku - podíly
    /// <summary>IV.1. Výnosy z podílů - ovládaná nebo ovládající osoba</summary>
    public VykazRadek IV1_VynosyZPodiluOvladana { get; init; } = new();
    /// <summary>IV.2. Ostatní výnosy z podílů</summary>
    public VykazRadek IV2_OstatniVynosyZPodilu { get; init; } = new();

    /// <summary>IV. Výnosy z dlouhodobého finančního majetku - podíly</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek IV_VynosyZDFMPodily => SumRadky(IV1_VynosyZPodiluOvladana, IV2_OstatniVynosyZPodilu);

    /// <summary>G. Náklady vynaložené na prodané podíly</summary>
    public VykazRadek G_NakladyNaProdanePodily { get; init; } = new();

    // V. Výnosy z ostatního dlouhodobého finančního majetku
    /// <summary>V.1. Výnosy z ostatního DFM - ovládaná nebo ovládající osoba</summary>
    public VykazRadek V1_VynosyZOstatnihoOvladana { get; init; } = new();
    /// <summary>V.2. Ostatní výnosy z ostatního DFM</summary>
    public VykazRadek V2_OstatniVynosyZOstatniho { get; init; } = new();

    /// <summary>V. Výnosy z ostatního DFM</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek V_VynosyZOstatnihoFinancnihoMajetku => SumRadky(
        V1_VynosyZOstatnihoOvladana, V2_OstatniVynosyZOstatniho);

    /// <summary>H. Náklady související s ostatním DFM</summary>
    public VykazRadek H_NakladySouvisejiciSOstatnimDFM { get; init; } = new();

    // VI. Výnosové úroky a podobné výnosy
    /// <summary>VI.1. Výnosové úroky - ovládaná nebo ovládající osoba</summary>
    public VykazRadek VI1_VynosoveUrokyOvladana { get; init; } = new();
    /// <summary>VI.2. Ostatní výnosové úroky a podobné výnosy</summary>
    public VykazRadek VI2_OstatniVynosoveUroky { get; init; } = new();

    /// <summary>VI. Výnosové úroky a podobné výnosy</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek VI_VynosoveUroky => SumRadky(VI1_VynosoveUrokyOvladana, VI2_OstatniVynosoveUroky);

    /// <summary>I. Úpravy hodnot a rezervy ve finanční oblasti</summary>
    public VykazRadek I_UpravyHodnotVeFinancniOblasti { get; init; } = new();

    // J. Nákladové úroky a podobné náklady
    /// <summary>J.1. Nákladové úroky - ovládaná nebo ovládající osoba</summary>
    public VykazRadek J1_NakladoveUrokyOvladana { get; init; } = new();
    /// <summary>J.2. Ostatní nákladové úroky a podobné náklady</summary>
    public VykazRadek J2_OstatniNakladoveUroky { get; init; } = new();

    /// <summary>J. Nákladové úroky a podobné náklady</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek J_NakladoveUroky => SumRadky(J1_NakladoveUrokyOvladana, J2_OstatniNakladoveUroky);

    /// <summary>VII. Ostatní finanční výnosy</summary>
    public VykazRadek VII_OstatniFinancniVynosy { get; init; } = new();

    /// <summary>K. Ostatní finanční náklady</summary>
    public VykazRadek K_OstatniFinancniNaklady { get; init; } = new();

    /// <summary>* Finanční výsledek hospodaření (extracted directly from the asterisk row in VZZ table)</summary>
    public VykazRadek FinancniVysledekHospodareni { get; init; } = new();

    /// <summary>Finanční VH computed from sub-items for validation: IV - G + V - H + VI - I - J + VII - K</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek FinancniVHVypoctem => new()
    {
        Bezne = IV_VynosyZDFMPodily.Bezne - G_NakladyNaProdanePodily.Bezne
            + V_VynosyZOstatnihoFinancnihoMajetku.Bezne - H_NakladySouvisejiciSOstatnimDFM.Bezne
            + VI_VynosoveUroky.Bezne - I_UpravyHodnotVeFinancniOblasti.Bezne
            - J_NakladoveUroky.Bezne + VII_OstatniFinancniVynosy.Bezne
            - K_OstatniFinancniNaklady.Bezne,
        Minule = IV_VynosyZDFMPodily.Minule - G_NakladyNaProdanePodily.Minule
            + V_VynosyZOstatnihoFinancnihoMajetku.Minule - H_NakladySouvisejiciSOstatnimDFM.Minule
            + VI_VynosoveUroky.Minule - I_UpravyHodnotVeFinancniOblasti.Minule
            - J_NakladoveUroky.Minule + VII_OstatniFinancniVynosy.Minule
            - K_OstatniFinancniNaklady.Minule
    };

    // ==============================
    // Výsledek hospodaření
    // ==============================

    /// <summary>** Výsledek hospodaření před zdaněním (extracted directly from the VZZ table)</summary>
    public VykazRadek VysledekHospodareniPredZdanenim { get; init; } = new();

    // L. Daň z příjmů
    /// <summary>L.1. Daň z příjmů splatná</summary>
    public VykazRadek L1_DanZPrijmuSplatna { get; init; } = new();
    /// <summary>L.2. Daň z příjmů odložená (+/-)</summary>
    public VykazRadek L2_DanZPrijmuOdlozena { get; init; } = new();

    /// <summary>L. Daň z příjmů = L.1 + L.2</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public VykazRadek L_DanZPrijmu => SumRadky(L1_DanZPrijmuSplatna, L2_DanZPrijmuOdlozena);

    /// <summary>** Výsledek hospodaření po zdanění (extracted directly from the VZZ table)</summary>
    public VykazRadek VysledekHospodareniPoZdaneni { get; init; } = new();

    /// <summary>M. Převod podílu na výsledku hospodaření společníkům (+/-)</summary>
    public VykazRadek M_PrevodPodiluNaVH { get; init; } = new();

    /// <summary>*** Výsledek hospodaření za účetní období (extracted directly from the VZZ table)</summary>
    public VykazRadek VysledekHospodareniZaObdobi { get; init; } = new();

    /// <summary>* Čistý obrat za účetní období (extracted directly from the VZZ table)</summary>
    public VykazRadek CistyObrat { get; init; } = new();

    // ==============================
    // Helper
    // ==============================
    private static VykazRadek SumRadky(params VykazRadek?[] radky)
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
/// Single row of the Výkaz zisku a ztráty.
/// Contains current period and prior period values.
/// </summary>
public record VykazRadek
{
    /// <summary>Běžné účetní období (current period, in thousands CZK)</summary>
    public decimal Bezne { get; init; }
    /// <summary>Minulé účetní období (prior period, in thousands CZK)</summary>
    public decimal Minule { get; init; }
}
