using QuoteMapper.Api.Dtos;

namespace QuoteMapper.Api.Interfaces
{
    public interface IQuoteHtmlTemplateService
    {
        string RenderQuoteHtml(GenerateQuoteDocumentRequestDto request);
    }
}