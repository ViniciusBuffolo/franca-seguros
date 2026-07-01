using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers;

public interface IQuotePromptBuilder
{
    string InsurerKey { get; }
    string BuildPrompt(CoverageType coverageType);
}