using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Interfaces;

public interface IQuoteTemplateMapper
{
    QuoteTemplateData MapToTemplateData(QuoteTemplateExtractionResult extracted);
}
