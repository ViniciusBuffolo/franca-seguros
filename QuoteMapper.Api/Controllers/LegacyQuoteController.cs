using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace QuoteMapper.Api.Controllers
{
    [ApiController]
    [Route("api/Quote")]
    public class LegacyQuoteController : ControllerBase
    {
        private readonly string _uploadPath;

        public LegacyQuoteController(IWebHostEnvironment env)
        {
            _uploadPath = Path.Combine(env.ContentRootPath, "Uploads");

            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        [HttpPost("process-pdf")]
        public async Task<IActionResult> ProcessQuote(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Arquivo não enviado.");

            var safeFileName = Path.GetFileName(file.FileName);
            var fileName = $"{Guid.NewGuid()}_{safeFileName}";
            var filePath = Path.Combine(_uploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var pdfText = ExtractText(filePath);

            var result = new
            {
                SuasInformacoes = new
                {
                    Nome = GetMatch(pdfText, @"SUAS INFORMAÇÕES\s*Nome:\s*(.*?)(?=CPF|INFORMAÇÕES|$)"),
                    CpfCnpj = GetMatch(pdfText, @"CPF/CNPJ:\s*([\d\.\-/]+)")
                },
                InformacoesDoSeuSeguro = new
                {
                    Vigencia = GetMatch(pdfText, @"Vigência:\s*(.*?)(?=Nº|Tipo|$)"),
                    TipoSeguro = GetMatch(pdfText, @"Tipo de Seguro:\s*(.*?)(?=Produto|$)"),
                    Veiculo = GetMatch(pdfText, @"Veículo:\s*(.*?)(?=Versão|$)"),
                    CodFipe = GetMatch(pdfText, @"Cód\. FIPE:\s*([\d-]+)"),
                    Placa = GetMatch(pdfText, @"Placa:\s*([A-Z0-9]{7})"),
                    Chassi = GetMatch(pdfText, @"Chassi:\s*([A-Z0-9]+)(?=Grupo|$)"),
                    ZeroKm = GetMatch(pdfText, @"Zero Km:\s*(Sim|Não)"),
                    AnoModelo = GetMatch(pdfText, @"Ano/Modelo:\s*(\d{4})"),
                    CategoriaRisco = GetMatch(pdfText, @"Categoria de Risco:\s*(.*?)(?=Seguradora|$)"),
                    KitGas = GetMatch(pdfText, @"Kit gás:\s*(Sim|Não)"),
                    NoApolice = GetMatch(pdfText, @"Nº da Apólice:\s*(.*?)(?=Produto|$)"),
                    Produto = GetMatch(pdfText, @"Produto:\s*(.*?)(?=Veículo|$)"),
                    Versao = GetMatch(pdfText, @"Versão:\s*([\d/\.]+)"),
                    CondicoesGerais = GetMatch(pdfText, @"Condições Gerais:\s*([\d/]+[A-Z]?)"),
                    ClasseBonus = GetMatch(pdfText, @"Classe Bônus:\s*(\d+)"),
                    Grupo = GetMatch(pdfText, @"Grupo:\s*(\d+)"),
                    CepPernoite = GetMatch(pdfText, @"CEP Pernoite:\s*([\d-]+)"),
                    FinalidadeUso = GetMatch(pdfText, @"Finalidade de Uso:\s*(\w+)(?=Categoria|$)"),
                    SeguradoraAnterior = GetMatch(pdfText, @"Seguradora Anterior:\s*(.*?)(?=Kit|INFORMAÇÕES|$)")
                },
                InformacoesCondutorPrincipal = new
                {
                    Nome = GetMatch(pdfText, @"INFORMAÇÕES DO CONDUTOR PRINCIPAL\s*Nome:\s*(.*?)(?=CPF|Idade|$)"),
                    Cpf = GetMatch(pdfText, @"CPF:\s*([\d\.\-/]+)", 1),
                    Idade = GetMatch(pdfText, @"Idade:\s*(\d+\s*anos)"),
                    EstadoCivil = GetMatch(pdfText, @"Estado Civil:\s*([^D\r\n]+)"),
                    ResideEm = GetMatch(pdfText, @"reside em:\s*(\w+)(?=DETALHES|$)"),
                    ResideComJovens = pdfText.Contains("idade entre 18 a 25 anos:Não") ? "Não" : "Sim"
                }
            };

            return Ok(result);
        }

        private static string GetMatch(string input, string pattern, int index = 0)
        {
            var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (matches.Count > index)
                return matches[index].Groups[1].Value.Trim();

            return "Não encontrado";
        }

        private static string ExtractText(string path)
        {
            var text = new StringBuilder();

            using var pdf = PdfDocument.Open(path);
            foreach (var page in pdf.GetPages())
            {
                text.Append(page.Text);
            }

            return text.ToString();
        }
    }
}
