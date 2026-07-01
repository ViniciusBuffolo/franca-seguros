using Microsoft.AspNetCore.Mvc;
using QuoteMapper.Api.Dtos;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Controllers
{
    // Define este controller como um controller de API
    [ApiController]

    // Define a rota base: api/Quote
    [Route("api/[controller]")]
    public class QuoteController : ControllerBase
    {
        // Serviço responsável por extrair texto do PDF e mapear os dados da cotação
        private readonly IQuoteService _quoteService;

        // Serviço responsável por renderizar o HTML da cotação
        private readonly IQuoteHtmlTemplateService _quoteHtmlTemplateService;

        // Serviço responsável por buscar o valor FIPE
        private readonly IFipeService _fipeService;

        // Construtor com injeção de dependência dos serviços necessários
        public QuoteController(
            IQuoteService quoteService,
            IQuoteHtmlTemplateService quoteHtmlTemplateService,
            IFipeService fipeService)
        {
            _quoteService = quoteService;
            _quoteHtmlTemplateService = quoteHtmlTemplateService;
            _fipeService = fipeService;
        }

        // Endpoint para upload do arquivo e retorno dos dados mapeados
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadQuoteRequestDto request)
        {
            // Valida se o arquivo foi enviado
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "File is required." });

            // Define o caminho da pasta Uploads dentro da aplicação
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            // Cria a pasta caso ela não exista
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Garante que apenas o nome do arquivo será utilizado
            var safeFileName = Path.GetFileName(request.File.FileName);

            // Gera um nome único para evitar conflitos
            var filePath = Path.Combine(uploadsPath, $"{Guid.NewGuid()}_{safeFileName}");

            // Salva o arquivo fisicamente no disco
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // Extrai o texto do PDF salvo
            var extractedText = await _quoteService.ExtractTextFromPdfAsync(filePath);

            // Faz o parse do texto extraído para um objeto estruturado
            var mappedData = _quoteService.ParseQuote(extractedText, request.InsurerHint);

            // Normaliza alguns dados mapeados
            NormalizeMappedData(mappedData);

            // Inicializa o valor FIPE como nulo
            string? fipeValue = null;

            // Só tenta buscar FIPE se houver código FIPE e ano/modelo
            if (!string.IsNullOrWhiteSpace(mappedData.FipeCode) &&
                !string.IsNullOrWhiteSpace(mappedData.YearModel))
            {
                try
                {
                    // Pega o último trecho do campo YearModel, ex: "2023/2024" => "2024"
                    var modelYear = mappedData.YearModel
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault();

                    // Tenta converter o ano para inteiro
                    if (int.TryParse(modelYear, out var year))
                    {
                        // Consulta o valor FIPE usando os dados do veículo
                        var fipeResponse = await _fipeService.GetFipeValueAsync(new FipeRequestDto
                        {
                            CodigoTabelaReferencia = 173,
                            AnoModelo = year,
                            CodigoTipoCombustivel = 5,
                            ModeloCodigoExterno = mappedData.FipeCode.Replace("-", "")
                        });

                        // Converte a resposta para string
                        fipeValue = fipeResponse?.ToString();
                    }
                }
                catch
                {
                    // Em caso de erro na consulta FIPE, mantém como nulo
                    fipeValue = null;
                }
            }

            // Retorna os dados processados para o cliente
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

        // Endpoint para receber os dados da cotação já montados e renderizar o HTML
        [HttpPost("render-html")]
        public IActionResult RenderHtml([FromBody] GenerateQuoteDocumentRequestDto request)
        {
            // Valida se o objeto principal foi enviado corretamente
            if (request?.MappedData == null)
                return BadRequest(new { message = "MappedData is required." });

            // Normaliza os dados recebidos
            NormalizeMappedData(request.MappedData);

            // Resolve o plano preferido com base no que foi enviado e nos planos disponíveis
            request.SelectedPlan = ResolvePreferredPlan(request.MappedData, request.SelectedPlan);

            // Gera o HTML final da cotação
            var html = _quoteHtmlTemplateService.RenderQuoteHtml(request);

            // Retorna o conteúdo HTML com o content type correto
            return Content(html, "text/html; charset=utf-8");
        }

        // Endpoint para fazer upload do PDF, extrair/mapear os dados,
        // buscar FIPE, montar o request do HTML e retornar tudo junto
        [HttpPost("upload-render-html")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRenderHtml([FromForm] UploadQuoteRequestDto request)
        {
            // Valida se o arquivo foi enviado
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { message = "File is required." });

            // Define o caminho da pasta Uploads
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            // Cria a pasta se ela ainda não existir
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Obtém um nome seguro para o arquivo
            var safeFileName = Path.GetFileName(request.File.FileName);

            // Gera um nome único para armazenar o arquivo
            var filePath = Path.Combine(uploadsPath, $"{Guid.NewGuid()}_{safeFileName}");

            // Salva o arquivo no disco
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // Extrai o texto do PDF
            var extractedText = await _quoteService.ExtractTextFromPdfAsync(filePath);

            // Faz o parse do texto extraído para um objeto estruturado
            var mappedData = _quoteService.ParseQuote(extractedText, request.InsurerHint);

            // Normaliza os dados extraídos
            NormalizeMappedData(mappedData);

            // Inicializa o valor FIPE como nulo
            string? fipeValue = null;

            // Só consulta FIPE se houver código FIPE e ano/modelo
            if (!string.IsNullOrWhiteSpace(mappedData.FipeCode) &&
                !string.IsNullOrWhiteSpace(mappedData.YearModel))
            {
                try
                {
                    // Extrai o ano final do campo YearModel
                    var modelYear = mappedData.YearModel
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault();

                    // Se conseguir converter o ano, consulta a FIPE
                    if (int.TryParse(modelYear, out var year))
                    {
                        var fipeResponse = await _fipeService.GetFipeValueAsync(new FipeRequestDto
                        {
                            CodigoTabelaReferencia = 173,
                            AnoModelo = year,
                            CodigoTipoCombustivel = 5,
                            ModeloCodigoExterno = mappedData.FipeCode.Replace("-", "")
                        });

                        // Salva o retorno da FIPE em string
                        fipeValue = fipeResponse?.ToString();
                    }
                }
                catch
                {
                    // Se ocorrer erro, apenas mantém fipeValue como nulo
                    fipeValue = null;
                }
            }

            // Resolve automaticamente qual plano será usado, preferindo "Básico"
            var selectedPlan = ResolvePreferredPlan(mappedData, "Básico");

            // Monta o DTO completo para renderização do HTML
            var htmlRequest = new GenerateQuoteDocumentRequestDto
            {
                MappedData = mappedData,
                SelectedPlan = selectedPlan,
                IssueDate = DateTime.Now.ToString("dd/MM/yyyy"),
                City = "",
                FipeValue = fipeValue,
                BrokerContactName = mappedData.Broker?.Name,
                BrokerContactPhone = mappedData.Broker?.Phone,
                BrokerContactEmail = mappedData.Broker?.Email
            };

            // Gera o HTML final da cotação
            var html = _quoteHtmlTemplateService.RenderQuoteHtml(htmlRequest);

            // Retorna tanto os dados processados quanto o HTML renderizado
            return Ok(new
            {
                fileName = safeFileName,
                detectedInsurer = mappedData.InsurerName,
                insurerKey = mappedData.InsurerKey,
                logoFileName = mappedData.InsurerLogoFileName,
                fipeValue,
                selectedPlan,
                mappedData,
                html
            });
        }

        // Método auxiliar para definir qual plano deve ser usado
        private static string ResolvePreferredPlan(QuoteData mappedData, string? requestedPlan = null)
        {
            // Se não houver coberturas, retorna um nome padrão
            if (mappedData?.Coverages == null || mappedData.Coverages.Count == 0)
                return "Plano Único";

            // Se o plano solicitado existir nas coberturas, retorna ele
            if (!string.IsNullOrWhiteSpace(requestedPlan) &&
                mappedData.Coverages.ContainsKey(requestedPlan))
            {
                return requestedPlan;
            }

            // Ordem de preferência dos planos
            if (mappedData.Coverages.ContainsKey("Básico"))
                return "Básico";

            if (mappedData.Coverages.ContainsKey("Master"))
                return "Master";

            if (mappedData.Coverages.ContainsKey("Plano Único"))
                return "Plano Único";

            // Se nenhum dos anteriores existir, retorna o primeiro disponível
            return mappedData.Coverages.Keys.First();
        }

        // Método auxiliar para normalizar dados após o parse
        private static void NormalizeMappedData(QuoteData? mappedData)
        {
            // Se não houver dados, não faz nada
            if (mappedData == null)
                return;

            // Normaliza o nome do arquivo da logo para trim + minúsculo
            if (!string.IsNullOrWhiteSpace(mappedData.InsurerLogoFileName))
            {
                mappedData.InsurerLogoFileName = mappedData.InsurerLogoFileName
                    .Trim()
                    .ToLowerInvariant();
            }
        }
    }
}
