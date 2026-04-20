using MyPdfApi.Models;

namespace MyPdfApi.Services;

public interface IQuoteTemplateMapper
{
    QuoteTemplateData MapToTemplateData(QuoteTemplateExtractionResult extracted);
}