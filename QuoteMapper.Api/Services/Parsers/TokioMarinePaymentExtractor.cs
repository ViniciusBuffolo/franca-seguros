using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using QuoteMapper.Api.Models;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Services.Parsers;

public static class TokioMarinePaymentExtractor
{
    private static readonly string[] DebitHeaders =
    [
        "Débito/Pix Aut.",
        "Débito/Pix Aut",
        "Debito/Pix Aut.",
        "Debito/Pix Aut"
    ];

    private static readonly string[] FichaHeaders =
    [
        "Ficha"
    ];

    private static readonly string[] CardHeaders =
    [
        "Cartão",
        "Cartao"
    ];

    private static readonly Regex MoneyRegex = new(
        @"^(\d{1,3}(?:\.\d{3})*,\d{2}|\d{1,3},\d{2})",
        RegexOptions.Compiled);

    private static readonly Regex InterestRegex = new(
        @"^(Sem Juros|\d{1,2},\d{2})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static List<PaymentRowData> Extract(IFormFile file)
    {
        var pages = ExtractPages(file);
        var debitRows = ExtractRowsByHeaders(pages, DebitHeaders, FichaHeaders, CardHeaders);
        var fichaRows = ExtractRowsByHeaders(pages, FichaHeaders, DebitHeaders, CardHeaders);
        var cardRows = ExtractRowsByHeaders(pages, CardHeaders, DebitHeaders, FichaHeaders);

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

    private static List<TokioMarinePaymentRow> ExtractRowsByHeaders(
        List<string> pages,
        string[] targetHeaders,
        params string[][] otherHeaderGroups)
    {
        var rowsByInstallment = new Dictionary<int, TokioMarinePaymentRow>();

        foreach (var page in pages)
        {
            var section = TryExtractSection(page, targetHeaders, otherHeaderGroups);
            if (string.IsNullOrWhiteSpace(section))
                continue;

            foreach (var row in MergeBlocks(FindInstallmentBlocks(section)))
                rowsByInstallment[row.Installment] = row;
        }

        return rowsByInstallment.Values
            .OrderBy(row => row.Installment)
            .ToList();
    }

    private static string? TryExtractSection(
        string pageText,
        string[] targetHeaders,
        params string[][] otherHeaderGroups)
    {
        if (string.IsNullOrWhiteSpace(pageText))
            return null;

        var start = FindFirstIndex(pageText, targetHeaders);
        if (start < 0)
            return null;

        var end = pageText.Length;

        foreach (var headerGroup in otherHeaderGroups)
        {
            var otherIndex = FindFirstIndex(pageText, headerGroup, start + 1);
            if (otherIndex >= 0 && otherIndex < end)
                end = otherIndex;
        }

        return pageText[start..end];
    }

    private static int FindFirstIndex(
        string text,
        string[] headers,
        int startIndex = 0)
    {
        var index = -1;

        foreach (var header in headers)
        {
            var current = text.IndexOf(header, startIndex, StringComparison.OrdinalIgnoreCase);
            if (current < 0)
                continue;

            if (index < 0 || current < index)
                index = current;
        }

        return index;
    }

    private static List<TokioMarinePaymentRow> MergeBlocks(List<List<TokioMarinePaymentRow>> blocks)
    {
        var rowsByInstallment = new Dictionary<int, TokioMarinePaymentRow>();

        foreach (var block in blocks)
        {
            foreach (var row in block)
                rowsByInstallment[row.Installment] = row;
        }

        return rowsByInstallment.Values
            .OrderBy(row => row.Installment)
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
