using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using QuoteMapper.Api.Models;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Services.Parsers;

public static class AzulQuoteExtractor
{
    private static readonly Regex MoneyRegex = new(@"R\$\s?\d{1,3}(?:\.\d{3})*,\d{2}|\d{1,3}(?:\.\d{3})*,\d{2}", RegexOptions.Compiled);

    public static void Fill(IFormFile file, QuoteTemplateExtractionResult extracted)
    {
        var text = ExtractText(file);
        if (string.IsNullOrWhiteSpace(text))
            return;

        FillMainData(text, extracted);
        FillCoverageData(text, extracted);
        FillFranchiseData(text, extracted);
        FillPaymentRows(text, extracted);
    }

    private static string ExtractText(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        return string.Concat(document.GetPages().Select(page => page.Text ?? string.Empty));
    }

    private static void FillMainData(string text, QuoteTemplateExtractionResult extracted)
    {
        var mainMatch = Regex.Match(
            text,
            @"Fipe(?<fipe>\d+)Combustível(?<fuel>[A-Z/]+)(?<vehicle>\d+\s*-\s*-.*?AUT\.)Veículo(?<plate>[A-Z0-9]{7})[A-Z0-9]+Chassi(?<ano1>\d{4})\s*/\s*(?<ano2>\d{4})Ano Fabricação / Modelo",
            RegexOptions.IgnoreCase);

        if (mainMatch.Success)
        {
            extracted.FipeCode = mainMatch.Groups["fipe"].Value.Trim();
            extracted.Combustivel = mainMatch.Groups["fuel"].Value.Trim();
            extracted.Vehicle = NormalizeSpacing(mainMatch.Groups["vehicle"].Value);
            extracted.Plate = mainMatch.Groups["plate"].Value.Trim();
            extracted.AnoModelo = mainMatch.Groups["ano2"].Value.Trim();
        }

        var usoMatch = Regex.Match(
            text,
            @"CEP de pernoite(?<cep>\d{5}-\d{3})(?<uso>Táxi|Particular|Comercial)",
            RegexOptions.IgnoreCase);

        if (usoMatch.Success)
        {
            extracted.CepPernoite = usoMatch.Groups["cep"].Value.Trim();
            extracted.UsoVeiculo = usoMatch.Groups["uso"].Value.Trim();
        }
    }

    private static void FillCoverageData(string text, QuoteTemplateExtractionResult extracted)
    {
        extracted.DanosCorporais = FormatMoney(ExtractCoverageLmi(text, "RCF-V Danos Corporais"));
        extracted.DanosMateriais = FormatMoney(ExtractCoverageLmi(text, "RCF-V Danos Materiais"));
        extracted.DanosMorais = FormatMoney(ExtractCoverageLmi(text, "Danos Morais e Estéticos"));

        var appValue = FormatMoney(ExtractCoverageLmi(text, "Acidentes Pessoais Passageiros"));
        extracted.AppMorte = appValue;
        extracted.AppInvalidez = appValue;

        var cascoMatch = Regex.Match(
            text,
            @"Compreensiva \(Colisão, Incêndio, Roubo ouFurto\) - Valor de mercado(?<lmi>\d+\.\d+%)R\$\s?(?<premio>\d{1,3}(?:\.\d{3})*,\d{2})R\$\s?(?<franquia>\d{1,3}(?:\.\d{3})*,\d{2})\s*\((?<tipo>[^)]+)\)",
            RegexOptions.IgnoreCase);

        if (cascoMatch.Success)
        {
            extracted.FranquiaVeiculo = FormatMoney(cascoMatch.Groups["franquia"].Value);
            extracted.TipoFranquiaVeiculo = NormalizeSpacing(cascoMatch.Groups["tipo"].Value);
        }

        var assistMatch = Regex.Match(
            text,
            @"Assistência Ilimitada - Referenciada",
            RegexOptions.IgnoreCase);

        if (assistMatch.Success)
            extracted.AssistenciaGuincho = "ILIMITADA";
    }

    private static void FillFranchiseData(string text, QuoteTemplateExtractionResult extracted)
    {
        var vidrosSection = ExtractBetween(text, "Franquias: Vidros", "Danos aos Vidros e Retrovisores e Faróis e Lanternas");
        if (string.IsNullOrWhiteSpace(vidrosSection))
            return;

        extracted.FranquiaParabrisa = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Vidros (Para-Brisa e Traseiro):"));
        extracted.FranquiaVidroLateral = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Vidros Laterais:"));
        extracted.FranquiaFarolConvencional = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Faróis/Lanternas:"));
        extracted.FranquiaLanternaConvencional = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Faróis/Lanternas:"));
        extracted.FranquiaRetrovisor = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Retrovisores:"));
        extracted.FranquiaFarolXenonLed = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Faróis de Xenônio:"));
        extracted.FranquiaLanternaLed = FormatMoney(ExtractValueAfterLabel(vidrosSection, "Lanternas de LED:"));
    }

