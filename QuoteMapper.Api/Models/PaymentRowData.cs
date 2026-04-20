namespace MyPdfApi.Models;

public sealed class PaymentRowData
{
    public string Parcela { get; set; } = string.Empty;
    public string Carne { get; set; } = string.Empty;
    public string CartaoCredito { get; set; } = string.Empty;
    public string DebitoConta { get; set; } = string.Empty;
}