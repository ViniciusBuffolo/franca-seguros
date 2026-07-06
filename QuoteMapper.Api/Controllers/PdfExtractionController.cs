using Microsoft.AspNetCore.Mvc;
using QuoteMapper.Api.Dtos;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Controllers;

[ApiController]
[Route("api/pdf-extraction")]
public sealed class PdfExtractionController : ControllerBase
{
    private readonly IChatPdfService _chatPdfService;
    private readonly IQuoteTemplateMapper _quoteTemplateMapper;
    private readonly IQuoteTemplateRenderService _quoteTemplateRenderService;
    private readonly IFipeService _fipeService;
    private readonly QuotePromptBuilderFactory _promptBuilderFactory;

    public PdfExtractionController(
        IChatPdfService chatPdfService,
        IQuoteTemplateMapper quoteTemplateMapper,
        IQuoteTemplateRenderService quoteTemplateRenderService,
        IFipeService fipeService,
        QuotePromptBuilderFactory promptBuilderFactory)
    {
        _chatPdfService = chatPdfService;
        _quoteTemplateMapper = quoteTemplateMapper;
        _quoteTemplateRenderService = quoteTemplateRenderService;
        _fipeService = fipeService;
        _promptBuilderFactory = promptBuilderFactory;
    }

    [HttpPost("extract-template-html")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ExtractTemplateHtml(
        [FromForm] ExtractTemplateHtmlRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { message = "PDF file is required." });

        if (!request.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only PDF files are allowed." });

        try
        {
            var sourceId = await _chatPdfService.UploadPdfAsync(request.File, cancellationToken);
            var promptBuilder = _promptBuilderFactory.GetByInsurer(request.Insurer);
            var prompt = promptBuilder.BuildPrompt(request.CoverageType);

            var extracted = await _chatPdfService.ExtractTemplateFieldsAsync(
                sourceId,
                prompt,
                cancellationToken);

            extracted.Insurer = request.Insurer;

            TryFillTokioMarinePaymentRows(request.File, request.Insurer, extracted);
            TryFillYelumFields(request.File, request.Insurer, extracted);
            TryFillBanestesFields(request.File, request.Insurer, extracted);
            TryFillAzulFields(request.File, request.Insurer, extracted);

            await TryFillFipeValueAsync(extracted, cancellationToken);

            var templateData = _quoteTemplateMapper.MapToTemplateData(extracted);
            var html = await _quoteTemplateRenderService.RenderAsync(templateData, cancellationToken);

            return Content(html, "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Failed to process PDF with ChatPDF.",
                error = ex.Message
            });
        }
    }

    private static void TryFillTokioMarinePaymentRows(
        IFormFile file,
        string? insurer,
        QuoteTemplateExtractionResult extracted)
    {
        if (!IsTokioMarine(insurer))
            return;

        var paymentRows = TokioMarinePaymentExtractor.Extract(file);

        if (paymentRows.Count > 0)
            extracted.PaymentRows = paymentRows;
    }

    private static void TryFillYelumFields(
        IFormFile file,
        string? insurer,
        QuoteTemplateExtractionResult extracted)
    {
        if (!IsYelum(insurer))
            return;

        YelumQuoteExtractor.Fill(file, extracted);
    }

    private static void TryFillBanestesFields(
        IFormFile file,
        string? insurer,
        QuoteTemplateExtractionResult extracted)
    {
        if (!IsBanestes(insurer))
            return;

        BanestesQuoteExtractor.Fill(file, extracted);
    }

    private static void TryFillAzulFields(
        IFormFile file,
        string? insurer,
        QuoteTemplateExtractionResult extracted)
    {
        if (!IsAzul(insurer))
            return;

        AzulQuoteExtractor.Fill(file, extracted);
    }

    private async Task TryFillFipeValueAsync(
        QuoteTemplateExtractionResult extracted,
        CancellationToken cancellationToken)
    {
        try
        {
            var fipeCode = (extracted.FipeCode ?? string.Empty).Trim();
            var anoModeloText = (extracted.AnoModelo ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fipeCode))
                return;

            if (!int.TryParse(anoModeloText, out var anoModelo))
                return;

            var request = new FipeRequestDto
            {
                CodigoTabelaReferencia = 173,
                CodigoTipoCombustivel = ResolveCodigoTipoCombustivel(extracted.Combustivel),
                AnoModelo = anoModelo,
                ModeloCodigoExterno = fipeCode
            };

            var fipeValue = await _fipeService.GetFipeValueAsync(request);

            if (!string.IsNullOrWhiteSpace(fipeValue))
            {
                extracted.FipeValue = fipeValue;
            }
        }
        catch
        {
            // Não quebra o fluxo caso a consulta FIPE falhe.
        }
    }

    private static int ResolveCodigoTipoCombustivel(string? combustivel)
    {
        var value = (combustivel ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

        return value switch
        {
            "GASOLINA" => 1,
            "FLEX" => 1,
            "ALCOOL" => 2,
            "ÁLCOOL" => 2,
            "DIESEL" => 3,
            _ => 1
        };
    }

    private static bool IsTokioMarine(string? insurer)
    {
        var normalized = (insurer ?? string.Empty)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return normalized.Equals("TokioMarine", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsYelum(string? insurer)
    {
        return string.Equals((insurer ?? string.Empty).Trim(), "Yelum", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBanestes(string? insurer)
    {
        return string.Equals((insurer ?? string.Empty).Trim(), "Banestes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAzul(string? insurer)
    {
        return string.Equals((insurer ?? string.Empty).Trim(), "Azul", StringComparison.OrdinalIgnoreCase);
    }

}
