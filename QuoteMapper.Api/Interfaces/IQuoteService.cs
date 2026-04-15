using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Interfaces
{
    public interface IQuoteService
    {
        Task<string> ExtractTextFromPdfAsync(string filePath);
        string NormalizeText(string text);
        QuoteData ParseQuote(string rawText, string? insurerHint = null);
    }
}