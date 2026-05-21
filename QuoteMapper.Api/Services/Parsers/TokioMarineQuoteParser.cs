using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers
{
    public class TokioMarineQuoteParser : BaseQuoteParser
    {
        public override string InsurerKey => "tokio-marine";
        public override string InsurerName => "Tokio Marine";
        public override string LogoFileName => "tokio-marine-seguradora.svg";

        public override bool CanParse(string rawText, string normalizedText, string? insurerHint = null)
        {
            return normalizedText.Contains("TOKIO MARINE", StringComparison.OrdinalIgnoreCase)
                   || normalizedText.Contains("Cotação Tokio Marine", StringComparison.OrdinalIgnoreCase)
                   || HintMatches(insurerHint, "tokio", "tokio-marine");
        }

        public override QuoteData Parse(string rawText, string normalizedText)
        {
            var data = CreateBaseQuote();

            data.QuoteNumber = Extract(normalizedText, @"N[ºo]\s*Cotação:\s*([0-9]+)");
            data.ValidUntil = Extract(normalizedText, @"Validade cotação:\s*(\d{2}/\d{2}/\d{4})");
            data.StartDate = Extract(normalizedText, @"Vigência:\s*(\d{2}/\d{2}/\d{4})\s*a\s*\d{2}/\d{2}/\d{4}");
            data.EndDate = Extract(normalizedText, @"Vigência:\s*\d{2}/\d{2}/\d{4}\s*a\s*(\d{2}/\d{2}/\d{4})");

            data.InsuredName = Extract(normalizedText, @"Olá\s*(.*?)\,");
            data.CpfCnpj = Extract(normalizedText, @"Proponente\s*CPF\/CNPJ:\s*(.*?)\s*Principal Condutor");
            data.MainDriverName = Extract(normalizedText, @"Principal Condutor\s*(.*?)\s*Estado Civil");
            data.MaritalStatus = Extract(normalizedText, @"Estado Civil principal condutor\s*(.*?)\s*Nome Social");
            data.CoversDrivers18To25 = normalizedText.Contains("Não e estou ciente", StringComparison.OrdinalIgnoreCase) ? "Não" : null;

            data.Vehicle = Extract(
                normalizedText,
                @"CAOA CHERY\s+(TIGGO.*?)\s*Gasolina",
                System.Text.RegularExpressions.RegexOptions.Singleline
            );
            data.Vehicle ??= Extract(normalizedText, @"Veículo\s*(.*?)\s*Combust[ií]vel");
            data.FipeCode = Extract(normalizedText, @"Código FIPE\s*(\d{6,7}\-\d)");
            data.Plate = Extract(
                normalizedText,
                @"Seguro Novo\s+\d{2}/\d{2}/\d{4}\s*-\s*\d{2}/\d{2}/\d{4}\s+Sim\s+([A-Z]{3}\-?\d[A-Z0-9]\d{2})"
            );
            data.Chassis = Extract(
                normalizedText,
                @"Seguro Novo\s+\d{2}/\d{2}/\d{4}\s*-\s*\d{2}/\d{2}/\d{4}\s+Sim\s+[A-Z]{3}\-?\d[A-Z0-9]\d{2}\s+([A-Z0-9]{10,})"
            );
            data.YearModel = Extract(normalizedText, @"Ano modelo\s*(\d{4})");
            data.ZipCode = Extract(normalizedText, @"CEP de pernoite\s*(\d{5}\-?\d{3})");
            data.UsageType = Extract(normalizedText, @"Tipo de utilização\s*(.*?)\s*Dispositivo comodato");
            data.PreviousInsurer = Extract(normalizedText, @"Nome da Congênere\s*(.*?)\s*Número da apólice");
            data.PolicyNumber = Extract(normalizedText, @"Número da apólice\s*(.*?)\s*Fim de vigência");
            data.BonusClass = Extract(normalizedText, @"Classe de B[oô]nus\s*(\d+)");

            var plan = CreatePlan("Plano Único");
            plan.Casco = "100% FIPE";
            plan.PropertyDamage = CleanMoney(Extract(normalizedText, @"RCF\-V \- Danos Materiais\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            plan.BodilyInjury = CleanMoney(Extract(normalizedText, @"RCF\-V \- Danos Corporais\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            plan.MoralDamage = CleanMoney(Extract(normalizedText, @"RCF\-V \- Danos Morais\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            plan.AppDeath = CleanMoney(Extract(normalizedText, @"APP \- Morte \(por passageiro\)\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            plan.AppPermanentDisability = CleanMoney(Extract(normalizedText, @"APP \- Invalidez permanente \(por passageiro\)\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            plan.Assistance24h = Extract(normalizedText, @"Assistência 24 horas\s*(.*?)\s*R\$\s*\d");
            plan.GlassCoverage = normalizedText.Contains("Vidros", StringComparison.OrdinalIgnoreCase) ? "Contratada" : null;
            plan.RentalCar = Extract(normalizedText, @"Carro reserva\s*(\d+)\s*diárias");
            plan.RentalCarType = Extract(normalizedText, @"Carro reserva\s*\d+\s*diárias\s*(.*?)\s*Tipo de oficina", System.Text.RegularExpressions.RegexOptions.Singleline);
            plan.TowTruck = Extract(normalizedText, @"Km adicional reboque\s*(.*?)\s*1\s");
            plan.NetPrice = CleanMoney(Extract(normalizedText, @"Pr[eê]mio L[ií]quido total\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            plan.TotalPrice = CleanMoney(Extract(normalizedText, @"Auto\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})\s*[àa]\s*vista"));
            plan.Iof = Extract(normalizedText, @"IOF:\s*(\d{1,2},\d{2}%)");

            AddOrUpdatePlan(data, plan);

            data.Deductibles.VehicleDeductibleType = "50% da Básica";
            data.Deductibles.VehicleDeductibleValue = CleanMoney(Extract(normalizedText, @"50%\s*da Básica\s*\|\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.Windshield = CleanMoney(Extract(normalizedText, @"Parabrisa\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.RearWindow = CleanMoney(Extract(normalizedText, @"Vigia\/Traseiro\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.SideWindows = CleanMoney(Extract(normalizedText, @"Lateral\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.StandardHeadlight = CleanMoney(Extract(normalizedText, @"Farol Hal[oó]geno\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.XenonLedHeadlight = CleanMoney(Extract(normalizedText, @"Farol xenon\/led\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.StandardTailLight = CleanMoney(Extract(normalizedText, @"Lanterna Hal[oó]gena\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.LedTailLight = CleanMoney(Extract(normalizedText, @"Lanterna led\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.AuxiliaryHeadlight = CleanMoney(Extract(normalizedText, @"Farol auxiliar\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.SideMirror = CleanMoney(Extract(normalizedText, @"Retrovisor externo\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.Sunroof = CleanMoney(Extract(normalizedText, @"Teto Solar\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.MinorRepairs = CleanMoney(Extract(normalizedText, @"Lataria e pintura\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.TireAndWheelProtection = CleanMoney(Extract(normalizedText, @"Roda, Pneu e Suspensão\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));

            ParseInstallmentsFromSimpleTable(
                normalizedText,
                data.Payments,
                @"PAGAMENTOS(.*?)(?:Cotação Tokio Marine|$)"
            );

            SetCommonVehicleFields(data, normalizedText);

            return data;
        }
    }
}