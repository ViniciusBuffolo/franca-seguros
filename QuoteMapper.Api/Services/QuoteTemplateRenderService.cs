using System.Text;
using System.Text.Encodings.Web;
using MyPdfApi.Models;

namespace MyPdfApi.Services;

public sealed class QuoteTemplateRenderService : IQuoteTemplateRenderService
{
    private readonly IWebHostEnvironment _environment;

    public QuoteTemplateRenderService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> RenderAsync(QuoteTemplateData data, CancellationToken cancellationToken = default)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "quote-template.html");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template file not found.", templatePath);

        var html = await File.ReadAllTextAsync(templatePath, cancellationToken);

        var replacements = new Dictionary<string, string>
        {
            ["{{ISSUE_DATE}}"] = Encode(data.IssueDate),
            ["{{CITY}}"] = Encode(data.City),

            ["{{LOGO_ALLIANZ_URL}}"] = Encode(data.LogoAllianzUrl),
            ["{{LOGO_CORRETORA_URL}}"] = Encode(data.LogoCorretoraUrl),
            ["{{LOGO_FRANCA_URL}}"] = Encode(data.LogoFrancaUrl),

            ["{{PROPONENTE}}"] = Encode(data.Proponente),
            ["{{FIPE_CODE}}"] = Encode(data.FipeCode),
            ["{{FIPE_VALUE}}"] = Encode(data.FipeValue),
            ["{{VEHICLE}}"] = Encode(data.Vehicle),
            ["{{PLATE}}"] = Encode(data.Plate),

            ["{{DANOS_MATERIAIS}}"] = Encode(data.DanosMateriais),
            ["{{DANOS_CORPORAIS}}"] = Encode(data.DanosCorporais),
            ["{{DANOS_MORAIS}}"] = Encode(data.DanosMorais),
            ["{{APP_MORTE}}"] = Encode(data.AppMorte),
            ["{{APP_INVALIDEZ}}"] = Encode(data.AppInvalidez),

            ["{{ASSISTENCIA_GUINCHO}}"] = Encode(data.AssistenciaGuincho),
            ["{{CARRO_RESERVA}}"] = Encode(data.CarroReserva),

            ["{{FRANQUIA_PARABRISA}}"] = Encode(data.FranquiaParabrisa),
            ["{{FRANQUIA_VIDRO_LATERAL}}"] = Encode(data.FranquiaVidroLateral),
            ["{{FRANQUIA_FAROL_CONVENCIONAL}}"] = Encode(data.FranquiaFarolConvencional),
            ["{{FRANQUIA_LANTERNA_CONVENCIONAL}}"] = Encode(data.FranquiaLanternaConvencional),
            ["{{FRANQUIA_FAROL_XENON_LED}}"] = Encode(data.FranquiaFarolXenonLed),
            ["{{FRANQUIA_LANTERNA_LED}}"] = Encode(data.FranquiaLanternaLed),
            ["{{FRANQUIA_RETROVISOR}}"] = Encode(data.FranquiaRetrovisor),
            ["{{FRANQUIA_LANTERNA_AUXILIAR}}"] = Encode(data.FranquiaLanternaAuxiliar),
            ["{{FRANQUIA_PNEU_RODA}}"] = Encode(data.FranquiaPneuRoda),
            ["{{FRANQUIA_PEQUENOS_REPAROS}}"] = Encode(data.FranquiaPequenosReparos),
            ["{{FRANQUIA_VEICULO}}"] = Encode(data.FranquiaVeiculo),
            ["{{TIPO_FRANQUIA_VEICULO}}"] = Encode(data.TipoFranquiaVeiculo),

            ["{{CONDUTOR_PRINCIPAL}}"] = Encode(data.CondutorPrincipal),
            ["{{USO_VEICULO}}"] = Encode(data.UsoVeiculo),
            ["{{ESTADO_CIVIL}}"] = Encode(data.EstadoCivil),
            ["{{CEP_PERNOITE}}"] = Encode(data.CepPernoite),
            ["{{RESIDE_EM}}"] = Encode(data.ResideEm),
            ["{{CONDUTORES_18_25}}"] = Encode(data.Condutores18a25),

            ["{{BROKER_CONTACT_NAME}}"] = Encode(data.BrokerContactName),
            ["{{BROKER_CONTACT_PHONE}}"] = Encode(data.BrokerContactPhone),
            ["{{BROKER_CONTACT_EMAIL}}"] = Encode(data.BrokerContactEmail),

            ["{{PAYMENT_ROWS}}"] = BuildPaymentRows(data.PaymentRows)
        };

        foreach (var item in replacements)
        {
            html = html.Replace(item.Key, item.Value);
        }

        return html;
    }

    private static string Encode(string? value)
    {
        return HtmlEncoder.Default.Encode(value ?? string.Empty);
    }

    private static string BuildPaymentRows(List<PaymentRowData> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            return """
<tr>
    <td class="payment-label">-</td>
    <td>-</td>
    <td>-</td>
    <td>-</td>
</tr>
""";
        }

        var sb = new StringBuilder();

        foreach (var row in rows)
        {
            var carneClass = GetPaymentCellClass(row.CarneSemJuros, row.Parcela);
            var cartaoClass = GetPaymentCellClass(row.CartaoCreditoSemJuros, row.Parcela);
            var debitoClass = GetPaymentCellClass(row.DebitoContaSemJuros, row.Parcela);

            sb.AppendLine("<tr>");
            sb.AppendLine($"    <td class=\"payment-label\">{Encode(row.Parcela)}</td>");
            sb.AppendLine($"    <td{BuildClassAttribute(carneClass)}>{Encode(row.Carne)}</td>");
            sb.AppendLine($"    <td{BuildClassAttribute(cartaoClass)}>{Encode(row.CartaoCredito)}</td>");
            sb.AppendLine($"    <td{BuildClassAttribute(debitoClass)}>{Encode(row.DebitoConta)}</td>");
            sb.AppendLine("</tr>");
        }

        return sb.ToString();
    }

    private static string GetPaymentCellClass(bool semJuros, string? parcela)
    {
        if (!semJuros)
            return string.Empty;

        return IsVista(parcela)
            ? "payment-highlight-strong"
            : "payment-highlight";
    }

    private static bool IsVista(string? parcela)
    {
        var normalized = (parcela ?? string.Empty).Trim();

        return normalized == "01" || normalized == "1";
    }

    private static string BuildClassAttribute(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return string.Empty;

        return $" class=\"{className}\"";
    }
}