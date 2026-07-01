namespace QuoteMapper.Api.Models;

public sealed class QuoteTemplateData
{
    public string IssueDate { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public string LogoAllianzUrl { get; set; } = string.Empty;
    public string LogoCorretoraUrl { get; set; } = string.Empty;
    public string LogoFrancaUrl { get; set; } = string.Empty;

    public string Proponente { get; set; } = string.Empty;
    public string FipeCode { get; set; } = string.Empty;
    public string FipeValue { get; set; } = string.Empty;
    public string Vehicle { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;

    public string DanosMateriais { get; set; } = string.Empty;
    public string DanosCorporais { get; set; } = string.Empty;
    public string DanosMorais { get; set; } = string.Empty;
    public string AppMorte { get; set; } = string.Empty;
    public string AppInvalidez { get; set; } = string.Empty;

    public string AssistenciaGuincho { get; set; } = string.Empty;
    public string CarroReserva { get; set; } = string.Empty;

    public string FranquiaParabrisa { get; set; } = string.Empty;
    public string FranquiaVidroLateral { get; set; } = string.Empty;
    public string FranquiaFarolConvencional { get; set; } = string.Empty;
    public string FranquiaLanternaConvencional { get; set; } = string.Empty;
    public string FranquiaFarolXenonLed { get; set; } = string.Empty;
    public string FranquiaLanternaLed { get; set; } = string.Empty;
    public string FranquiaRetrovisor { get; set; } = string.Empty;
    public string FranquiaLanternaAuxiliar { get; set; } = string.Empty;
    public string FranquiaPneuRoda { get; set; } = string.Empty;
    public string FranquiaPequenosReparos { get; set; } = string.Empty;
    public string FranquiaVeiculo { get; set; } = string.Empty;
    public string TipoFranquiaVeiculo { get; set; } = string.Empty;

    public string CondutorPrincipal { get; set; } = string.Empty;
    public string UsoVeiculo { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string CepPernoite { get; set; } = string.Empty;
    public string ResideEm { get; set; } = string.Empty;
    public string Condutores18a25 { get; set; } = string.Empty;

    public List<PaymentRowData> PaymentRows { get; set; } = new();

    public string BrokerContactName { get; set; } = string.Empty;
    public string BrokerContactPhone { get; set; } = string.Empty;
    public string BrokerContactEmail { get; set; } = string.Empty;
}