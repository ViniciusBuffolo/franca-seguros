namespace QuoteMapper.Api.Interfaces
{
    public interface IQuoteParserFactory
    {
        IQuoteParser Resolve(string rawText, string normalizedText, string? insurerHint = null);
    }
}