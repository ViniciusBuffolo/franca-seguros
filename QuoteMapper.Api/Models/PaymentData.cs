namespace QuoteMapper.Api.Models
{
    public class PaymentData
    {
        public Dictionary<string, string> Boleto { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> DebitAccount { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CreditCard { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}