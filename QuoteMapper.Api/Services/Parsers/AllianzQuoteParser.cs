using System.Text.RegularExpressions;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers
{
    public class AllianzQuoteParser : BaseQuoteParser
    {
        public override string InsurerKey => "allianz";
        public override string InsurerName => "Allianz";
        public override string LogoFileName => "allianz.svg";

        private static readonly string[] FirstBlockPlans = { "Roubo e Furto", "Básico", "Ampliado" };
        private static readonly string[] SecondBlockPlans = { "Completo", "Master", "Exclusivo" };

        // The DOCX template matches the "Master" plan values.
        private const string PreferredTemplatePlan = "Master";

        public override bool CanParse(string rawText, string normalizedText, string? insurerHint = null)
        {
            return normalizedText.Contains("ALLIANZ", StringComparison.OrdinalIgnoreCase)
                   || normalizedText.Contains("Agradecemos por cotar com a Allianz", StringComparison.OrdinalIgnoreCase)
                   || HintMatches(insurerHint, "allianz");
        }

        public override QuoteData Parse(string rawText, string normalizedText)
        {
            var data = CreateBaseQuote();

            ParseBaseFields(data, normalizedText);
            ParseBroker(data, normalizedText);
            ParsePlans(data, normalizedText);
            ParseDeductibles(data, normalizedText);
            ParsePayments(data, normalizedText);

            SetCommonVehicleFields(data, normalizedText);
            EnsureMainPlan(data);

            return data;
        }

        private static void ParseBaseFields(QuoteData data, string text)
        {
            data.InsuredName = Extract(text, @"Nome:\s*(.*?)\s*CPF\/CNPJ:");
            data.CpfCnpj = Extract(text, @"CPF\/CNPJ:\s*([\d\.\-\/]+)");
            data.PolicyNumber = Extract(text, @"N[ºo]\s*da Apólice:\s*(.*?)\s*Tipo de Seguro:");
            data.InsuranceType = Extract(text, @"Tipo de Seguro:\s*(.*?)\s*Produto:");
            data.Product = Extract(text, @"Produto:\s*(.*?)\s*Veículo:");
            data.Vehicle = Extract(text, @"Veículo:\s*(.*?)\s*Versão:");
            data.Version = Extract(text, @"Versão:\s*(.*?)\s*C[oó]d\.?\s*FIPE:");
            data.FipeCode = Extract(text, @"C[oó]d\.?\s*FIPE:\s*(.*?)\s*Condições Gerais:");
            data.Plate = Extract(text, @"Placa:\s*(.*?)\s*Classe B[oô]nus:");
            data.BonusClass = Extract(text, @"Classe B[oô]nus:\s*(.*?)\s*Chassi:");
            data.Chassis = Extract(text, @"Chassi:\s*(.*?)\s*Grupo:");
            data.Group = Extract(text, @"Grupo:\s*(.*?)\s*Zero Km:");
            data.ZeroKm = Extract(text, @"Zero Km:\s*(.*?)\s*CEP Pernoite:");
            data.ZipCode = Extract(text, @"CEP Pernoite:\s*(.*?)\s*Ano\/Modelo:");
            data.YearModel = Extract(text, @"Ano\/Modelo:\s*(.*?)\s*Finalidade de Uso:");
            data.UsageType = Extract(text, @"Finalidade de Uso:\s*(.*?)\s*Categoria de Risco:");
            data.RiskCategory = Extract(text, @"Categoria de Risco:\s*(.*?)\s*Seguradora Anterior:");
            data.PreviousInsurer = Extract(text, @"Seguradora Anterior:\s*(.*?)\s*Kit gás:");
            data.GasKit = Extract(text, @"Kit gás:\s*(.*?)\s*INFORMAÇÕES DO CONDUTOR PRINCIPAL");

            data.MainDriverName = Extract(text, @"INFORMAÇÕES DO CONDUTOR PRINCIPAL.*?Nome:\s*(.*?)\s*CPF:", RegexOptions.Singleline);
            data.MainDriverCpf = Extract(text, @"INFORMAÇÕES DO CONDUTOR PRINCIPAL.*?CPF:\s*([\d\.\-\/]+)", RegexOptions.Singleline);
            data.MainDriverAge = Extract(text, @"Idade:\s*(.*?)\s*Estado Civil:");
            data.MaritalStatus = Extract(text, @"Estado Civil:\s*(.*?)\s*Deseja ampliar");
            data.ResidenceType = Extract(text, @"O principal condutor reside em:\s*(.*?)\s*DETALHES DAS OFERTAS");
            data.CoversDrivers18To25 = text.Contains("não haverá cobertura para condutores entre 18 a 25 anos", StringComparison.OrdinalIgnoreCase)
                ? "Não"
                : "Sim";

            data.QuoteNumber = Extract(text, @"N[ºo]\s*Cotação:\s*(\d+)");
            data.ValidUntil = Extract(text, @"condições desta cotação até\s*(\d{2}\/\d{2}\/\d{4})");
            data.StartDate = Extract(text, @"Vigência:\s*das 24H de\s*(\d{2}\/\d{2}\/\d{4})");
            data.EndDate = Extract(text, @"às 24H de\s*(\d{2}\/\d{2}\/\d{4})");
        }

        private static void ParseBroker(QuoteData data, string text)
        {
            data.Broker = new BrokerData
            {
                Name = Extract(text, @"SEU CORRETOR\s*(.*?)\s*E-mail:"),
                Email = Extract(text, @"E-mail:\s*(.*?)\s*Telefone:"),
                Phone = Extract(text, @"Telefone:\s*(.*?)\s*Código:"),
                Code = Extract(text, @"Código:\s*(.*?)\s*SUSEP"),
                Susep = Extract(text, @"SUSEP\s*N[ºo]?:\s*(.*?)\s*F(?:i|I)lial:"),
                Branch = Extract(text, @"F(?:i|I)lial:\s*(.*?)\s*DESCONTO EM OUTROS PRODUTOS")
            };
        }

        private static void ParsePlans(QuoteData data, string text)
        {
            foreach (var plan in FirstBlockPlans.Concat(SecondBlockPlans))
            {
                data.Coverages[plan] = new CoveragePlan { Name = plan };
            }

            ApplyRowAcrossPlans(
                data,
                text,
                @"Casco\s*\-\s*B[aá]sica Compreensiva\s*\-\s*Colis[aã]o,\s*Inc[eê]ndio,\s*Roubo e Furto\s*(100%\s*FIPE)\s*([\d\.\,]+)\s*(100%\s*FIPE)\s*([\d\.\,]+)\s*(100%\s*FIPE)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) =>
                {
                    plan.Casco = Clean(limit);
                });

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Danos Materiais\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) => plan.PropertyDamage = CleanMoney(limit));

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Danos Corporais\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) => plan.BodilyInjury = CleanMoney(limit));

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Danos Morais(?: e Est[eé]ticos)?\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) => plan.MoralDamage = CleanMoney(limit));

            ApplyRowAcrossPlans(
                data,
                text,
                @"APP\*+\s*\-\s*Morte\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) => plan.AppDeath = CleanMoney(limit));

            ApplyRowAcrossPlans(
                data,
                text,
                @"APP\*+\s*\-\s*Invalidez Permanente\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) => plan.AppPermanentDisability = CleanMoney(limit));

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Gastos com Defesa\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, limit, premium) => plan.LegalDefenseCosts = CleanMoney(limit));

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Assist[eê]ncia 24 hs\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, value, premium) => plan.Assistance24h = Clean(value));

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Vidros,\s*lanternas,\s*far[oó]is e\s*retrovisores\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, value, premium) => plan.GlassCoverage = Clean(value));

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Carro Reserva\s*(\d+\s*Dias)\s*([\d\.\,]+)\s*(\d+\s*Dias)\s*([\d\.\,]+)\s*(\d+\s*Dias)\s*([\d\.\,]+)",
                FirstBlockPlans,
                (plan, value, premium) => plan.RentalCar = Clean(value));

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Tipo de Carro Reserva\s*(B[aá]sico|Intermedi[aá]rio|Completo)\s*\-\s*(?:[\d\.\,]+\s*)?(B[aá]sico|Intermedi[aá]rio|Completo)\s*\-\s*(?:[\d\.\,]+\s*)?(B[aá]sico|Intermedi[aá]rio|Completo)\s*\-",
                FirstBlockPlans,
                (plan, value, premium) => plan.RentalCarType = Clean(value),
                hasPremiums: false);

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Guincho\s*(Km Livre|\d+\s*km)\s*\-\s*(Km Livre|\d+\s*km)\s*\-\s*(Km Livre|\d+\s*km)\s*\-",
                FirstBlockPlans,
                (plan, value, premium) =>
                {
                    plan.TowTruck = Clean(value);
                    if (string.IsNullOrWhiteSpace(plan.Assistance24h))
                        plan.Assistance24h = Clean(value);
                },
                hasPremiums: false);

            ApplySummaryRowAcrossPlans(data, text, @"Preço Líquido\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)", FirstBlockPlans, (plan, v) => plan.NetPrice = CleanMoney(v));
            ApplySummaryRowAcrossPlans(data, text, @"IOF\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)", FirstBlockPlans, (plan, v) => plan.Iof = CleanMoney(v));
            ApplySummaryRowAcrossPlans(data, text, @"Preço Total\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)", FirstBlockPlans, (plan, v) => plan.TotalPrice = CleanMoney(v));

            ApplyRowAcrossPlans(
                data,
                text,
                @"Casco\s*\-\s*B[aá]sica Compreensiva\s*\-\s*Colis[aã]o,\s*Inc[eê]ndio,\s*Roubo e Furto\s*(100%\s*FIPE)\s*([\d\.\,]+)\s*(100%\s*FIPE)\s*([\d\.\,]+)\s*(100%\s*FIPE)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.Casco = Clean(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Danos Materiais\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.PropertyDamage = CleanMoney(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Danos Corporais\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.BodilyInjury = CleanMoney(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Danos Morais(?: e Est[eé]ticos)?\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.MoralDamage = CleanMoney(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyRowAcrossPlans(
                data,
                text,
                @"APP\*+\s*\-\s*Morte\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.AppDeath = CleanMoney(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyRowAcrossPlans(
                data,
                text,
                @"APP\*+\s*\-\s*Invalidez Permanente\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.AppPermanentDisability = CleanMoney(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyRowAcrossPlans(
                data,
                text,
                @"RCF\*+\s*\-\s*Gastos com Defesa\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, limit, premium) => plan.LegalDefenseCosts = CleanMoney(limit),
                startAfter: "Completo Master Exclusivo");

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Assist[eê]ncia 24 hs\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, value, premium) => plan.Assistance24h = Clean(value),
                startAfter: "Completo Master Exclusivo");

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Vidros,\s*lanternas,\s*far[oó]is e\s*retrovisores\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)\s*(Plano\s*\d+)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, value, premium) => plan.GlassCoverage = Clean(value),
                startAfter: "Completo Master Exclusivo");

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Carro Reserva\s*(\d+\s*Dias)\s*([\d\.\,]+)\s*(\d+\s*Dias)\s*([\d\.\,]+)\s*(\d+\s*Dias)\s*([\d\.\,]+)",
                SecondBlockPlans,
                (plan, value, premium) => plan.RentalCar = Clean(value),
                startAfter: "Completo Master Exclusivo");

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Tipo de Carro Reserva\s*(B[aá]sico|Intermedi[aá]rio|Completo)\s*\-\s*(?:[\d\.\,]+\s*)?(B[aá]sico|Intermedi[aá]rio|Completo)\s*\-\s*(?:[\d\.\,]+\s*)?(B[aá]sico|Intermedi[aá]rio|Completo)\s*\-",
                SecondBlockPlans,
                (plan, value, premium) => plan.RentalCarType = Clean(value),
                hasPremiums: false,
                startAfter: "Completo Master Exclusivo");

            ApplyStringRowAcrossPlans(
                data,
                text,
                @"Guincho\s*(Km Livre|\d+\s*km)\s*\-\s*(Km Livre|\d+\s*km)\s*\-\s*(Km Livre|\d+\s*km)\s*\-",
                SecondBlockPlans,
                (plan, value, premium) =>
                {
                    plan.TowTruck = Clean(value);
                    if (string.IsNullOrWhiteSpace(plan.Assistance24h))
                        plan.Assistance24h = Clean(value);
                },
                hasPremiums: false,
                startAfter: "Completo Master Exclusivo");

            ApplySummaryRowAcrossPlans(data, text, @"Preço Líquido\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)", SecondBlockPlans, (plan, v) => plan.NetPrice = CleanMoney(v), "Completo Master Exclusivo");
            ApplySummaryRowAcrossPlans(data, text, @"IOF\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)", SecondBlockPlans, (plan, v) => plan.Iof = CleanMoney(v), "Completo Master Exclusivo");
            ApplySummaryRowAcrossPlans(data, text, @"Preço Total\s*([\d\.\,]+)\s*([\d\.\,]+)\s*([\d\.\,]+)", SecondBlockPlans, (plan, v) => plan.TotalPrice = CleanMoney(v), "Completo Master Exclusivo");

            foreach (var plan in data.Coverages.Values)
            {
                plan.Casco ??= "100% FIPE";
                plan.RentalCarType ??= "Básico";
            }
        }

        private static void ParseDeductibles(QuoteData data, string text)
        {
            data.Deductibles.VehicleDeductibleType = MatchFirst(
                Extract(text, @"(50%\s*da\s*Normal)"),
                Extract(text, @"(Franquia reduzida)"));

            data.Deductibles.VehicleDeductibleValue = CleanMoney(Extract(text, @"50%\s*da\s*Normal\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.Windshield = CleanMoney(Extract(text, @"Parabrisa\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.SideWindows = CleanMoney(Extract(text, @"Vidros Laterais\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.RearWindow = CleanMoney(Extract(text, @"Vidro Traseiro\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.StandardHeadlight = CleanMoney(Extract(text, @"Farol Convencional\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.XenonLedHeadlight = CleanMoney(Extract(text, @"Farol X[eê]non e Led\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.StandardTailLight = CleanMoney(Extract(text, @"Lanterna Convencional\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.LedTailLight = CleanMoney(Extract(text, @"Lanternas Led\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.AuxiliaryHeadlight = CleanMoney(Extract(text, @"Farol Auxiliar\/Milha\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.AuxiliaryTailLight = CleanMoney(Extract(text, @"Lanterna Auxiliar\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.SideMirror = CleanMoney(Extract(text, @"Retrovisores\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.Sunroof = CleanMoney(Extract(text, @"Teto Solar\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.MinorRepairs = CleanMoney(Extract(text, @"Pequenos Reparos\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.TireAndWheelProtection = CleanMoney(Extract(text, @"Proteção Pneu e Roda\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
        }

        private static void ParsePayments(QuoteData data, string text)
        {
            var paymentSection = Extract(text, @"OUTRAS FORMAS DE PAGAMENTO(.*)", RegexOptions.Singleline)
                                 ?? Extract(text, @"FORMAS DE PAGAMENTO(.*)", RegexOptions.Singleline);

            if (string.IsNullOrWhiteSpace(paymentSection))
                return;

            ParsePaymentMethodBlock(
                paymentSection,
                @"Boleto Banc[aá]rio\s+Oferta\s*\(R\$\)(.*?)(?=D[eé]bito em Conta\s+Oferta\s*\(R\$\)|Cart[aã]o de Cr[eé]dito\s+Oferta\s*\(R\$\)|$)",
                data.Payments.Boleto,
                PreferredTemplatePlan);

            ParsePaymentMethodBlock(
                paymentSection,
                @"D[eé]bito em Conta\s+Oferta\s*\(R\$\)(.*?)(?=Cart[aã]o de Cr[eé]dito\s+Oferta\s*\(R\$\)|$)",
                data.Payments.DebitAccount,
                PreferredTemplatePlan);

            ParsePaymentMethodBlock(
                paymentSection,
                @"Cart[aã]o de Cr[eé]dito\s+Oferta\s*\(R\$\)(.*)",
                data.Payments.CreditCard,
                PreferredTemplatePlan);
        }

        private static void ParsePaymentMethodBlock(
            string paymentSection,
            string blockPattern,
            IDictionary<string, string> target,
            string preferredPlanName)
        {
            var block = Extract(paymentSection, blockPattern, RegexOptions.Singleline);

            if (string.IsNullOrWhiteSpace(block))
                return;

            var rowMatches = Regex.Matches(
                block,
                @"\b(01|02|03|04|05|06|07|08|09|10)\s+(?:sem juros|[\d\.\,]+%)\s+([\d\.\,]+)\s+([\d\.\,]+)\s+([\d\.\,]+)\s+([\d\.\,]+)\s+([\d\.\,]+)\s+([\d\.\,]+)",
                RegexOptions.IgnoreCase);

            foreach (Match row in rowMatches)
            {
                if (!row.Success || row.Groups.Count < 8)
                    continue;

                var installment = row.Groups[1].Value.PadLeft(2, '0');

                var valueByPlan = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Roubo e Furto"] = CleanMoney(row.Groups[2].Value),
                    ["Básico"] = CleanMoney(row.Groups[3].Value),
                    ["Ampliado"] = CleanMoney(row.Groups[4].Value),
                    ["Completo"] = CleanMoney(row.Groups[5].Value),
                    ["Master"] = CleanMoney(row.Groups[6].Value),
                    ["Exclusivo"] = CleanMoney(row.Groups[7].Value)
                };

                var chosenValue = valueByPlan.TryGetValue(preferredPlanName, out var selected)
                    ? selected
                    : valueByPlan["Master"];

                if (!string.IsNullOrWhiteSpace(chosenValue))
                    target[installment] = chosenValue!;
            }
        }

        private static void ApplyRowAcrossPlans(
            QuoteData data,
            string text,
            string pattern,
            string[] planNames,
            Action<CoveragePlan, string, string> apply,
            string? startAfter = null)
        {
            var workingText = SliceAfter(text, startAfter);
            var match = Regex.Match(workingText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
                return;

            if (match.Groups.Count < 7)
                return;

            for (var i = 0; i < planNames.Length; i++)
            {
                var plan = data.Coverages[planNames[i]];
                var limit = match.Groups[1 + (i * 2)].Value;
                var premium = match.Groups[2 + (i * 2)].Value;
                apply(plan, limit, premium);
            }
        }

        private static void ApplyStringRowAcrossPlans(
            QuoteData data,
            string text,
            string pattern,
            string[] planNames,
            Action<CoveragePlan, string, string> apply,
            bool hasPremiums = true,
            string? startAfter = null)
        {
            var workingText = SliceAfter(text, startAfter);
            var match = Regex.Match(workingText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
                return;

            if (hasPremiums)
            {
                if (match.Groups.Count < 7)
                    return;

                for (var i = 0; i < planNames.Length; i++)
                {
                    var plan = data.Coverages[planNames[i]];
                    var value = match.Groups[1 + (i * 2)].Value;
                    var premium = match.Groups[2 + (i * 2)].Value;
                    apply(plan, value, premium);
                }
            }
            else
            {
                if (match.Groups.Count < 4)
                    return;

                for (var i = 0; i < planNames.Length; i++)
                {
                    var plan = data.Coverages[planNames[i]];
                    var value = match.Groups[1 + i].Value;
                    apply(plan, value, string.Empty);
                }
            }
        }

        private static void ApplySummaryRowAcrossPlans(
            QuoteData data,
            string text,
            string pattern,
            string[] planNames,
            Action<CoveragePlan, string> apply,
            string? startAfter = null)
        {
            var workingText = SliceAfter(text, startAfter);
            var match = Regex.Match(workingText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
                return;

            if (match.Groups.Count < 4)
                return;

            for (var i = 0; i < planNames.Length; i++)
            {
                var plan = data.Coverages[planNames[i]];
                var value = match.Groups[1 + i].Value;
                apply(plan, value);
            }
        }

        private static string SliceAfter(string text, string? startAfter)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(startAfter))
                return text;

            var index = text.IndexOf(startAfter, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return text;

            return text[(index + startAfter.Length)..];
        }
    }
}