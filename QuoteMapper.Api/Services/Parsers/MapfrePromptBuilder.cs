using MyPdfApi.Models;
using MyPdfApi.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class MapfrePromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Mapfre";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Mapfre e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Mapfre normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Mapfre.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor após "Segurado:".
            - vehicle = descrição completa do campo "Veículo".
            - plate = se não existir claramente, retorne string vazia.
            - condutorPrincipal = valor após "Nome do Principal Condutor:".
            - usoVeiculo = valor do campo "Uso".
            - estadoCivil = valor após "Estado Civil do Condutor".
            - cepPernoite = valor após "CEP de pernoite do veículo".
            - resideEm = combine cidade e UF, por exemplo "ITAPEMIRIM - ES".
            - condutores18a25 = resposta de "Deseja contratar cobertura para condutores residentes entre 18 e 25 anos?".

            REGRAS FIPE:
            - fipeCode = código entre parênteses após "Tabela de Referência: FIPE".
            - Exemplo: "(015090-8)" => "015090-8".
            - anoModelo = valor do campo "Ano Modelo".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da linha "RCFA - Danos Materiais".
            - danosCorporais = valor da linha "RCFA - Danos Corporais".
            - danosMorais = valor da linha "RCFA - Danos Morais / Estéticos".
            - appMorte = valor da linha "APP - Morte".
            - appInvalidez = valor da linha "APP - Invalidez".
            - assistenciaGuincho = use "Extensão de Reboque ILIMITADO" se contratada; caso contrário use "Assistência 24h 250 km".
            - carroReserva = extraia a quantidade de dias de "Carro Reserva - 15 dias - Intermediário".
            - tipoCarroReserva = extraia o tipo de "Carro Reserva", por exemplo "Intermediário".
            - franquiaVeiculo = valor da linha "Casco: Reduzida 50%".
            - tipoFranquiaVeiculo = texto da franquia do casco, por exemplo "Reduzida 50%".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Faróis Convencionais".
            - franquiaLanternaConvencional = valor de "Lanternas Convencionais".
            - franquiaFarolXenonLed = valor de "Faróis Xenon" ou "Faróis Led".
            - franquiaLanternaLed = valor de "Lanternas Led".
            - franquiaRetrovisor = valor de "Retrovisores Convencionais".
            - franquiaLanternaAuxiliar = valor de "Lanternas Auxiliares".
            - franquiaPneuRoda = se não existir claramente, retorne string vazia.
            - franquiaPequenosReparos = valor de "SRA - Reparo em arranhões - 1a peça" ou "Reparo de Lataria e Pintura/Para-choque".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção "Formas de Pagamento".
            - Existem 4 tabelas:
              1. Boleto
              2. Débito em Conta
              3. Débito c/ 1° em Boleto
              4. Cartão de Crédito
            - Para carne, use a tabela "Boleto".
            - Para debitoConta, use a tabela "Débito em Conta".
            - Para cartaoCredito, use a tabela "Cartão de Crédito".
            - Ignore a tabela "Débito c/ 1° em Boleto".
            - Use somente o valor da coluna "Valor da Parcela".
            - Não use a coluna "Total".
            - No campo parcela, converta:
              - "1x Sem Juros" = "01"
              - "2x Sem Juros" = "02"
              - "3x Com Juros" = "03"
              - até "12x" = "12"
            - Preserve os valores exatamente como aparecem, incluindo "R$".
            - Se a parcela estiver com "-", retorne string vazia.

            REGRAS DE DESTAQUE DE JUROS:
            - Se a parcela contiver "Sem Juros", marque o booleano correspondente como true.
            - Se a parcela contiver "Com Juros", marque como false.
            - Se o valor estiver vazio ou "-", marque como false.

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
            1. Use somente dados da cotação Mapfre.
            2. Não invente valores.
            3. Não use a coluna "Total" como valor da parcela.
            4. Ignore "Débito c/ 1° em Boleto".
            5. Retorne somente JSON válido.
            """;
    }
}