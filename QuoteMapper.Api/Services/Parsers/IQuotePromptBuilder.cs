using MyPdfApi.Models;

namespace MyPdfApi.Services.Parsers;

public interface IQuotePromptBuilder
{
    string InsurerKey { get; }
    string BuildPrompt(CoverageType coverageType);
}