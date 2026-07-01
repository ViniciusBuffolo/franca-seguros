using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Interfaces;

public interface IQuoteTemplateRenderService
{
    Task<string> RenderAsync(QuoteTemplateData data, CancellationToken cancellationToken = default);
}
