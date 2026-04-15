using QuoteMapper.Api.Interfaces;

namespace QuoteMapper.Api.Services
{
    public class QuoteParserFactory : IQuoteParserFactory
    {
        private readonly IEnumerable<IQuoteParser> _parsers;

        public QuoteParserFactory(IEnumerable<IQuoteParser> parsers)
        {
            _parsers = parsers;
        }

        public IQuoteParser Resolve(string rawText, string normalizedText, string? insurerHint = null)
        {
            var parser = _parsers.FirstOrDefault(p => p.CanParse(rawText, normalizedText, insurerHint));

            if (parser == null)
                throw new InvalidOperationException("No parser was able to process the quote.");

            return parser;
        }
    }
}