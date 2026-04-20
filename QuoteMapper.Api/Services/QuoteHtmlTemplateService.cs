using System.Net;
using QuoteMapper.Api.Dtos;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services
{
    public class QuoteHtmlTemplateService : IQuoteHtmlTemplateService
    {
        private const string DefaultFeaturedLogo = "/logos/featured/frade.svg";
        private const string FrancaLogo = "/logos/featured/franca.svg";

        public string RenderQuoteHtml(GenerateQuoteDocumentRequestDto request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.MappedData == null)
                throw new ArgumentException("MappedData is required.", nameof(request));

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Templates",
                "quote-template.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException("HTML template not found.", templatePath);

            var html = File.ReadAllText(templatePath);

            var plan = ResolvePlan(request);

            var dynamicInsurerLogo = BuildInsurerLogoUrl(request.MappedData.InsurerLogoFileName);
            var corretoraLogoUrl = DefaultFeaturedLogo;
            var francaLogoUrl = FrancaLogo;

            html = Replace(html, "LOGO_ALLIANZ_URL", dynamicInsurerLogo);
            html = Replace(html, "LOGO_CORRETORA_URL", corretoraLogoUrl);
            html = Replace(html, "LOGO_FRANCA_URL", francaLogoUrl);

            html = Replace(html, "ISSUE_DATE", request.IssueDate);
            html = Replace(html, "CITY", request.City);

            html = Replace(html, "PROPONENTE", request.MappedData.InsuredName);
            html = Replace(html, "FIPE_VALUE", request.FipeValue ?? string.Empty);
            html = Replace(html, "VEHICLE", request.MappedData.Vehicle);
            html = Replace(html, "PLATE", request.MappedData.Plate);

            html = Replace(html, "DANOS_MATERIAIS", ToCurrency(plan?.PropertyDamage));
            html = Replace(html, "DANOS_CORPORAIS", ToCurrency(plan?.BodilyInjury));
            html = Replace(html, "DANOS_MORAIS", ToCurrency(plan?.MoralDamage));
            html = Replace(html, "APP_MORTE", ToCurrency(plan?.AppDeath));
            html = Replace(html, "APP_INVALIDEZ", ToCurrency(plan?.AppPermanentDisability));

            html = Replace(html, "ASSISTENCIA_GUINCHO", plan?.TowTruck ?? string.Empty);
            html = Replace(html, "CARRO_RESERVA", BuildRentalText(plan));

            html = Replace(html, "FRANQUIA_PARABRISA", ToCurrency(request.MappedData.Deductibles?.Windshield));
            html = Replace(html, "FRANQUIA_VIDRO_LATERAL", ToCurrency(request.MappedData.Deductibles?.SideWindows));
            html = Replace(html, "FRANQUIA_FAROL_CONVENCIONAL", ToCurrency(request.MappedData.Deductibles?.StandardHeadlight));
            html = Replace(html, "FRANQUIA_LANTERNA_CONVENCIONAL", ToCurrency(request.MappedData.Deductibles?.StandardTailLight));
            html = Replace(html, "FRANQUIA_FAROL_XENON_LED", ToCurrency(request.MappedData.Deductibles?.XenonLedHeadlight));
            html = Replace(html, "FRANQUIA_LANTERNA_LED", ToCurrency(request.MappedData.Deductibles?.LedTailLight));
            html = Replace(html, "FRANQUIA_RETROVISOR", ToCurrency(request.MappedData.Deductibles?.SideMirror));
            html = Replace(html, "FRANQUIA_LANTERNA_AUXILIAR", ToCurrency(request.MappedData.Deductibles?.AuxiliaryTailLight));
            html = Replace(html, "FRANQUIA_PNEU_RODA", ToCurrency(request.MappedData.Deductibles?.TireAndWheelProtection));
            html = Replace(html, "FRANQUIA_PEQUENOS_REPAROS", ToCurrency(request.MappedData.Deductibles?.MinorRepairs));

            html = Replace(html, "FRANQUIA_VEICULO", ToCurrency(request.MappedData.Deductibles?.VehicleDeductibleValue));
            html = Replace(html, "TIPO_FRANQUIA_VEICULO", request.MappedData.Deductibles?.VehicleDeductibleType);

            html = Replace(html, "CONDUTOR_PRINCIPAL", request.MappedData.MainDriverName);
            html = Replace(html, "USO_VEICULO", request.MappedData.UsageType);
            html = Replace(html, "ESTADO_CIVIL", request.MappedData.MaritalStatus);
            html = Replace(html, "CEP_PERNOITE", request.MappedData.ZipCode);
            html = Replace(html, "RESIDE_EM", request.MappedData.ResidenceType);
            html = Replace(html, "CONDUTORES_18_25", request.MappedData.CoversDrivers18To25);

            html = Replace(html, "BROKER_CONTACT_NAME", request.BrokerContactName ?? request.MappedData.Broker?.Name);
            html = Replace(html, "BROKER_CONTACT_PHONE", request.BrokerContactPhone ?? request.MappedData.Broker?.Phone);
            html = Replace(html, "BROKER_CONTACT_EMAIL", request.BrokerContactEmail ?? request.MappedData.Broker?.Email);

            html = ReplaceRaw(html, "PAYMENT_ROWS", BuildPaymentRows(request.MappedData.Payments));

            return html;
        }

        private static string BuildInsurerLogoUrl(string? logoFileName)
        {
            if (string.IsNullOrWhiteSpace(logoFileName))
                return DefaultFeaturedLogo;

            var normalized = logoFileName.Trim().ToLowerInvariant();
            return $"/logos/insurers/{normalized}";
        }

        private static CoveragePlan? ResolvePlan(GenerateQuoteDocumentRequestDto request)
        {
            if (request.MappedData?.Coverages == null || request.MappedData.Coverages.Count == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(request.SelectedPlan) &&
                request.MappedData.Coverages.TryGetValue(request.SelectedPlan, out var selected))
            {
                return selected;
            }

            if (request.MappedData.Coverages.TryGetValue("Básico", out var basic))
                return basic;

            if (request.MappedData.Coverages.TryGetValue("Master", out var master))
                return master;

            if (request.MappedData.Coverages.TryGetValue("Plano Único", out var single))
                return single;

            return request.MappedData.Coverages.Values.FirstOrDefault();
        }

        private static string BuildRentalText(CoveragePlan? plan)
        {
            if (plan == null)
                return string.Empty;

            return $"{plan.RentalCar} {plan.RentalCarType}".Trim();
        }

        private static string BuildPaymentRows(PaymentData? payments)
        {
            if (payments == null)
                return string.Empty;

            var boleto = payments.Boleto ?? new Dictionary<string, string>();
            var creditCard = payments.CreditCard ?? new Dictionary<string, string>();
            var debitAccount = payments.DebitAccount ?? new Dictionary<string, string>();

            var allInstallments = Enumerable.Range(1, 12)
                .Select(x => x.ToString("00"))
                .ToList();

            var rows = new List<string>();

            foreach (var installment in allInstallments)
            {
                boleto.TryGetValue(installment, out var boletoValue);
                creditCard.TryGetValue(installment, out var creditValue);
                debitAccount.TryGetValue(installment, out var debitValue);

                if (string.IsNullOrWhiteSpace(boletoValue) &&
                    string.IsNullOrWhiteSpace(creditValue) &&
                    string.IsNullOrWhiteSpace(debitValue))
                {
                    continue;
                }

                var label = installment == "01" ? "À vista / 1x" : installment + "x";

                rows.Add($@"
<tr>
    <td class=""payment-label"">{Encode(label)}</td>
    <td>{Encode(ToCurrency(boletoValue))}</td>
    <td>{Encode(ToCurrency(creditValue))}</td>
    <td>{Encode(ToCurrency(debitValue))}</td>
</tr>");
            }

            return string.Join(Environment.NewLine, rows);
        }

        private static string ToCurrency(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (value.TrimStart().StartsWith("R$"))
                return value.Trim();

            return $"R$ {value.Trim()}";
        }

        private static string Replace(string html, string token, string? value)
        {
            return html.Replace($"{{{{{token}}}}}", Encode(value));
        }

        private static string ReplaceRaw(string html, string token, string? value)
        {
            return html.Replace($"{{{{{token}}}}}", value ?? string.Empty);
        }

        private static string Encode(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}