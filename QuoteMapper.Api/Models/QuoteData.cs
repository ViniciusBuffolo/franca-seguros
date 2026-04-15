namespace QuoteMapper.Api.Models
{
    public class QuoteData
    {
        public string? InsurerName { get; set; }
        public string? InsurerKey { get; set; }
        public string? InsurerLogoFileName { get; set; }

        public string? InsuredName { get; set; }
        public string? CpfCnpj { get; set; }
        public string? PolicyNumber { get; set; }
        public string? InsuranceType { get; set; }
        public string? Product { get; set; }
        public string? Vehicle { get; set; }
        public string? Version { get; set; }
        public string? FipeCode { get; set; }
        public string? Plate { get; set; }
        public string? BonusClass { get; set; }
        public string? Chassis { get; set; }
        public string? Group { get; set; }
        public string? ZeroKm { get; set; }
        public string? ZipCode { get; set; }
        public string? YearModel { get; set; }
        public string? UsageType { get; set; }
        public string? RiskCategory { get; set; }
        public string? PreviousInsurer { get; set; }
        public string? GasKit { get; set; }

        public string? MainDriverName { get; set; }
        public string? MainDriverCpf { get; set; }
        public string? MainDriverAge { get; set; }
        public string? MaritalStatus { get; set; }
        public string? ResidenceType { get; set; }
        public string? CoversDrivers18To25 { get; set; }

        public string? QuoteNumber { get; set; }
        public string? ValidUntil { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        public Dictionary<string, CoveragePlan> Coverages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public DeductibleData Deductibles { get; set; } = new();
        public PaymentData Payments { get; set; } = new();
        public BrokerData Broker { get; set; } = new();
    }
}