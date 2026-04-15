using System.Text.RegularExpressions;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly IQuoteParserFactory _quoteParserFactory;

        public QuoteService(IQuoteParserFactory quoteParserFactory)
        {
            _quoteParserFactory = quoteParserFactory;
        }

        public async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            var text = string.Empty;

            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    text += page.Text + Environment.NewLine;
                }
            }

            return await Task.FromResult(text);
        }

        public string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        public QuoteData ParseQuote(string rawText, string? insurerHint = null)
        {
            var normalized = NormalizeText(rawText);
            var parser = _quoteParserFactory.Resolve(rawText, normalized, insurerHint);
            return parser.Parse(rawText, normalized);
        }
    }
}