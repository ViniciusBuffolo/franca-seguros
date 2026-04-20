using MyPdfApi.Models;

namespace MyPdfApi.Services;

public sealed class QuoteTemplateMapper : IQuoteTemplateMapper
{
    private readonly IConfiguration _configuration;

    public QuoteTemplateMapper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public QuoteTemplateData MapToTemplateData(QuoteTemplateExtractionResult extracted)
    {
        var apiBaseUrl = _configuration["PublicApiBaseUrl"]?.TrimEnd('/')
                         ?? "https://localhost:5001";

        return new QuoteTemplateData
        {
            IssueDate = DateTime.Now.ToString("dd/MM/yyyy"),
            City = "Cachoeiro de Itapemirim - ES",

            LogoAllianzUrl = $"{apiBaseUrl}/logos/allianz.svg",
            LogoCorretoraUrl = $"{apiBaseUrl}/logos/corretora.svg",
            LogoFrancaUrl = $"{apiBaseUrl}/logos/franca.svg",

            Proponente = extracted.Proponente,
            FipeValue = extracted.FipeValue,
            Vehicle = extracted.Vehicle,
            Plate = extracted.Plate,

            DanosMateriais = extracted.DanosMateriais,
            DanosCorporais = extracted.DanosCorporais,
            DanosMorais = extracted.DanosMorais,
            AppMorte = extracted.AppMorte,
            AppInvalidez = extracted.AppInvalidez,

            AssistenciaGuincho = extracted.AssistenciaGuincho,
            CarroReserva = extracted.CarroReserva,

            FranquiaParabrisa = extracted.FranquiaParabrisa,
            FranquiaVidroLateral = extracted.FranquiaVidroLateral,
            FranquiaFarolConvencional = extracted.FranquiaFarolConvencional,
            FranquiaLanternaConvencional = extracted.FranquiaLanternaConvencional,
            FranquiaFarolXenonLed = extracted.FranquiaFarolXenonLed,
            FranquiaLanternaLed = extracted.FranquiaLanternaLed,
            FranquiaRetrovisor = extracted.FranquiaRetrovisor,
            FranquiaLanternaAuxiliar = extracted.FranquiaLanternaAuxiliar,
            FranquiaPneuRoda = extracted.FranquiaPneuRoda,
            FranquiaPequenosReparos = extracted.FranquiaPequenosReparos,
            FranquiaVeiculo = extracted.FranquiaVeiculo,
            TipoFranquiaVeiculo = extracted.TipoFranquiaVeiculo,

            CondutorPrincipal = extracted.CondutorPrincipal,
            UsoVeiculo = extracted.UsoVeiculo,
            EstadoCivil = extracted.EstadoCivil,
            CepPernoite = extracted.CepPernoite,
            ResideEm = extracted.ResideEm,
            Condutores18a25 = extracted.Condutores18a25,

            PaymentRows = extracted.PaymentRows,

            BrokerContactName = "Franca Seguros",
            BrokerContactPhone = "(28) 99999-9999",
            BrokerContactEmail = "contato@francaseguros.com.br"
        };
    }
}