using Microsoft.AspNetCore.Mvc;
using QuoteMapper.Api.Dtos;
using QuoteMapper.Api.Interfaces;

namespace QuoteMapper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuoteController : ControllerBase
    {
        private readonly IQuoteService _quoteService;
        private readonly IQuoteHtmlTemplateService _quoteHtmlTemplateService;
        private readonly IFipeService _fipeService;

        public QuoteController(
            IQuoteService quoteService,
            IQuoteHtmlTemplateService quoteHtmlTemplateService,
            IFipeService fipeService)
        {
            _quoteService = quoteService;
            _quoteHtmlTemplateService = quoteHtmlTemplateService;
            _fipeService = fipeService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadQuoteRequestDto request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "File is required." });

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var safeFileName = Path.GetFileName(request.File.FileName);
            var filePath = Path.Combine(uploadsPath, $"{Guid.NewGuid()}_{safeFileName}");

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var extractedText = await _quoteService.ExtractTextFromPdfAsync(filePath);
            var mappedData = _quoteService.ParseQuote(extractedText, request.InsurerHint);

            string? fipeValue = null;

            if (!string.IsNullOrWhiteSpace(mappedData.FipeCode) && !string.IsNullOrWhiteSpace(mappedData.YearModel))
            {
                // This stays optional because your FIPE lookup may need a better dynamic resolver later.
                // For now, keep it safe and do not hardcode insurer quote values.
                try
                {
                    var modelYear = mappedData.YearModel
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault();

                    if (int.TryParse(modelYear, out var year))
                    {
                        var fipeResponse = await _fipeService.GetFipeValueAsync(new FipeRequestDto
                        {
                            CodigoTabelaReferencia = 173,
                            AnoModelo = year,
                            CodigoTipoCombustivel = 5,
                            ModeloCodigoExterno = mappedData.FipeCode.Replace("-", "")
                        });

                        fipeValue = fipeResponse?.ToString();
                    }
                }
                catch
                {
                    fipeValue = null;
                }
            }

            return Ok(new
            {
                fileName = safeFileName,
                detectedInsurer = mappedData.InsurerName,
                insurerKey = mappedData.InsurerKey,
                logoFileName = mappedData.InsurerLogoFileName,
                fipeValue,
                mappedData
            });
        }

        [HttpPost("render-html")]
        public IActionResult RenderHtml([FromBody] GenerateQuoteDocumentRequestDto request)
        {
            var html = _quoteHtmlTemplateService.RenderQuoteHtml(request);
            return Content(html, "text/html; charset=utf-8");
        }
    }
}