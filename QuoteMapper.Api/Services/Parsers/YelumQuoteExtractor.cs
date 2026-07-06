using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using QuoteMapper.Api.Models;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Services.Parsers;

public static class YelumQuoteExtractor
{
    private static readonly Regex MoneyRegex = new(@"\d{1,3}(?:\.\d{3})*,\d{2}", RegexOptions.Compiled);

    public static void Fill(IFormFile file, QuoteTemplateExtractionResult extracted)
    {
        var text = ExtractText(file);
        if (string.IsNullOrWhiteSpace(text))
            return;

        FillVehicleData(text, extracted);
        FillCoverageData(text, extracted);
        FillDeductibles(text, extracted);
        FillDriverData(text, extracted);
        FillPaymentRows(text, extracted);
    }

    private static string ExtractText(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        return string.Concat(document.GetPages().Select(page => page.Text ?? string.Empty));
    }

    private static void FillVehicleData(string text, QuoteTemplateExtractionResult extracted)
    {
        var vehicleMatch = Regex.Match(
            text,
            @"CódigoFIPEMarca/TipodoVeículoAnoFabricação/ModeloChassiPlaca(?<fipe>\d{6,7}-\d)(?<vehicle>.*?)(?<ano1>\d{4})/(?<ano2>\d{4})(?<chassi>[A-Z0-9]{10,})(?<placa>[A-Z0-9]{7,8})",
            RegexOptions.IgnoreCase);

        if (vehicleMatch.Success)
        {
            extracted.FipeCode = vehicleMatch.Groups["fipe"].Value.Trim();
            extracted.Vehicle = vehicleMatch.Groups["vehicle"].Value.Trim();
            extracted.AnoModelo = vehicleMatch.Groups["ano2"].Value.Trim();
            extracted.Plate = vehicleMatch.Groups["placa"].Value.Trim();

            var fuelMatch = Regex.Match(extracted.Vehicle, @"\(([^)]+)\)");
            if (fuelMatch.Success)
                extracted.Combustivel = fuelMatch.Groups[1].Value.Trim();
        }

        var usageMatch = Regex.Match(
            text,
            @"CEPdePernoite(?<cep>\d{5,8})(?<tipoFranquia>.*?)UtilizaçãoAntifurtoIsençãoFiscal(?<uso>.*?)(?:Não|Sim)DADOSDOSEGURO",
            RegexOptions.IgnoreCase);

        if (usageMatch.Success)
        {
            extracted.CepPernoite = usageMatch.Groups["cep"].Value.Trim();
            extracted.TipoFranquiaVeiculo = NormalizeFranchiseType(usageMatch.Groups["tipoFranquia"].Value);
            extracted.UsoVeiculo = usageMatch.Groups["uso"].Value.Trim();
        }
    }

    private static void FillCoverageData(string text, QuoteTemplateExtractionResult extracted)
    {
        extracted.FranquiaVeiculo = FormatMoney(ExtractLastMoney(
            text,
            @"BASICA-01-COMPREENSIVA.*?(?<values>(?:\d{1,3}(?:\.\d{3})*,\d{2}){2})"));

        extracted.DanosMateriais = FormatMoney(ExtractFirstMoneyAfterLabel(
            text,
            "RESPCIVILFACULTATIVAVEÍCULOS-DANOSMATERIAIS"));

        extracted.DanosCorporais = FormatMoney(ExtractFirstMoneyAfterLabel(
            text,
            "RESPCIVILFACULTATIVAVEÍCULOS-DANOSCORPORAIS"));

        extracted.DanosMorais = FormatMoney(ExtractFirstMoneyAfterLabel(
            text,
            "RESPCIVILFACULTATIVAVEÍCULOS-DANOSMORAISEESTÉTICOS"));

        extracted.AppMorte = FormatMoney(ExtractFirstMoneyAfterLabel(
            text,
            "ACIDENTESPESSOAISPASSAGEIROS-LMIPORPASSAGEIRO-MORTE"));

        extracted.AppInvalidez = FormatMoney(ExtractFirstMoneyAfterLabel(
            text,
            "ACIDENTESPESSOAISPASSAGEIROS-LMIPORPASSAGEIRO-INVALIDEZPERMANENTE"));

        var assistanceMatch = Regex.Match(
            text,
            @"ASSISTENCIA-(?<tipo>[A-Z]+)VerCond\.Gerais",
            RegexOptions.IgnoreCase);

        if (assistanceMatch.Success)
            extracted.AssistenciaGuincho = assistanceMatch.Groups["tipo"].Value.Trim();
    }

