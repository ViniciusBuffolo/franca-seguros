using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Interfaces
{
    public interface IQuoteParser
    {
        string InsurerKey { get; }
        string InsurerName { get; }
        string LogoFileName { get; }

        bool CanParse(string rawText, string normalizedText, string? insurerHint = null);
        QuoteData Parse(string rawText, string normalizedText);
    }
}