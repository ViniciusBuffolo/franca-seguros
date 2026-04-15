using QuoteMapper.Api.Dtos;

namespace QuoteMapper.Api.Interfaces
{
    public interface IHtmlTemplateService
    {
        Task<string> RenderQuoteHtmlAsync(GenerateQuoteDocumentRequestDto request);
    }
}