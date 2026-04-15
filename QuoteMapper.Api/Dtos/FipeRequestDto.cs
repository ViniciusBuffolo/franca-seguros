namespace QuoteMapper.Api.Dtos
{
    public class FipeRequestDto
    {
        public int CodigoTabelaReferencia { get; set; }
        public int AnoModelo { get; set; }
        public int CodigoTipoCombustivel { get; set; }
        public string ModeloCodigoExterno { get; set; } = string.Empty;
    }
}