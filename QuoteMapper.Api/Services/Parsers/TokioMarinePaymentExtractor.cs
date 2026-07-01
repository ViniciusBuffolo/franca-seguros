using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using QuoteMapper.Api.Models;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Services.Parsers;

public static class TokioMarinePaymentExtractor
{
    private static readonly Regex MoneyRegex = new(
        @"^(\d{1,3}(?:\.\d{3})*,\d{2}|\d{1,3},\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex InterestRegex = new(
        @"^(Sem Juros|\d{1,2},\d{2})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static List<PaymentRowData> Extract(IFormFile file)
    {
        var pages = ExtractPages(file);
        var debitAndFichaPage = pages.FirstOrDefault(page =>
            page.Contains("Débito/Pix Aut.", StringComparison.OrdinalIgnoreCase) &&
            page.Contains("Ficha", StringComparison.OrdinalIgnoreCase));

        var cardPage = pages.FirstOrDefault(page =>
            page.Contains("CartãoParcela", StringComparison.OrdinalIgnoreCase));

        var debitAndFichaBlocks = FindInstallmentBlocks(debitAndFichaPage);
        var cardBlocks = FindInstallmentBlocks(cardPage);

        var debitRows = debitAndFichaBlocks.ElementAtOrDefault(0) ?? new List<TokioMarinePaymentRow>();
        var fichaRows = debitAndFichaBlocks.ElementAtOrDefault(1) ?? new List<TokioMarinePaymentRow>();
        var cardRows = cardBlocks.FirstOrDefault() ?? new List<TokioMarinePaymentRow>();

        if (debitRows.Count == 0 && fichaRows.Count == 0 && cardRows.Count == 0)
            return new List<PaymentRowData>();

        return Enumerable.Range(1, 12)
            .Select(installment => BuildPaymentRow(
                installment,
                fichaRows,
                cardRows,
                debitRows))
            .Where(row =>
                !string.IsNullOrWhiteSpace(row.Carne) ||
                !string.IsNullOrWhiteSpace(row.CartaoCredito) ||
                !string.IsNullOrWhiteSpace(row.DebitoConta))
            .ToList();
    }

    private static List<string> ExtractPages(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var document = PdfDocument.Open(stream);

        return document.GetPages()
            .Select(page => page.Text)
            .ToList();
    }

    private static List<List<TokioMarinePaymentRow>> FindInstallmentBlocks(string? pageText)
    {
        if (string.IsNullOrWhiteSpace(pageText))
            return new List<List<TokioMarinePaymentRow>>();

        var blocks = new List<List<TokioMarinePaymentRow>>();

        for (var index = 0; index < pageText.Length; index++)
        {
            if (pageText[index] != '1')
                continue;

            var position = index;
            var rows = new List<TokioMarinePaymentRow>();

            for (var installment = 1; installment <= 12; installment++)
            {
                if (!TryReadRow(pageText, ref position, installment, out var row))
                    break;

                rows.Add(row);
            }

            if (rows.Count >= 5)
            {
                blocks.Add(rows);
                index = position - 1;
            }
        }

        return blocks;
    }

    private static bool TryReadRow(
        string text,
        ref int position,
        int installment,
        out TokioMarinePaymentRow row)
    {
        row = default!;

        var installmentText = installment.ToString();
        if (!text.AsSpan(position).StartsWith(installmentText, StringComparison.Ordinal))
            return false;

        position += installmentText.Length;

        if (!TryReadRegex(text, ref position, MoneyRegex, out var value))
            return false;

        if (!TryReadRegex(text, ref position, InterestRegex, out var interest))
            return false;

        if (!TryReadRegex(text, ref position, MoneyRegex, out _))
            return false;

        row = new TokioMarinePaymentRow(
            installment,
            value,
            IsInterestFree(interest));

        return true;
    }

    private static bool TryReadRegex(
        string text,
        ref int position,
        Regex regex,
        out string value)
    {
        value = string.Empty;

        var match = regex.Match(text[position..]);
        if (!match.Success)
            return false;

        value = match.Groups[1].Value;
        position += value.Length;

        return true;
    }

    private static PaymentRowData BuildPaymentRow(
        int installment,
        List<TokioMarinePaymentRow> fichaRows,
        List<TokioMarinePaymentRow> cardRows,
        List<TokioMarinePaymentRow> debitRows)
    {
        var ficha = FindByInstallment(fichaRows, installment);
        var card = FindByInstallment(cardRows, installment);
        var debit = FindByInstallment(debitRows, installment);

        return new PaymentRowData
        {
            Parcela = installment.ToString(),
            Carne = ficha?.Value ?? string.Empty,
            CarneSemJuros = ficha?.IsInterestFree ?? false,
            CartaoCredito = card?.Value ?? string.Empty,
            CartaoCreditoSemJuros = card?.IsInterestFree ?? false,
            DebitoConta = debit?.Value ?? string.Empty,
            DebitoContaSemJuros = debit?.IsInterestFree ?? false
        };
    }

    private static TokioMarinePaymentRow? FindByInstallment(
        List<TokioMarinePaymentRow> rows,
        int installment)
    {
        return rows.FirstOrDefault(row => row.Installment == installment);
    }

    private static bool IsInterestFree(string interest)
    {
        return interest.Equals("Sem Juros", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record TokioMarinePaymentRow(
        int Installment,
        string Value,
        bool IsInterestFree);
}
