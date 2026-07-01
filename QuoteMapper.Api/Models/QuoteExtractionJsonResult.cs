namespace QuoteMapper.Api.Models;

public sealed class QuoteExtractionJsonResult
{
    public SeguradoJson Segurado { get; set; } = new();
    public SeguroJson Seguro { get; set; } = new();
    public CondutorPrincipalJson CondutorPrincipal { get; set; } = new();
    public List<OfertaCoberturaJson> OfertasCoberturas { get; set; } = new();
    public InformacoesComplementaresJson InformacoesComplementares { get; set; } = new();
    public List<int> PaginasReferencia { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
}

public sealed class SeguradoJson
{
    public string Nome { get; set; } = string.Empty;
    public string CpfCnpj { get; set; } = string.Empty;
    public string Idade { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
}

public sealed class SeguroJson
{
    public string Vigencia { get; set; } = string.Empty;
    public string NumeroApolice { get; set; } = string.Empty;
    public string TipoSeguro { get; set; } = string.Empty;
    public string Veiculo { get; set; } = string.Empty;
    public string Versao { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string Chassi { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string FinalidadeUso { get; set; } = string.Empty;
    public string SeguradoraAnterior { get; set; } = string.Empty;
}

public sealed class CondutorPrincipalJson
{
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Idade { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string PossuiCondutoresAdicionais { get; set; } = string.Empty;
    public string CondicaoCondutoresAdicionais { get; set; } = string.Empty;
}

public sealed class OfertaCoberturaJson
{
    public string Plano { get; set; } = string.Empty;
    public string Cobertura { get; set; } = string.Empty;
    public string LimiteMaximoIndenizacao { get; set; } = string.Empty;
    public string Preco { get; set; } = string.Empty;
}

public sealed class InformacoesComplementaresJson
{
    public string Franquias { get; set; } = string.Empty;
    public string Assistencia24h { get; set; } = string.Empty;
    public string CarroReserva { get; set; } = string.Empty;
    public string FormasPagamento { get; set; } = string.Empty;
    public string Parcelas { get; set; } = string.Empty;
    public string DescontosCondicoesAdicionais { get; set; } = string.Empty;
}