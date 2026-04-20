using System.Text.RegularExpressions;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers
{
    public class AllianzQuoteParser : IQuoteParser
    {
        public string InsurerKey => "allianz";
        public string InsurerName => "Allianz";
        public string LogoFileName => "allianz.svg";

        private static readonly string[] FirstBlockPlans = { "Roubo e Furto", "Básico", "Ampliado" };
        private static readonly string[] SecondBlockPlans = { "Completo", "Master", "Exclusivo" };

        public bool CanParse(string rawText, string normalizedText, string? insurerHint = null)
        {
            if (!string.IsNullOrWhiteSpace(insurerHint) &&
                insurerHint.Equals("allianz", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalizedText.Contains("ALLIANZ", StringComparison.OrdinalIgnoreCase);
        }

        public QuoteData Parse(string rawText, string normalizedText)
        {
            var data = CreateBaseQuote();

            ParsePlans(data, rawText);

            return data;
        }

        private QuoteData CreateBaseQuote()
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

        private static void ParsePlans(QuoteData data, string text)
        {
            foreach (var plan in FirstBlockPlans.Concat(SecondBlockPlans))
            {
                data.Coverages[plan] = new CoveragePlan
                {
                    Name = plan
                };
            }

            var firstBlock = ExtractFirstCoverageBlock(text);
            var secondBlock = ExtractSecondCoverageBlock(text);

            ParseCoverageBlock(data, firstBlock, FirstBlockPlans);
            ParseCoverageBlock(data, secondBlock, SecondBlockPlans);
        }

        private static string ExtractFirstCoverageBlock(string text)
        {
            var start = text.IndexOf("Roubo e Furto", StringComparison.OrdinalIgnoreCase);
            var end = text.IndexOf("Completo", StringComparison.OrdinalIgnoreCase);

            if (start < 0)
                return text;

            if (end < 0)
                return text[start..];

            return text[start..end];
        }

        private static string ExtractSecondCoverageBlock(string text)
        {
            var start = text.IndexOf("Completo", StringComparison.OrdinalIgnoreCase);

            if (start < 0)
                return string.Empty;

            return text[start..];
        }

        private static void ParseCoverageBlock(
            QuoteData data,
            string block,
            string[] planNames)
        {
            if (string.IsNullOrWhiteSpace(block))
                return;

            ParseCoverageRowByPosition(
                data,
                block,
                "Danos Materiais",
                planNames,
                (plan, value) => plan.PropertyDamage = value
            );

            ParseCoverageRowByPosition(
                data,
                block,
                "Danos Corporais",
                planNames,
                (plan, value) => plan.BodilyInjury = value
            );

            ParseCoverageRowByPosition(
                data,
                block,
                "Danos Morais",
                planNames,
                (plan, value) => plan.MoralDamage = value
            );

            ParseCoverageRowByPosition(
                data,
                block,
                "APP",
                planNames,
                (plan, value) => plan.AppDeath = value,
                rowIndex: 0
            );

            ParseCoverageRowByPosition(
                data,
                block,
                "APP",
                planNames,
                (plan, value) => plan.AppPermanentDisability = value,
                rowIndex: 1
            );

            ParseGuincho(data, block, planNames);
            ParseCarroReserva(data, block, planNames);
        }

        private static void ParseCoverageRowByPosition(
            QuoteData data,
            string text,
            string label,
            string[] planNames,
            Action<CoveragePlan, string?> apply,
            int rowIndex = 0)
        {
            var lines = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var targetLines = lines
                .Where(l => l.Contains(label, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (targetLines.Count <= rowIndex)
                return;

            var line = targetLines[rowIndex];
            var numbers = ExtractAllMoneyValues(line);

            if (numbers.Count == 0)
                return;

            for (int i = 0; i < planNames.Length && i < numbers.Count; i++)
            {
                if (!data.Coverages.TryGetValue(planNames[i], out var plan))
                    continue;

                apply(plan, numbers[i]);
            }
        }

        private static List<string> ExtractAllMoneyValues(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            text = Regex.Replace(text, @"(\d)\s+(\d{2},\d{2})", "$1$2");
            text = Regex.Replace(text, @"(\d)\s+(\d{3}\.\d{3},\d{2})", "$1$2");
            text = Regex.Replace(text, @"\s+", " ");

            var matches = Regex.Matches(text, @"\d{1,3}(?:\.\d{3})*,\d{2}");

            return matches
                .Select(m => m.Value.Trim())
                .ToList();
        }

        private static void ParseGuincho(QuoteData data, string text, string[] planNames)
        {
            var line = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .FirstOrDefault(l => l.Contains("Guincho", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(line))
                return;

            var values = Regex.Matches(line, @"Km Livre|\d+\s*km", RegexOptions.IgnoreCase)
                .Select(m => m.Value.Trim())
                .ToList();

            for (int i = 0; i < planNames.Length && i < values.Count; i++)
            {
                if (data.Coverages.TryGetValue(planNames[i], out var plan))
                {
                    plan.TowTruck = values[i];
                }
            }
        }

        private static void ParseCarroReserva(QuoteData data, string text, string[] planNames)
        {
            var line = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .FirstOrDefault(l => l.Contains("Carro Reserva", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(line))
                return;

            var values = Regex.Matches(line, @"\d+\s*Dias", RegexOptions.IgnoreCase)
                .Select(m => m.Value.Trim())
                .ToList();

            for (int i = 0; i < planNames.Length && i < values.Count; i++)
            {
                if (data.Coverages.TryGetValue(planNames[i], out var plan))
                {
                    plan.RentalCar = values[i];
                    plan.RentalCarType = "Básico";
                }
            }
        }
    }
}