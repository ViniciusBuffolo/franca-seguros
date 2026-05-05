using Microsoft.AspNetCore.Mvc;
using MyPdfApi.Models;
using MyPdfApi.Services;
using MyPdfApi.Services.Parsers;
using QuoteMapper.Api.Dtos;
using QuoteMapper.Api.Interfaces;

namespace MyPdfApi.Controllers;

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
            //var prompt = BuildPrompt(request.CoverageType);
            var promptBuilder = _promptBuilderFactory.GetByInsurer(request.Insurer);
            var prompt = promptBuilder.BuildPrompt(request.CoverageType);

            var extracted = await _chatPdfService.ExtractTemplateFieldsAsync(
                sourceId,
                prompt,
                cancellationToken);

            extracted.Insurer = request.Insurer;

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
                CodigoTipoCombustivel = 5,
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

    /*
    private static string BuildPrompt(CoverageType coverageType)
    {
        var coverageTypeText = coverageType switch
        {
            CoverageType.RouboEFurto => "Roubo e Furto",
            CoverageType.Basico => "Básico",
            CoverageType.Ampliado => "Ampliado",
            CoverageType.Completo => "Completo",
            CoverageType.Master => "Master",
            CoverageType.Exclusivo => "Exclusivo",
            _ => "Completo"
        };

        var template = """
        Analise o documento PDF da cotação de seguro Allianz e retorne SOMENTE um JSON válido.

        O tipo de cobertura escolhido pelo usuário é: "__COVERAGE_TYPE__".

        O PDF pode conter múltiplos planos, como:
        - Roubo e Furto
        - Básico
        - Ampliado
        - Completo
        - Master
        - Exclusivo

        Use SOMENTE os dados do plano "__COVERAGE_TYPE__".
        Ignore completamente os valores dos demais planos.

        REGRAS GERAIS:
        - Leia todas as páginas do PDF.
        - Retorne apenas JSON válido.
        - Não escreva explicações.
        - Não use markdown.
        - Não use blocos de código.
        - Não escreva nada antes ou depois do JSON.
        - Se um campo não existir, retorne string vazia.
        - Preserve os valores exatamente como aparecem no PDF, inclusive formato monetário, texto descritivo e percentuais.

        REGRAS DE COBERTURA:
        - Para os campos de cobertura abaixo, extraia SOMENTE o valor da coluna "Limite Máximo de Indenização" do plano "__COVERAGE_TYPE__".
        - NÃO use os valores da coluna "Preço por Cobertura" para preencher esses campos.

        Campos que devem usar SOMENTE "Limite Máximo de Indenização" ou valor descritivo do benefício:
        - danosMateriais
        - danosCorporais
        - danosMorais
        - appMorte
        - appInvalidez
        - assistenciaGuincho
        - carroReserva
        - tipoCarroReserva
        - franquiaParabrisa
        - franquiaVidroLateral
        - franquiaFarolConvencional
        - franquiaLanternaConvencional
        - franquiaFarolXenonLed
        - franquiaLanternaLed
        - franquiaRetrovisor
        - franquiaLanternaAuxiliar
        - franquiaPneuRoda
        - franquiaPequenosReparos
        - franquiaVeiculo
        - tipoFranquiaVeiculo

        REGRAS DE MAPEAMENTO DE COBERTURA:
        - danosMateriais = valor da linha "RCF - Danos Materiais"
        - danosCorporais = valor da linha "RCF - Danos Corporais"
        - danosMorais = valor da linha "RCF - Danos Morais e Estéticos" ou equivalente
        - appMorte = valor da linha "APP - Morte"
        - appInvalidez = valor da linha "APP - Invalidez Permanente"
        - assistenciaGuincho = usar o valor descritivo do benefício de assistência, como "Km Livre", "Km Ilimitado" ou equivalente, referente ao plano escolhido
        - carroReserva = usar somente a quantidade de diárias do carro reserva do plano escolhido, como "15 Dias"
        - tipoCarroReserva = usar o tipo textual da linha "Tipo de Carro Reserva" do plano escolhido, como "Básico", "Intermediário" ou "Superior"
        - tipoFranquiaVeiculo = tipo textual da franquia do veículo, como "Normal", "50% da Normal", "Reduzida" ou "Majorada"
        - franquiaVeiculo = valor monetário da franquia do veículo

        REGRAS ESPECÍFICAS PARA FORMAS DE PAGAMENTO:
        - Procure a seção "OUTRAS FORMAS DE PAGAMENTO".
        - Nessa seção existem 3 tabelas independentes:
          1. Boleto Bancário
          2. Débito em Conta
          3. Cartão de Crédito
        - Cada tabela possui:
          - uma coluna "Parcelas"
          - uma coluna "Juros"
          - colunas de planos: Roubo e Furto, Básico, Ampliado, Completo, Master e Exclusivo
        - Para preencher paymentRows, use SOMENTE a coluna do plano "__COVERAGE_TYPE__".
        - A coluna "carne" no JSON deve receber os valores da tabela "Boleto Bancário".
        - A coluna "cartaoCredito" no JSON deve receber os valores da tabela "Cartão de Crédito".
        - A coluna "debitoConta" no JSON deve receber os valores da tabela "Débito em Conta".
        - Monte uma linha para cada quantidade de parcela encontrada.
        - Una as tabelas pelo número da parcela.
        - Se uma parcela existir em uma forma de pagamento e não existir em outra, preencha a que faltar com string vazia.
        - Mantenha a parcela exatamente como aparece na tabela, por exemplo: "01", "02", "03", etc.
        - Não invente valores.
        - Não misture valores de outros planos.

        REGRAS DE DESTAQUE DE JUROS:
        - Se a condição da parcela na tabela correspondente for "sem juros", marque o campo booleano correspondente como true.
        - Caso contrário, marque como false.
        - carneSemJuros corresponde à tabela "Boleto Bancário"
        - cartaoCreditoSemJuros corresponde à tabela "Cartão de Crédito"
        - debitoContaSemJuros corresponde à tabela "Débito em Conta"

        REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
        - proponente = nome do segurado/proponente
        - vehicle = descrição do veículo
        - plate = placa
        - condutorPrincipal = nome do principal condutor
        - usoVeiculo = finalidade de uso
        - estadoCivil = estado civil
        - cepPernoite = CEP de pernoite
        - resideEm = residência do principal condutor
        - condutores18a25 = resposta sobre condutores entre 18 e 25 anos

        REGRAS FIPE (CRÍTICO):
        - fipeCode = extrair EXATAMENTE o valor após "Cód. FIPE:"
        - Exemplo correto: "015090-8"
        - anoModelo = extrair EXATAMENTE o valor de "Ano/Modelo"
        - Exemplo correto: "2014"
        - NÃO extrair valores monetários
        - NÃO usar valores da tabela de cobertura
        - fipeValue = sempre string vazia
        - O campo fipeCode DEVE ser preenchido quando existir "Cód. FIPE"
        - O campo anoModelo DEVE ser preenchido quando existir "Ano/Modelo"
        - NÃO deixar esses campos vazios se existirem no PDF

        Retorne exatamente nesta estrutura:

        {
          "proponente": "",
          "fipeValue": "",
          "fipeCode": "",
          "anoModelo": "",
          "vehicle": "",
          "plate": "",
          "danosMateriais": "",
          "danosCorporais": "",
          "danosMorais": "",
          "appMorte": "",
          "appInvalidez": "",
          "assistenciaGuincho": "",
          "carroReserva": "",
          "tipoCarroReserva": "",
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
            "carneSemJuros": false,
            "cartaoCredito": "",
            "cartaoCreditoSemJuros": false,
            "debitoConta": "",
            "debitoContaSemJuros": false
          }
        ]
        }

        REGRAS FINAIS:
        1. Use somente o plano "__COVERAGE_TYPE__".
        2. Para coberturas, use somente "Limite Máximo de Indenização" ou o valor descritivo do benefício.
        3. Nunca use "Preço por Cobertura" para preencher coberturas.
        4. Para paymentRows, use somente a coluna do plano "__COVERAGE_TYPE__" nas 3 tabelas de pagamento.
        5. Ignore a coluna "Juros".
        6. Retorne somente JSON válido.
        """;

        return template.Replace("__COVERAGE_TYPE__", coverageTypeText);
    }*/
}