using Microsoft.AspNetCore.Mvc;
using MyPdfApi.Models;
using MyPdfApi.Services;

namespace MyPdfApi.Controllers;

[ApiController]
[Route("api/pdf-extraction")]
public sealed class PdfExtractionController : ControllerBase
{
    private readonly IChatPdfService _chatPdfService;
    private readonly IQuoteTemplateMapper _quoteTemplateMapper;
    private readonly IQuoteTemplateRenderService _quoteTemplateRenderService;

    public PdfExtractionController(
        IChatPdfService chatPdfService,
        IQuoteTemplateMapper quoteTemplateMapper,
        IQuoteTemplateRenderService quoteTemplateRenderService)
    {
        _chatPdfService = chatPdfService;
        _quoteTemplateMapper = quoteTemplateMapper;
        _quoteTemplateRenderService = quoteTemplateRenderService;
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
            var prompt = BuildPrompt(request.CoverageType);

            var extracted = await _chatPdfService.ExtractTemplateFieldsAsync(
                sourceId,
                prompt,
                cancellationToken);

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

    private static string BuildPrompt(CoverageType coverageType)
    {
        var coverageTypeText = coverageType switch
        {
            CoverageType.RouboEFurto => "Roubo e Furto",
            CoverageType.Basico => "Basico",
            CoverageType.Ampliado => "Ampliado",
            CoverageType.Completo => "Completo",
            CoverageType.Master => "Master",
            CoverageType.Exclusivo => "Exclusivo",
            _ => "Completo"
        };

        var template = """
            Analise o documento PDF da cotação de seguro Allianz e retorne SOMENTE um JSON válido.

            O tipo de cobertura escolhido pelo usuário é: "__COVERAGE_TYPE__".

            Se o PDF contiver múltiplos planos, use SOMENTE a coluna/plano "__COVERAGE_TYPE__".

            IMPORTANTE:
            - Para os campos abaixo, extraia o valor da coluna "Limite Máximo de Indenização" do plano escolhido.
            - NÃO use os valores da coluna "Preço por Cobertura" para preencher esses campos.
            - "Preço por Cobertura" deve ser ignorado para:
              danosMateriais
              danosCorporais
              danosMorais
              appMorte
              appInvalidez
              assistenciaGuincho
              carroReserva
              franquiaParabrisa
              franquiaVidroLateral
              franquiaFarolConvencional
              franquiaLanternaConvencional
              franquiaFarolXenonLed
              franquiaLanternaLed
              franquiaRetrovisor
              franquiaLanternaAuxiliar
              franquiaPneuRoda
              franquiaPequenosReparos
              franquiaVeiculo
              tipoFranquiaVeiculo

            Regras de extração por campo:
            - danosMateriais = valor do limite da linha "RCF - Danos Materiais"
            - danosCorporais = valor do limite da linha "RCF - Danos Corporais"
            - danosMorais = valor do limite da linha "RCF - Danos Morais e Estéticos" ou equivalente
            - appMorte = valor do limite da linha "APP - Morte"
            - appInvalidez = valor do limite da linha "APP - Invalidez Permanente"
            - assistenciaGuincho = valor do benefício/limite da linha "Assistência 24 hs" ou "Guincho"
            - carroReserva = valor do benefício/limite da linha "Carro Reserva"
            - tipoFranquiaVeiculo = tipo da franquia do veículo, como Normal, Reduzida ou Majorada
            - franquiaVeiculo = valor monetário da franquia do veículo
            - paymentRows = tabela de pagamento, onde os valores podem vir das áreas de formas de pagamento do documento

            Para campos como:
            - assistência 24h
            - guincho
            - carro reserva
            - tipo de carro reserva

            use sempre o valor descritivo do benefício/plano do produto escolhido, e não o preço.

            Não escreva explicações.
            Não use markdown.
            Não use blocos de código.
            Não escreva texto antes ou depois do JSON.

            Retorne exatamente nesta estrutura:

            {
              "proponente": "",
              "fipeValue": "",
              "vehicle": "",
              "plate": "",
              "danosMateriais": "",
              "danosCorporais": "",
              "danosMorais": "",
              "appMorte": "",
              "appInvalidez": "",
              "assistenciaGuincho": "",
              "carroReserva": "",
              "franquiaParabrisa": "",
              "franquiaVidroLateral": "",
              "franquiaFarolConvencional": "",
              "franquiaLanternaConvencional": "",
              "franquiaFarolXenonLed": "",
              "franquiaLanternaLed": "",
              "franquiaRetrovisor": "",
              "franquiaLanternaAuxiliar": "",
              "franquiaPneuRoda": "",
              "franquiaPequenosReparos": "",
              "franquiaVeiculo": "",
              "tipoFranquiaVeiculo": "",
              "condutorPrincipal": "",
              "usoVeiculo": "",
              "estadoCivil": "",
              "cepPernoite": "",
              "resideEm": "",
              "condutores18a25": "",
              "paymentRows": [
                {
                  "parcela": "",
                  "carne": "",
                  "cartaoCredito": "",
                  "debitoConta": ""
                }
              ]
            }

            Regras finais:
            1. Extraia os dados de todas as páginas do PDF.
            2. Se um campo não existir, deixe string vazia.
            3. Se houver múltiplos planos, use somente o plano "__COVERAGE_TYPE__".
            4. Para os campos de cobertura, use SOMENTE a coluna "Limite Máximo de Indenização".
            5. Não use "Preço por Cobertura" para preencher campos de cobertura.
            6. Retorne somente JSON válido.
            """;

        return template.Replace("__COVERAGE_TYPE__", coverageTypeText);
    }
}