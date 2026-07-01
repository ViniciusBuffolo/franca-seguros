using Microsoft.AspNetCore.Http;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Interfaces;

public interface IChatPdfService
{
    Task<string> UploadPdfAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task<QuoteTemplateExtractionResult> ExtractTemplateFieldsAsync(
        string sourceId,
        string extractionPrompt,
        CancellationToken cancellationToken = default);
}