    private static void FillDeductibles(string text, QuoteTemplateExtractionResult extracted)
    {
        var info = ExtractBetween(
            text,
            "INFORMAÇÕESCOMPLEMENTARES",
            "DADOSDOPERFIL");

        if (string.IsNullOrWhiteSpace(info))
            return;

        extracted.FranquiaPequenosReparos = FormatMoney(ExtractFirstMoneyAfterLabel(
            info,
            "PROTECAOPEQUENOSREPAROS-FranquiaR$"));

        extracted.FranquiaParabrisa = FormatMoney(ExtractFirstMoneyAfterLabel(info, "Para-brisaR$"));
        extracted.FranquiaVidroLateral = FormatMoney(ExtractFirstMoneyAfterLabel(info, "LateraisR$"));
        extracted.FranquiaRetrovisor = FormatMoney(ExtractFirstMoneyAfterLabel(info, "RetrovisoresR$"));
        extracted.FranquiaFarolConvencional = FormatMoney(ExtractFirstMoneyAfterLabel(info, "FaroisR$"));
        extracted.FranquiaLanternaConvencional = FormatMoney(ExtractFirstMoneyAfterLabel(info, "LanternasR$"));
        extracted.FranquiaLanternaAuxiliar = FormatMoney(ExtractFirstMoneyAfterLabel(info, "FarolAuxiliarR$"));
        extracted.FranquiaFarolXenonLed = FormatMoney(ExtractFirstMoneyAfterLabel(info, "FaroisdeLEDouXenonR$"));
        extracted.FranquiaLanternaLed = FormatMoney(ExtractFirstMoneyAfterLabel(info, "LanternasLEDR$"));
        extracted.FranquiaPneuRoda = string.Empty;
    }

    private static void FillDriverData(string text, QuoteTemplateExtractionResult extracted)
    {
        var proponente = extracted.Proponente;
        if (string.IsNullOrWhiteSpace(proponente))
        {
            var proposerMatch = Regex.Match(
                text,
                @"NomedoSegurado\(a\)CPF/CNPJ(?<name>.*?)(?:\d{2,3}\.\d{3}\.\d{3}/\d{4}-\d{2})",
                RegexOptions.IgnoreCase);

            if (proposerMatch.Success)
                proponente = proposerMatch.Groups["name"].Value.Trim();
        }

        var driverMatch = Regex.Match(
            text,
            @"DADOSDOPERFILNomedoPrincipalCondutorEstadoCivilDatadeNascimentoIdadeSexo(?<driver>.*?)CPFCondutor",
            RegexOptions.IgnoreCase);

        if (driverMatch.Success)
        {
            var driver = driverMatch.Groups["driver"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(driver) && driver != "0")
                extracted.CondutorPrincipal = driver;
        }

        if (string.IsNullOrWhiteSpace(extracted.CondutorPrincipal) || extracted.CondutorPrincipal == "0")
        {
            if (text.Contains("Própriosegurado", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Propriosegurado", StringComparison.OrdinalIgnoreCase))
            {
                extracted.CondutorPrincipal = proponente ?? string.Empty;
            }
        }

        if (extracted.Condutores18a25.Contains("Deseja estender cobertura", StringComparison.OrdinalIgnoreCase))
            extracted.Condutores18a25 = string.Empty;
    }

    private static void FillPaymentRows(string text, QuoteTemplateExtractionResult extracted)
    {
        var block = ExtractBetween(text, "Àvista", "ITEM1-DADOSDOVEICULOSEGURADO");
        if (string.IsNullOrWhiteSpace(block))
            return;

        var labels = new[]
        {
            "Àvista", "1+1", "1+2", "1+3", "1+4", "1+5",
            "1+6", "1+7", "1+8", "1+9", "1+10", "1+11"
        };

        var rows = new List<PaymentRowData>();

        for (var i = 0; i < labels.Length; i++)
        {
            var start = block.IndexOf(labels[i], StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                continue;

            start += labels[i].Length;

            var end = i + 1 < labels.Length
                ? block.IndexOf(labels[i + 1], start, StringComparison.OrdinalIgnoreCase)
                : block.Length;

            if (end < 0)
                end = block.Length;

            var segment = block[start..end];
            var values = MoneyRegex.Matches(segment)
                .Select(match => match.Value)
                .ToList();

            if (values.Count == 0)
                continue;

            rows.Add(new PaymentRowData
            {
                Parcela = (i + 1).ToString("00"),
                Carne = FormatMoney(values.ElementAtOrDefault(0)),
                DebitoConta = FormatMoney(values.ElementAtOrDefault(1)),
                CartaoCredito = FormatMoney(values.ElementAtOrDefault(2)),
                CarneSemJuros = !string.IsNullOrWhiteSpace(values.ElementAtOrDefault(0)),
                DebitoContaSemJuros = !string.IsNullOrWhiteSpace(values.ElementAtOrDefault(1)),
                CartaoCreditoSemJuros = !string.IsNullOrWhiteSpace(values.ElementAtOrDefault(2))
            });
        }

        if (rows.Count > 0)
            extracted.PaymentRows = rows;
    }

    private static string ExtractBetween(string text, string startLabel, string endLabel)
    {
        var start = text.IndexOf(startLabel, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return string.Empty;

        var end = text.IndexOf(endLabel, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
            end = text.Length;

        return text[start..end];
    }

    private static string ExtractFirstMoneyAfterLabel(string text, string label)
    {
        var index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return string.Empty;

        var segment = text[index..];
        var match = MoneyRegex.Match(segment);
        return match.Success ? match.Value : string.Empty;
    }

    private static string ExtractLastMoney(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!match.Success)
            return string.Empty;

        var values = MoneyRegex.Matches(match.Value)
            .Select(item => item.Value)
            .ToList();

        return values.LastOrDefault() ?? string.Empty;
    }

    private static string NormalizeFranchiseType(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        normalized = normalized.Replace("0.5", "0,5", StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace("-", " - ", StringComparison.OrdinalIgnoreCase);
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    private static string FormatMoney(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return normalized.StartsWith("R$", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"R$ {normalized}";
    }
}
