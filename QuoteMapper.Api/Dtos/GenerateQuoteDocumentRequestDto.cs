using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Dtos
{
    public class GenerateQuoteDocumentRequestDto
    {
        // Parsed and optionally edited data
        public QuoteData MappedData { get; set; } = new();

        // Selected plan to render in the final document
        public string SelectedPlan { get; set; } = "Master";

        // Header info
        public string IssueDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy");
        public string City { get; set; } = "Cachoeiro de Itapemirim - ES";

        // Optional manual override for FIPE value shown in the template
        // The Allianz PDF you uploaded shows FIPE code, but not the value in R$.
        public string? FipeValue { get; set; }

        // Optional footer overrides
        public string? BrokerContactName { get; set; } = "Leandro Rocha França";
        public string? BrokerContactPhone { get; set; } = "(28) 99917-5338 / (28) 3521-0638";
        public string? BrokerContactEmail { get; set; } = "leandro@fradecorretora.com.br";
    }
}