using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using QuoteMapper.Api.Models;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Services.Parsers;

public static class BanestesQuoteExtractor
{
    private static readonly Regex MoneyRegex = new(@"\d{1,3}(?:\.\d{3})*,\d{2}", RegexOptions.Compiled);

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
        var vehicleMatch = Regex.Match(
            text,
            @"Veículo(?<fipe>\d{6}-\d)(?<vehicle>.*?)(?:Fabricação/Modelo)(?<ano1>\d{4})\s*/\s*(?<ano2>\d{4})",
            RegexOptions.IgnoreCase);

        if (vehicleMatch.Success)
        {
            extracted.FipeCode = vehicleMatch.Groups["fipe"].Value.Trim();
            extracted.Vehicle = vehicleMatch.Groups["vehicle"].Value.Trim();
            extracted.AnoModelo = vehicleMatch.Groups["ano2"].Value.Trim();

            var fuelMatch = Regex.Match(extracted.Vehicle, @"\b(FLEX|GASOLINA|DIESEL|ALCOOL)\b", RegexOptions.IgnoreCase);
            if (fuelMatch.Success)
                extracted.Combustivel = fuelMatch.Groups[1].Value.Trim();
        }
    }

    private static void FillCoverageData(string text, QuoteTemplateExtractionResult extracted)
    {
        extracted.DanosCorporais = FormatMoney(ExtractCoverageLmi(text, "Cobertura RCFV/Danos Corporais"));
        extracted.DanosMateriais = FormatMoney(ExtractCoverageLmi(text, "Cobertura RCFV/Danos Materiais"));
        extracted.DanosMorais = FormatMoney(ExtractCoverageLmi(text, "Cobertura RCFV/Danos Morais"));
        extracted.AppMorte = FormatMoney(ExtractCoverageLmi(text, "Cobertura APP/ Morte Acidental por Passageiro"));
        extracted.AppInvalidez = FormatMoney(ExtractCoverageLmi(text, "Cobertura APP/ Invalidez Permanente Total ou Parcial por Acidente por Passageiro"));

        var cascoMatch = Regex.Match(
            text,
            @"Cobertura Casco Compreensiva100% Tabela Fipe\.(?<premio>\d{1,3}(?:\.\d{3})*,\d{2})(?<tipo>Dedutível.*?)(?<franquia>\d{1,3}(?:\.\d{3})*,\d{2})",
            RegexOptions.IgnoreCase);

        if (cascoMatch.Success)
        {
            extracted.TipoFranquiaVeiculo = NormalizeFranchiseType(cascoMatch.Groups["tipo"].Value);
            extracted.FranquiaVeiculo = FormatMoney(cascoMatch.Groups["franquia"].Value);
        }

        var guinchoMatch = Regex.Match(
            text,
            @"Guincho:(?<guincho>.*?)(?:Carro Reserva:|SERVIÇO ASSISTÊNCIA À VIDROS)",
            RegexOptions.IgnoreCase);

        if (guinchoMatch.Success)
            extracted.AssistenciaGuincho = NormalizeSpacing(guinchoMatch.Groups["guincho"].Value);

        var carroReservaMatch = Regex.Match(
            text,
            @"Carro Reserva:(?<tipo>[A-ZÇÃÕÁÉÍÓÚ ]*?)\s*(?<dias>\d+\s*DIAS)",
            RegexOptions.IgnoreCase);

        if (carroReservaMatch.Success)
        {
            extracted.TipoCarroReserva = NormalizeSpacing(carroReservaMatch.Groups["tipo"].Value);
            extracted.CarroReserva = NormalizeSpacing(carroReservaMatch.Groups["dias"].Value);
        }
    }

    private static void FillFranchiseData(string text, QuoteTemplateExtractionResult extracted)
    {
        extracted.FranquiaParabrisa = FormatMoney(ExtractBracketValue(text, "Para-brisa Dianteiro"));
        extracted.FranquiaVidroLateral = FormatMoney(ExtractBracketValue(text, "Vidros Laterais"));
        extracted.FranquiaFarolConvencional = FormatMoney(ExtractBracketValue(text, "Farol Convencional"));
        extracted.FranquiaLanternaConvencional = FormatMoney(ExtractBracketValue(text, "Lanterna Convencional"));
        extracted.FranquiaFarolXenonLed = FormatMoney(ExtractBracketValue(text, "Farol Xenon/LED"));
        extracted.FranquiaLanternaLed = FormatMoney(ExtractBracketValue(text, "Lanterna LED"));
        extracted.FranquiaRetrovisor = FormatMoney(ExtractBracketValue(text, "Retrovisor Convencional"));
        extracted.FranquiaLanternaAuxiliar = FormatMoney(ExtractBracketValue(text, "Lanterna Auxiliar"));
    }

    private static void FillPaymentRows(string text, QuoteTemplateExtractionResult extracted)
    {
        var start = text.IndexOf("Parcelamento (R$)", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return;

        var paymentText = text[start..];
        var rowMatches = Regex.Matches(paymentText, @"(1(?:\s*\+\s*\d)?)\s*(\d{1,3}(?:\.\d{3})*,\d{2})\s*2\.618,98", RegexOptions.IgnoreCase);
        if (rowMatches.Count == 0)
            return;

        var uniqueRows = new Dictionary<string, string>();

        foreach (Match match in rowMatches)
        {
            var rawInstallment = NormalizeSpacing(match.Groups[1].Value).Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
            var parcel = ConvertInstallment(rawInstallment);
            if (string.IsNullOrWhiteSpace(parcel) || uniqueRows.ContainsKey(parcel))
                continue;

            uniqueRows[parcel] = match.Groups[2].Value;
        }

        extracted.PaymentRows = uniqueRows
            .OrderBy(item => item.Key)
            .Select(item => new PaymentRowData
            {
                Parcela = item.Key,
                Carne = FormatMoney(item.Value),
                CartaoCredito = FormatMoney(item.Value),
                DebitoConta = FormatMoney(item.Value),
                CarneSemJuros = true,
                CartaoCreditoSemJuros = true,
                DebitoContaSemJuros = true
            })
            .ToList();
    }

    private static string ExtractCoverageLmi(string text, string label)
    {
        var match = Regex.Match(
            text,
            Regex.Escape(label) + @"(?<lmi>\d{1,3}(?:\.\d{3})*,\d{2})",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["lmi"].Value : string.Empty;
    }

    private static string ExtractBracketValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            @"\[" + Regex.Escape(label) + @"\s*=\s*R\$(?<value>\d{1,3}(?:\.\d{3})*,\d{2})\]",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["value"].Value : string.Empty;
    }

    private static string ConvertInstallment(string rawInstallment)
    {
        return rawInstallment switch
        {
            "1" => "01",
            "1+1" => "02",
            "1+2" => "03",
            "1+3" => "04",
            "1+4" => "05",
            "1+5" => "06",
            "1+6" => "07",
            "1+7" => "08",
            "1+8" => "09",
            "1+9" => "10",
            _ => string.Empty
        };
    }

    private static string NormalizeFranchiseType(string value)
    {
        var normalized = NormalizeSpacing(value);
        normalized = normalized.Replace("Dedutível", "Dedutivel", StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace("50%", "50%", StringComparison.OrdinalIgnoreCase);
        return normalized;
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
