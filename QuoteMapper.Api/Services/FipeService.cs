using System.Text.Json;
using QuoteMapper.Api.Dtos;
using QuoteMapper.Api.Interfaces;

namespace QuoteMapper.Api.Services
{
    public class FipeService : IFipeService
    {
        private readonly HttpClient _httpClient;

        public FipeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetFipeValueAsync(FipeRequestDto request)
        {
            var url = "https://veiculos.fipe.org.br/api/veiculos/ConsultarValorComTodosParametros";

            var form = new Dictionary<string, string>
            {
                { "codigoTabelaReferencia", request.CodigoTabelaReferencia.ToString() },
                { "codigoMarca", "" },
                { "codigoModelo", "" },
                { "codigoTipoVeiculo", "1" },
                { "anoModelo", request.AnoModelo.ToString() },
                { "codigoTipoCombustivel", request.CodigoTipoCombustivel.ToString() },
                { "tipoVeiculo", "carro" },
                { "modeloCodigoExterno", request.ModeloCodigoExterno },
                { "tipoConsulta", "codigo" }
            };

            using var content = new FormUrlEncodedContent(form);
            using var response = await _httpClient.PostAsync(url, content);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Valor", out var valorElement))
            {
                return valorElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}