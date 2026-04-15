using System.Text.RegularExpressions;
using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers
{
    public class PortoFamilyQuoteParser : BaseQuoteParser
    {
        public override string InsurerKey => "porto-family";
        public override string InsurerName => "Porto Family";
        public override string LogoFileName => "porto.svg";

        private static readonly Dictionary<string, (string Name, string Logo)> BrandMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AZUL TRADICIONAL"] = ("Azul", "azul.svg"),
                ["ITAÚ TRADICIONAL"] = ("Itaú Seguros", "Itau-Seguros.svg"),
                ["AUTO MULHER"] = ("Porto Seguro", "porto.svg"),
                ["PORTO SEGURO"] = ("Porto Seguro", "porto.svg"),
                ["MITSUI SUMITOMO SEGUROS"] = ("Mitsui Sumitomo Seguros", "Mitsui-Seguros.svg")
            };

        public override bool CanParse(string rawText, string normalizedText, string? insurerHint = null)
        {
            return normalizedText.Contains("PORTO SEGURO", StringComparison.OrdinalIgnoreCase)
                   || normalizedText.Contains("AZUL TRADICIONAL", StringComparison.OrdinalIgnoreCase)
                   || normalizedText.Contains("ITAÚ TRADICIONAL", StringComparison.OrdinalIgnoreCase)
                   || normalizedText.Contains("MITSUI SUMITOMO SEGUROS", StringComparison.OrdinalIgnoreCase)
                   || HintMatches(insurerHint, "porto", "azul", "itau", "mitsui");
        }

        public override QuoteData Parse(string rawText, string normalizedText)
        {
            var data = CreateBaseQuote();

            var segment = MatchFirst(
                Extract(normalizedText, @"Versão Condições Gerais:\s*\w+\s*(.*?)\s*v1\.0"),
                Extract(normalizedText, @"Segmento\s*(.*?)\s*Sucursal"));

            if (!string.IsNullOrWhiteSpace(segment))
            {
                foreach (var item in BrandMap)
                {
                    if (segment.Contains(item.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        data.InsurerName = item.Value.Name;
                        data.InsurerLogoFileName = item.Value.Logo;
                        data.InsurerKey = Path.GetFileNameWithoutExtension(item.Value.Logo);
                        break;
                    }
                }
            }

            data.QuoteNumber = Extract(normalizedText, @"Orçamento:\s*([\d\-]+)");
            data.ValidUntil = Extract(normalizedText, @"Orçamento válido\s*(\d{2}/\d{2}/\d{4})");
            data.InsuredName = Extract(normalizedText, @"PROPONENTE\s*(.*?)\s*\d{2}/\d{2}/\d{4}\s*[\d\.\-]+");
            data.CpfCnpj = MatchFirst(
                Extract(normalizedText, @"PROPONENTE\s*.*?\s*(\d{3}\.\d{3}\.\d{3}\-\d{2})", RegexOptions.Singleline),
                Extract(normalizedText, @"CPF\/CNPJ:\s*([\d\.\-\/]+)"));

            data.InsuranceType = Extract(normalizedText, @"Tipo de Operação\s*(.*?)\s*Segmento");
            data.Product = segment;
            data.BonusClass = Extract(normalizedText, @"CLASSE\s*(\d+)");
            data.StartDate = Extract(normalizedText, @"Vigência Renovação\s*(\d{2}/\d{2}/\d{4})");
            data.EndDate = Extract(normalizedText, @"até\s*(\d{2}/\d{2}/\d{4})\s*\(\d+\s*dias\)");

            data.Vehicle = Extract(normalizedText, @"Veículo Ano Fabricação \/ Modelo\s*(.*?)\s*\d{4}\s*\/\s*\d{4}");
            data.YearModel = Extract(normalizedText, @"(\d{4}\s*\/\s*\d{4})");
            data.Plate = Extract(normalizedText, @"Placa Chassi\s*([A-Z0-9\-]+)\s*[A-Z0-9]+");
            data.Chassis = Extract(normalizedText, @"Placa Chassi\s*[A-Z0-9\-]+\s*([A-Z0-9]+)");
            data.FipeCode = Extract(normalizedText, @"Fipe Categoria Veículo 0 Km\s*(\d+)");
            data.ZeroKm = Extract(normalizedText, @"Fipe Categoria Veículo 0 Km\s*\d+\s*.*?\s*([SN])");
            data.GasKit = Extract(normalizedText, @"Câmbio Automático Kit-Gás Veículo de Pessoa com Deficiência\s*\w+\s*(\w+)\s*\w+");
            data.MainDriverName = Extract(normalizedText, @"Nome do principal Condutor:\s*(.*?)\s*CPF:");
            data.MainDriverCpf = Extract(normalizedText, @"Nome do principal Condutor:.*?CPF:\s*([\d\.\-\/]+)", RegexOptions.Singleline);
            data.ZipCode = Extract(normalizedText, @"CEP PERNOITE:?\s*(\d{5}\-?\d{3})");
            data.UsageType = Extract(normalizedText, @"Tipo do Uso:\s*(.*?)\s*Possui um ou mais");
            data.CoversDrivers18To25 = normalizedText.Contains("18 a 25", StringComparison.OrdinalIgnoreCase) ? "Não informado" : null;

            FillBasicBroker(
                data.Broker,
                name: Extract(normalizedText, @"CORRETOR\s*(.*?)\s*\d+\w?\s*\d{2}\-\d{4}\-\d{4}\s*[\w\.\-@]+"),
                susep: Extract(normalizedText, @"CORRETOR\s*.*?\s*(\d+\w?)\s*\d{2}\-\d{4}\-\d{4}", RegexOptions.Singleline),
                phone: Extract(normalizedText, @"CORRETOR\s*.*?\s*\d+\w?\s*(\d{2}\-\d{4}\-\d{4})", RegexOptions.Singleline),
                email: Extract(normalizedText, @"CORRETOR\s*.*?\d{2}\-\d{4}\-\d{4}\s*([\w\.\-@]+)", RegexOptions.Singleline));

            var vehicleDeductible = Extract(normalizedText, @"Casco\s*COMPREENSIVA\s*100\.00%\s*R\$\s*\d{1,3}(?:\.\d{3})*,\d{2}\s*0\.00%\s*0\.00%\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var propertyDamage = Extract(normalizedText, @"RCF\-V DANOS MATERIAIS\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var bodilyInjury = Extract(normalizedText, @"RCF\-V DANOS CORPORAIS\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var legal = Extract(normalizedText, @"CUSTOS DE DEFESA AUTO\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var app = Extract(normalizedText, @"ACIDENTES PESSOAIS PASSAGEIROS\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var moral = Extract(normalizedText, @"DANOS MORAIS E ESTÉTICOS\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var total = Extract(normalizedText, @"Prêmio Total:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var net = Extract(normalizedText, @"Prêmio Total Líquido:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var iof = Extract(normalizedText, @"IOF:\s*\+\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})");
            var tow = MatchFirst(
                Extract(normalizedText, @"REDE REFERENCIADA \- KM ILIMITADO"),
                Extract(normalizedText, @"ASSISTÊNCIA KM ILIMITADO"),
                Extract(normalizedText, @"ITAÚ KM ILIMITADO"),
                "Km ilimitado");
            var rental = Extract(normalizedText, @"CARRO RESERVA PORTE BÁSICO \- COMPLETO\s*(\d+)\s*DIAS");
            var glass = MatchFirst(
                Extract(normalizedText, @"VIDROS, RETROVISORES, LANTERNAS E FARÓIS \- REFERENCIADA"),
                "Contratada");

            AddSinglePlan(data, CreatePlan(
                name: "Plano Único",
                casco: "100% FIPE",
                propertyDamage: CleanMoney(propertyDamage),
                bodilyInjury: CleanMoney(bodilyInjury),
                moralDamage: CleanMoney(moral),
                appDeath: CleanMoney(app),
                appPermanentDisability: CleanMoney(app),
                legalDefenseCosts: CleanMoney(legal),
                assistance24h: tow,
                glassCoverage: glass,
                rentalCar: rental,
                rentalCarType: "Básico",
                towTruck: tow,
                netPrice: CleanMoney(net),
                iof: CleanMoney(iof),
                totalPrice: CleanMoney(total)
            ));

            data.Deductibles.VehicleDeductibleType = "50% da Obrigatória";
            data.Deductibles.VehicleDeductibleValue = CleanMoney(vehicleDeductible);
            data.Deductibles.Windshield = MatchFirst(
                CleanMoney(Extract(normalizedText, @"Vidros Para\-Brisa.*?R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")),
                CleanMoney(Extract(normalizedText, @"Vidros Para\-Brisa, Teto Solar ou Panorâmico:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")));
            data.Deductibles.RearWindow = MatchFirst(
                CleanMoney(Extract(normalizedText, @"Vidros Traseiros:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")));
            data.Deductibles.SideWindows = CleanMoney(Extract(normalizedText, @"Vidros Laterais\s*:?\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.StandardHeadlight = CleanMoney(Extract(normalizedText, @"Faróis Convencionais, Milha e Neblina:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.XenonLedHeadlight = MatchFirst(
                CleanMoney(Extract(normalizedText, @"Faróis de LED:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")),
                CleanMoney(Extract(normalizedText, @"Faróis de Xenon:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")));
            data.Deductibles.StandardTailLight = CleanMoney(Extract(normalizedText, @"Lanternas Convencionais:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.LedTailLight = CleanMoney(Extract(normalizedText, @"Lanternas de LED:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.SideMirror = CleanMoney(Extract(normalizedText, @"Retrovisores\s*:?\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));
            data.Deductibles.MinorRepairs = MatchFirst(
                CleanMoney(Extract(normalizedText, @"Reparo Rápido:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")),
                CleanMoney(Extract(normalizedText, @"Supermartelinho:\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})")));
            data.Deductibles.TireAndWheelProtection = CleanMoney(Extract(normalizedText, @"Roda, Pneu e Suspensão\s*:?\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})"));

            ParseInstallmentsFromPortoFamily(normalizedText, data.Payments);
            SetCommonVehicleFields(data, normalizedText);

            return data;
        }

        private static void ParseInstallmentsFromPortoFamily(string text, PaymentData payments)
        {
            var section = Extract(text, @"FORMAS DE PAGAMENTO(.*)", RegexOptions.Singleline);
            if (string.IsNullOrWhiteSpace(section))
                return;

            var matches = Regex.Matches(
                section,
                @"(\d{1,2})x\s*R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2})",
                RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (!match.Success || match.Groups.Count < 3)
                    continue;

                var installment = match.Groups[1].Value.PadLeft(2, '0');
                var value = CleanMoney(match.Groups[2].Value);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (!payments.CreditCard.ContainsKey(installment))
                    payments.CreditCard[installment] = value!;

                if (!payments.DebitAccount.ContainsKey(installment))
                    payments.DebitAccount[installment] = value!;

                if (!payments.Boleto.ContainsKey(installment))
                    payments.Boleto[installment] = value!;
            }
        }
    }
}