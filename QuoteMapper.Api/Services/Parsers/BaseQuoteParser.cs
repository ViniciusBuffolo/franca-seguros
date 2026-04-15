using System.Globalization;
using System.Text.RegularExpressions;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers
{
    public abstract class BaseQuoteParser : IQuoteParser
    {
        public abstract string InsurerKey { get; }
        public abstract string InsurerName { get; }
        public abstract string LogoFileName { get; }

        public abstract bool CanParse(string rawText, string normalizedText, string? insurerHint = null);
        public abstract QuoteData Parse(string rawText, string normalizedText);

        protected QuoteData CreateBaseQuote()
        {
            return new QuoteData
            {
                InsurerKey = InsurerKey,
                InsurerName = InsurerName,
                InsurerLogoFileName = LogoFileName,
                Coverages = new Dictionary<string, CoveragePlan>(StringComparer.OrdinalIgnoreCase),
                Deductibles = new DeductibleData(),
                Payments = new PaymentData(),
                Broker = new BrokerData()
            };
        }

        protected CoveragePlan CreatePlan(
            string name,
            string? casco = null,
            string? propertyDamage = null,
            string? bodilyInjury = null,
            string? moralDamage = null,
            string? appDeath = null,
            string? appPermanentDisability = null,
            string? legalDefenseCosts = null,
            string? assistance24h = null,
            string? glassCoverage = null,
            string? rentalCar = null,
            string? rentalCarType = null,
            string? towTruck = null,
            string? netPrice = null,
            string? iof = null,
            string? totalPrice = null)
        {
            return new CoveragePlan
            {
                Name = name,
                Casco = casco,
                PropertyDamage = propertyDamage,
                BodilyInjury = bodilyInjury,
                MoralDamage = moralDamage,
                AppDeath = appDeath,
                AppPermanentDisability = appPermanentDisability,
                LegalDefenseCosts = legalDefenseCosts,
                Assistance24h = assistance24h,
                GlassCoverage = glassCoverage,
                RentalCar = rentalCar,
                RentalCarType = rentalCarType,
                TowTruck = towTruck,
                NetPrice = netPrice,
                Iof = iof,
                TotalPrice = totalPrice
            };
        }

        protected static string NormalizeTextInternal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        protected static string? Extract(string text, string pattern, RegexOptions options = RegexOptions.None, int group = 1)
        {
            var match = Regex.Match(text, pattern, options | RegexOptions.IgnoreCase);
            if (!match.Success || match.Groups.Count <= group)
                return null;

            return Clean(match.Groups[group].Value);
        }

        protected static MatchCollection ExtractMany(string text, string pattern, RegexOptions options = RegexOptions.None)
        {
            return Regex.Matches(text, pattern, options | RegexOptions.IgnoreCase);
        }

        protected static IEnumerable<Match> ExtractManyEnumerable(string text, string pattern, RegexOptions options = RegexOptions.None)
        {
            return Regex.Matches(text, pattern, options | RegexOptions.IgnoreCase).Cast<Match>();
        }

        protected static string Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        protected static string? CleanMoney(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
            value = Regex.Replace(value, @"\s+", "");
            return value;
        }

        protected static string? MatchFirst(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        protected static string? FirstNotEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        protected static string? ExtractCurrency(string text, string labelPattern)
        {
            return CleanMoney(Extract(text, labelPattern + @"\s*R?\$?\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
        }

        protected static string? ExtractMoneyAfter(string text, string label)
        {
            return CleanMoney(Extract(text, Regex.Escape(label) + @"\s*:?[\sR\$+]*?(\d{1,3}(?:\.\d{3})*,\d{2})"));
        }

        protected static string? ExtractMoneyAfterLabel(string text, string label)
        {
            return CleanMoney(Extract(text, Regex.Escape(label) + @"\s*[:\-]?\s*R?\$?\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
        }

        protected static string? ExtractAnyMoney(string text, params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                var result = CleanMoney(Extract(text, pattern));
                if (!string.IsNullOrWhiteSpace(result))
                    return result;
            }

            return null;
        }

        protected static string? ExtractDate(string text, string labelPattern)
        {
            return Extract(text, labelPattern + @"\s*(\d{2}/\d{2}/\d{4})");
        }

        protected static string? ExtractDateByLabel(string text, string label)
        {
            return Extract(text, Regex.Escape(label) + @"\s*[:\-]?\s*(\d{2}/\d{2}/\d{4})");
        }

        protected static void EnsureMainPlan(QuoteData quote, string planName = "Plano Único")
        {
            if (quote.Coverages.Count == 0)
                quote.Coverages[planName] = new CoveragePlan { Name = planName };
        }

        protected static void AddSinglePlan(QuoteData quote, CoveragePlan plan)
        {
            quote.Coverages[plan.Name ?? "Plano Único"] = plan;
        }

        protected static void AddOrUpdatePlan(QuoteData quote, CoveragePlan plan)
        {
            var key = string.IsNullOrWhiteSpace(plan.Name) ? "Plano Único" : plan.Name!;
            quote.Coverages[key] = plan;
        }

        protected static void FillBasicBroker(
            BrokerData broker,
            string? name = null,
            string? email = null,
            string? phone = null,
            string? code = null,
            string? susep = null,
            string? branch = null)
        {
            broker.Name = name;
            broker.Email = email;
            broker.Phone = phone;
            broker.Code = code;
            broker.Susep = susep;
            broker.Branch = branch;
        }

        protected static void FillInstallments(
            PaymentData payments,
            params (string installment, string? value)[] boletoValues)
        {
            foreach (var item in boletoValues)
            {
                if (!string.IsNullOrWhiteSpace(item.value))
                {
                    payments.Boleto[item.installment] = item.value!;
                    payments.DebitAccount[item.installment] = item.value!;
                    payments.CreditCard[item.installment] = item.value!;
                }
            }
        }

        protected static void ParseInstallmentsFromSimpleTable(
            string normalizedText,
            PaymentData payments,
            string blockPattern,
            string labelPattern = @"(\d{1,2})x?\s*R?\$?\s*(\d{1,3}(?:\.\d{3})*,\d{2})")
        {
            var block = Extract(normalizedText, blockPattern, RegexOptions.Singleline);
            if (string.IsNullOrWhiteSpace(block))
                return;

            var matches = Regex.Matches(block, labelPattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (!match.Success || match.Groups.Count < 3)
                    continue;

                var installment = match.Groups[1].Value.PadLeft(2, '0');
                var value = CleanMoney(match.Groups[2].Value);

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                payments.Boleto[installment] = value;
                payments.DebitAccount[installment] = value;
                payments.CreditCard[installment] = value;
            }
        }

        protected static void SetCommonVehicleFields(QuoteData data, string normalizedText)
        {
            data.Plate ??= FirstNotEmpty(
                Extract(normalizedText, @"Placa[:\s]+([A-Z0-9\-]{7,8})"),
                Extract(normalizedText, @"\b([A-Z]{3}[0-9][A-Z0-9][0-9]{2})\b"));

            data.Chassis ??= Extract(normalizedText, @"Chassi[:\s]+([A-Z0-9]{10,25})");
            data.FipeCode ??= FirstNotEmpty(
                Extract(normalizedText, @"C[oó]d(?:igo)?\.?\s*FIPE[:\s]+([0-9\-]+)"),
                Extract(normalizedText, @"Tabela de Refer[eê]ncia:.*?\(([0-9\-]+)\)"));
        }

        protected static bool HintMatches(string? hint, params string[] expected)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return false;

            return expected.Any(x => string.Equals(hint, x, StringComparison.OrdinalIgnoreCase));
        }

        protected static decimal? ToDecimal(string? brValue)
        {
            if (string.IsNullOrWhiteSpace(brValue))
                return null;

            if (decimal.TryParse(
                    brValue.Replace(".", "").Replace(",", "."),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                return result;
            }

            return null;
        }
    }
}