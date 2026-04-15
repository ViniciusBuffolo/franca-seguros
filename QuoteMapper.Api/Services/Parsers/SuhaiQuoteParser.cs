using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers
{
    public class SuhaiQuoteParser : BaseQuoteParser
    {
        public override string InsurerKey => "suhai";
        public override string InsurerName => "Suhai";
        public override string LogoFileName => "suhai-seguradora.svg";

        public override bool CanParse(string rawText, string normalizedText, string? insurerHint = null)
        {
            return normalizedText.Contains("SUHAI SEGURADORA", StringComparison.OrdinalIgnoreCase)
                   || normalizedText.Contains("Produto SUHAI", StringComparison.OrdinalIgnoreCase)
                   || HintMatches(insurerHint, "suhai");
        }

        public override QuoteData Parse(string rawText, string normalizedText)
        {
            var data = CreateBaseQuote();

            data.QuoteNumber = Extract(normalizedText, @"Cálculo N[ºo]:\s*([0-9\/]+)");
            data.ValidUntil = null;
            data.InsuredName = Extract(normalizedText, @"Nome\/Raz[aã]o Social\s*(.*?)\s*Tipo Pessoa");
            data.CpfCnpj = Extract(normalizedText, @"CPF\/CNPJ\s*([\d\.\-]+)");
            data.InsuranceType = Extract(normalizedText, @"Tipo de Seguro\s*(.*?)\s*Ramo");
            data.Product = Extract(normalizedText, @"Produto\s*(.*?)\s*Vers[aã]o");
            data.StartDate = Extract(normalizedText, @"Vig[eê]ncia Proposta:\s*das 24h de\s*(\d{2}/\d{2}/\d{4})");
            data.EndDate = Extract(normalizedText, @"às 24h de\s*(\d{2}/\d{2}/\d{4})");
            data.PolicyNumber = Extract(normalizedText, @"Ap[oó]lice Renovaç[aã]o:\s*([0-9]+)");

            data.Vehicle = Extract(normalizedText, @"Modelo do Ve[ií]culo\s*(.*?)\s*Classe B[oô]nus");
            data.FipeCode = Extract(normalizedText, @"C[oó]digo FIPE\s*([0-9\-]+)");
            data.YearModel = Extract(normalizedText, @"Ano Fabr\.\/Modelo\s*(\d{4}\/\d{4})");
            data.Plate = Extract(normalizedText, @"Placa\s*([A-Z0-9]+)");
            data.Chassis = Extract(normalizedText, @"Chassi\s*([A-Z0-9]+)");
            data.ZipCode = Extract(normalizedText, @"Reg\. Tarif\.\/CEP Pernoite\s*\d+\/(\d+)");
            data.ZeroKm = Extract(normalizedText, @"Zero KM\s*(.*?)\s*Placa Preta");
            data.UsageType = Extract(normalizedText, @"Utilização\s*(.*?)\s*Reg\. Tarif");
            data.BonusClass = Extract(normalizedText, @"Classe B[oô]nus\s*(\d+)");
            data.MainDriverName = data.InsuredName;
            data.MainDriverCpf = data.CpfCnpj;
            data.MaritalStatus = Extract(normalizedText, @"Estado Civil\s*(.*?)\s*Telefone");
            data.GasKit = "Não";

            data.Broker.Name = Extract(normalizedText, @"Nome\s*(FRADE.*?)\s*Telefone");
            data.Broker.Phone = Extract(normalizedText, @"Telefone\s*(\(\d{2}\)\s*\d{4}\-\d{4})");
            data.Broker.Email = Extract(normalizedText, @"E\-Mail\s*([\w\.\-@]+)");
            data.Broker.Susep = Extract(normalizedText, @"SUSEP\s*(\d+)");

            ParsePlans(data, normalizedText);
            ParsePayments(data, normalizedText);

            data.Deductibles.VehicleDeductibleType = "Reduzida";
            data.Deductibles.VehicleDeductibleValue = CleanMoney(Extract(normalizedText, @"Franquia Perdas Parciais.*?Reduzida:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})", System.Text.RegularExpressions.RegexOptions.Singleline));

            SetCommonVehicleFields(data, normalizedText);

            return data;
        }

        private static void ParsePlans(QuoteData data, string text)
        {
            var options = new[]
            {
                "Compreensiva",
                "Terceiros RCF",
                "Roubo \\+ Furto",
                "Roubo \\+ Furto \\+ PTCol"
            };

            foreach (var optionPattern in options)
            {
                var optionName = optionPattern
                    .Replace("\\", "")
                    .Replace("PTCol", "PT Colisão");

                var plan = new CoveragePlan
                {
                    Name = optionName
                };

                var block = Extract(
                    text,
                    optionPattern + @".*?LMI Prêmio(.*?)(?:Prêmio líquido|Franquia Perdas Parciais|Página \d+ de \d+)",
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (string.IsNullOrWhiteSpace(block))
                    continue;

                plan.Casco = FirstNotEmpty(
                    Extract(block, @"(100%\s*Fipe)"),
                    Extract(block, @"(100%\s*FIPE)"));

                plan.PropertyDamage = CleanMoney(Extract(block, @"RCF\s*\-\s*Danos Materiais\s*150\.000,00\s*([\d\.\,]+)"));
                plan.BodilyInjury = CleanMoney(Extract(block, @"RCF\s*\-\s*Danos Corporais\s*150\.000,00\s*([\d\.\,]+)"));
                plan.MoralDamage = CleanMoney(Extract(block, @"RCF\s*\-\s*Danos Morais\s*30\.000,00\s*([\d\.\,]+)"));
                plan.Assistance24h = Extract(block, @"Plano\s*\d+\s*\-\s*Guincho\s*\d+km");
                plan.TowTruck = plan.Assistance24h;
                plan.NetPrice = CleanMoney(Extract(block, @"Pr[êe]mio l[ií]quido\s*([\d\.\,]+)"));
                plan.TotalPrice = CleanMoney(Extract(block, @"Pr[êe]mio total, com IOF\s*([\d\.\,]+)"));

                AddOrUpdatePlan(data, plan);
            }

            if (data.Coverages.Count == 0)
            {
                var plan = new CoveragePlan
                {
                    Name = "Plano Único",
                    TotalPrice = CleanMoney(Extract(text, @"Pr[êe]mio total, com IOF\s*([\d\.\,]+)")),
                    NetPrice = CleanMoney(Extract(text, @"Pr[êe]mio l[ií]quido\s*([\d\.\,]+)"))
                };

                AddOrUpdatePlan(data, plan);
            }
        }

        private static void ParsePayments(QuoteData data, string text)
        {
            var block = Extract(text, @"OPÇÃO COMPREENSIVA.*?Parcelas Valor Parcela Valor Total Juros \(%\)(.*)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (string.IsNullOrWhiteSpace(block))
                return;

            var matches = System.Text.RegularExpressions.Regex.Matches(
                block,
                @"(\d{1,2})\s+(\d{1,3}(?:\.\d{3})*,\d{2})\s+(\d{1,3}(?:\.\d{3})*,\d{2})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (!match.Success || match.Groups.Count < 4)
                    continue;

                var installment = match.Groups[1].Value.PadLeft(2, '0');
                var parcel = CleanMoney(match.Groups[2].Value);

                if (string.IsNullOrWhiteSpace(parcel))
                    continue;

                data.Payments.Boleto[installment] = parcel!;
                data.Payments.DebitAccount[installment] = parcel!;
                data.Payments.CreditCard[installment] = parcel!;
            }
        }
    }
}