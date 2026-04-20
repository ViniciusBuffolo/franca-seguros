using Microsoft.AspNetCore.Http;
using MyPdfApi.Models;

namespace MyPdfApi.Services;

public interface IChatPdfService
{
    Task<string> UploadPdfAsync(IFormFile file, CancellationToken cancellationToken = default);

    Task<QuoteTemplateExtractionResult> ExtractTemplateFieldsAsync(
        string sourceId,
        string extractionPrompt,
        CancellationToken cancellationToken = default);
}