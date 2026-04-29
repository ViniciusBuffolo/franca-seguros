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
            FipeCode = extracted.FipeCode,
            FipeValue = extracted.FipeValue,
            Vehicle = extracted.Vehicle,
            Plate = extracted.Plate,

            DanosMateriais = extracted.DanosMateriais,
            DanosCorporais = extracted.DanosCorporais,
            DanosMorais = extracted.DanosMorais,
            AppMorte = extracted.AppMorte,
            AppInvalidez = extracted.AppInvalidez,

            AssistenciaGuincho = extracted.AssistenciaGuincho,
            CarroReserva = FormatCarroReserva(
                extracted.CarroReserva,
                extracted.TipoCarroReserva),

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

            BrokerContactName = "França Seguros",
            BrokerContactPhone = "(28) 99917-5338",
            BrokerContactEmail = "francaseguros@gmail.com"
        };
    }

    private static string FormatCarroReserva(string? carroReserva, string? tipoCarroReserva)
    {
        var dias = (carroReserva ?? string.Empty).Trim();
        var tipo = (tipoCarroReserva ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(dias))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(tipo))
            return dias;

        return tipo.ToLowerInvariant() switch
        {
            "básico" => $"{dias} - manual",
            "basico" => $"{dias} - manual",

            "intermediário" => $"{dias} - Automatico",
            "intermediario" => $"{dias} - Automatico",

            "superior" => $"{dias} - SUV Automatico",

            _ => dias
        };
    }
}