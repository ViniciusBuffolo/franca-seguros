using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class SuhaiPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Suhai";

    public string BuildPrompt(CoverageType coverageType)
    {
        var coverageTypeText = coverageType switch
        {
            CoverageType.RouboEFurto => "ROUBO + FURTO",
            CoverageType.Basico => "TERCEIROS RCF",
            CoverageType.Ampliado => "ROUBO + FURTO + RCF",
            CoverageType.Completo => "OPÇÃO COMPREENSIVA",
            CoverageType.Master => "ROUBO + FURTO + PT COLISÃO + RCF",
            CoverageType.Exclusivo => "OPÇÃO COMPREENSIVA",
            _ => "OPÇÃO COMPREENSIVA"
        };

        var template = """
            Analise o documento PDF da cotação de seguro Suhai e retorne SOMENTE um JSON válido.

            O tipo de cobertura escolhido pelo usuário é: "__COVERAGE_TYPE__".

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - Use SOMENTE a opção/plano "__COVERAGE_TYPE__".

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor de "Nome/Razão Social".
            - vehicle = valor de "Modelo do Veículo".
            - plate = valor de "Placa".
            - condutorPrincipal = se não existir campo específico, use o proponente.
            - usoVeiculo = valor de "Utilização".
            - estadoCivil = valor de "Estado Civil".
            - cepPernoite = valor de "Reg. Tarif./CEP Pernoite"; use apenas o CEP, normalmente a parte após a barra.
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = se não existir claramente, retorne string vazia.

            REGRAS FIPE:
            - fipeCode = valor de "Código FIPE".
            - anoModelo = segundo ano de "Ano Fabr./Modelo".
            - Exemplo: "2014/2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - Extraia os valores da tabela "OPÇÕES" usando somente a coluna do plano "__COVERAGE_TYPE__".
            - danosMateriais = valor da linha "RCF - Danos Materiais".
            - danosCorporais = valor da linha "RCF - Danos Corporais".
            - danosMorais = valor da linha "RCF - Danos Morais".
            - appMorte = se não existir claramente, retorne string vazia.
            - appInvalidez = se não existir claramente, retorne string vazia.
            - assistenciaGuincho = valor da linha "Assistência 24 horas", por exemplo "Plano 2 - Guincho 500km".
            - carroReserva = se não existir claramente, retorne string vazia.
            - tipoCarroReserva = se não existir claramente, retorne string vazia.
            - franquiaVeiculo = valor da linha "Franquia Perdas Parciais" para o plano "__COVERAGE_TYPE__".
            - tipoFranquiaVeiculo = se a franquia tiver descrição, por exemplo "Reduzida", retorne essa descrição.

            REGRAS DE FRANQUIAS:
            - O PDF Suhai não apresenta franquias detalhadas de vidros, faróis e lanternas.
            - franquiaParabrisa = string vazia.
            - franquiaVidroLateral = string vazia.
            - franquiaFarolConvencional = string vazia.
            - franquiaLanternaConvencional = string vazia.
            - franquiaFarolXenonLed = string vazia.
            - franquiaLanternaLed = string vazia.
            - franquiaRetrovisor = string vazia.
            - franquiaLanternaAuxiliar = string vazia.
            - franquiaPneuRoda = string vazia.
            - franquiaPequenosReparos = string vazia.

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a tabela de pagamento correspondente ao plano "__COVERAGE_TYPE__".
            - Se "__COVERAGE_TYPE__" for "OPÇÃO COMPREENSIVA", use a tabela "OPÇÃO COMPREENSIVA (ROUBO + FURTO + PT COLISÃO + PERDAS PARCIAIS)".
            - Se "__COVERAGE_TYPE__" for "ROUBO + FURTO + RCF", use a tabela "OPÇÃO ROUBO + FURTO + RCF".
            - Se "__COVERAGE_TYPE__" for "ROUBO + FURTO + PT COLISÃO + RCF", use a tabela "OPÇÃO ROUBO + FURTO + PT COLISÃO + RCF".
            - Se "__COVERAGE_TYPE__" for "TERCEIROS RCF", use a tabela "TERCEIROS RCF".
            - Use somente o valor da coluna "Valor Parcela".
            - Não use a coluna "Valor Total".
            - No campo parcela, use "01", "02", "03" ... "12".
            - Como a Suhai não separa forma de pagamento por boleto, cartão e débito, preencha o mesmo valor em:
              - carne
              - cartaoCredito
              - debitoConta
            - Preserve os valores exatamente como aparecem.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.

            REGRAS DE DESTAQUE DE JUROS:
            - Se a coluna "Juros (%)" for "0,000000", marque os booleanos como true.
            - Se a coluna "Juros (%)" for maior que zero, marque como false.
            - A regra vale para carneSemJuros, cartaoCreditoSemJuros e debitoContaSemJuros.

            Retorne exatamente nesta estrutura:

            {
              "proponente": "",
              "fipeValue": "",
              "fipeCode": "",
              "anoModelo": "",
              "vehicle": "",
              "plate": "",
              "danosMateriais": "",
              "danosCorporais": "",
              "danosMorais": "",
              "appMorte": "",
              "appInvalidez": "",
              "assistenciaGuincho": "",
              "carroReserva": "",
              "tipoCarroReserva": "",
              "franquiaParabrisa": "",
              "franquiaVidroLateral": "",
              "franquiaFarolConvencional": "",
              "franquiaLanternaConvencional": "",
              "franquiaFarolXenonLed": "",
              "franquiaLanternaLed": "",
              "franquiaRetrovisor": "",
              "franquiaLanternaAuxiliar": "",
              "franquiaPneuRoda": "",
              "franquiaPequenosReparos": "",
              "franquiaVeiculo": "",
              "tipoFranquiaVeiculo": "",
              "condutorPrincipal": "",
              "usoVeiculo": "",
              "estadoCivil": "",
              "cepPernoite": "",
              "resideEm": "",
              "condutores18a25": "",
              "paymentRows": [
                {
                  "parcela": "",
                  "carne": "",
                  "carneSemJuros": false,
                  "cartaoCredito": "",
                  "cartaoCreditoSemJuros": false,
                  "debitoConta": "",
                  "debitoContaSemJuros": false
                }
              ]
            }

            REGRAS FINAIS:
            1. Use somente a opção "__COVERAGE_TYPE__".
            2. Não invente valores.
            3. Não use "Valor Total" como valor da parcela.
            4. Retorne somente JSON válido.
            """;

        return template.Replace("__COVERAGE_TYPE__", coverageTypeText);
    }
}