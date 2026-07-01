namespace QuoteMapper.Api.Models;

public sealed class QuoteTemplateExtractionResult
{
    public string Insurer { get; set; } = string.Empty;

    public string Proponente { get; set; } = string.Empty;

    public string FipeValue { get; set; } = string.Empty;
    public string FipeCode { get; set; } = string.Empty;
    public string AnoModelo { get; set; } = string.Empty;

    public string Vehicle { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;

    public string DanosMateriais { get; set; } = string.Empty;
    public string DanosCorporais { get; set; } = string.Empty;
    public string DanosMorais { get; set; } = string.Empty;
    public string AppMorte { get; set; } = string.Empty;
    public string AppInvalidez { get; set; } = string.Empty;

    public string AssistenciaGuincho { get; set; } = string.Empty;
    public string CarroReserva { get; set; } = string.Empty;
    public string TipoCarroReserva { get; set; } = string.Empty;

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
    public List<int> PaginasReferencia { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
    public string Combustivel { get; set; } = string.Empty;
}

public sealed class PaymentRowData
{
    public string Parcela { get; set; } = string.Empty;
    public string Carne { get; set; } = string.Empty;
    public string CartaoCredito { get; set; } = string.Empty;
    public string DebitoConta { get; set; } = string.Empty;

    public bool CarneSemJuros { get; set; }
    public bool CartaoCreditoSemJuros { get; set; }
    public bool DebitoContaSemJuros { get; set; }

    public string Insurer { get; set; } = string.Empty;
}