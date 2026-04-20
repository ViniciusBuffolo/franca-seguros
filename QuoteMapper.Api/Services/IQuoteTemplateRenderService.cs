using MyPdfApi.Models;

namespace MyPdfApi.Services;

public interface IQuoteTemplateRenderService
{
    Task<string> RenderAsync(QuoteTemplateData data, CancellationToken cancellationToken = default);
}