    private static void FillPaymentRows(string text, QuoteTemplateExtractionResult extracted)
    {
        var section = ExtractBetween(text, "TODAS CARTÃO DE CRÉDITO - DEMAIS BANDEIRAS", "Questionário de avaliação de risco");
        if (string.IsNullOrWhiteSpace(section))
            return;

        var cardRows = ParsePaymentTable(
            section,
            "TODAS CARTÃO DE CRÉDITO - DEMAIS BANDEIRAS",
            "TODAS DÉBITO C. CORRENTE");

        var debitRows = ParsePaymentTable(
            section,
            "TODAS DÉBITO C. CORRENTE",
            "1 BOLETO / DEMAIS CARNÊ");

        var carneRows = ParsePaymentTable(
            section,
            "1 BOLETO / DEMAIS CARNÊ",
            "1 BOLETO / DEMAIS C. CORRENTE");

        var rows = new List<PaymentRowData>();
        for (var installment = 1; installment <= 12; installment++)
        {
            var key = installment.ToString("00");
            if (!cardRows.ContainsKey(key) && !debitRows.ContainsKey(key) && !carneRows.ContainsKey(key))
                continue;

            var card = cardRows.GetValueOrDefault(key);
            var debit = debitRows.GetValueOrDefault(key);
            var carne = carneRows.GetValueOrDefault(key);

            rows.Add(new PaymentRowData
            {
                Parcela = key,
                Carne = carne.Value,
                CarneSemJuros = carne.IsInterestFree,
                CartaoCredito = card.Value,
                CartaoCreditoSemJuros = card.IsInterestFree,
                DebitoConta = debit.Value,
                DebitoContaSemJuros = debit.IsInterestFree
            });
        }

        if (rows.Count > 0)
            extracted.PaymentRows = rows;
    }

    private static Dictionary<string, (string Value, bool IsInterestFree)> ParsePaymentTable(
        string section,
        string startLabel,
        string endLabel)
    {
        var table = ExtractBetween(section, startLabel, endLabel);
        var result = new Dictionary<string, (string Value, bool IsInterestFree)>();

        for (var installment = 1; installment <= 12; installment++)
        {
            var startToken = $"{installment}x";
            var start = table.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                continue;

            start += startToken.Length;

            var end = installment < 12
                ? table.IndexOf($"{installment + 1}x", start, StringComparison.OrdinalIgnoreCase)
                : table.Length;

            if (end < 0)
                end = table.Length;

            var segment = table[start..end];
            var moneyMatch = Regex.Match(segment, @"R\$\s?\d{1,3}(?:\.\d{3})*,\d{2}");

            var value = moneyMatch.Success ? NormalizeMoney(moneyMatch.Value) : string.Empty;
            var hasJuros = segment.Contains("juros(", StringComparison.OrdinalIgnoreCase) ||
                           segment.Contains("juros(R$", StringComparison.OrdinalIgnoreCase) ||
                           segment.Contains("juros", StringComparison.OrdinalIgnoreCase) && !segment.Contains("s/juros", StringComparison.OrdinalIgnoreCase);

            if (segment.TrimStart().StartsWith("-", StringComparison.OrdinalIgnoreCase))
                value = string.Empty;

            result[installment.ToString("00")] = (value, !string.IsNullOrWhiteSpace(value) && !hasJuros);
        }

        return result;
    }

    private static string ExtractCoverageLmi(string text, string label)
    {
        var match = Regex.Match(
            text,
            Regex.Escape(label) + @"R\$\s?(?<lmi>\d{1,3}(?:\.\d{3})*,\d{2})",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["lmi"].Value : string.Empty;
    }

    private static string ExtractValueAfterLabel(string text, string label)
    {
        var match = Regex.Match(
            text,
            Regex.Escape(label) + @"\s*R\$\s?(?<value>\d{1,3}(?:\.\d{3})*,\d{2})",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["value"].Value : string.Empty;
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

    private static string NormalizeMoney(string value)
    {
        var normalized = NormalizeSpacing(value);
        return normalized.StartsWith("R$", StringComparison.OrdinalIgnoreCase)
            ? normalized.Replace("R$ ", "R$ ", StringComparison.OrdinalIgnoreCase)
            : $"R$ {normalized}";
    }

    private static string NormalizeSpacing(string value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
    }

    private static string FormatMoney(string? value)
    {
        var normalized = NormalizeSpacing(value ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        return normalized.StartsWith("R$", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"R$ {normalized}";
    }
}
