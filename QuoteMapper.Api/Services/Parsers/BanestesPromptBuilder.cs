using MyPdfApi.Models;
using MyPdfApi.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class BanestesPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Banestes";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Banestes e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Não use blocos de código.
            - Não escreva nada antes ou depois do JSON.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Banestes normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Banestes.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor do campo "Proponente".
            - vehicle = descrição completa do campo "Veículo", removendo o código FIPE do início se necessário.
            - plate = se não existir placa claramente identificada, retorne string vazia.
            - condutorPrincipal = valor do campo "Condutor Principal".
            - usoVeiculo = valor do campo "Tipo de uso?".
            - cepPernoite = valor do campo "CEP Pernoite".
            - estadoCivil = se não existir claramente, retorne string vazia.
            - resideEm = use "Cidade Pernoite" e "UF" quando existir, por exemplo "CACHOEIRO DE ITAPEMIRIM - ES".
            - condutores18a25 = valor do campo "Contratada cobertura para condutores menores de 25 anos?".

            REGRAS FIPE:
            - fipeCode = extrair o código no início do campo "Veículo".
            - Exemplo: "014074-0 CIVIC SEDAN..." => fipeCode = "014074-0".
            - anoModelo = extrair o segundo ano do campo "Fabricação/Modelo".
            - Exemplo: "2014 / 2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.
            - NÃO extrair valores monetários para fipeCode.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da linha "Cobertura RCFV/Danos Materiais".
            - danosCorporais = valor da linha "Cobertura RCFV/Danos Corporais".
            - danosMorais = valor da linha "Cobertura RCFV/Danos Morais".
            - appMorte = valor da linha "Cobertura APP/ Morte Acidental por Passageiro".
            - appInvalidez = valor da linha "Cobertura APP/ Invalidez Permanente Total ou Parcial por Acidente por Passageiro".
            - assistenciaGuincho = extrair o texto de guincho do serviço "ATENDIMENTO 24H", por exemplo "Guincho km ilimitado".
            - carroReserva = extrair a quantidade de dias de "Carro Reserva", por exemplo "15 DIAS".
            - tipoCarroReserva = extrair o tipo antes da quantidade de dias, por exemplo "MANUAL".
            - tipoFranquiaVeiculo = valor da coluna "Franquia" da linha "Cobertura Casco Compreensiva".
            - franquiaVeiculo = valor da coluna "Valor da Franquia" da linha "Cobertura Casco Compreensiva".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa Dianteiro".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Farol Convencional".
            - franquiaLanternaConvencional = valor de "Lanterna Convencional".
            - franquiaFarolXenonLed = valor de "Farol Xenon/LED".
            - franquiaLanternaLed = valor de "Lanterna LED".
            - franquiaRetrovisor = valor de "Retrovisor Convencional" ou "Retrovisor LED".
            - franquiaLanternaAuxiliar = valor de "Lanterna Auxiliar".
            - franquiaPneuRoda = se não existir claramente, retorne string vazia.
            - franquiaPequenosReparos = se não existir claramente, retorne string vazia.

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção "Parcelamento (R$)".
            - Existem 3 tabelas:
              1. Carnê
              2. Cartão de Crédito
              3. Débito em Conta Corrente - BANESTES S.A.
            - A coluna "carne" deve receber os valores da tabela "Carnê".
            - A coluna "cartaoCredito" deve receber os valores da tabela "Cartão de Crédito".
            - A coluna "debitoConta" deve receber os valores da tabela "Débito em Conta Corrente - BANESTES S.A.".
            - Use o campo "Valor", não use "Valor Total".
            - Converta as parcelas assim:
              - "1" = "01"
              - "1 + 1" = "02"
              - "1 + 2" = "03"
              - "1 + 3" = "04"
              - "1 + 4" = "05"
              - "1 + 5" = "06"
              - "1 + 6" = "07"
              - "1 + 7" = "08"
              - "1 + 8" = "09"
              - "1 + 9" = "10"
            - Preserve o valor monetário exatamente como aparece.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.
            - Não inclua "Valor Total" em paymentRows.
            - Monte paymentRows somente com as parcelas existentes no PDF.

            REGRAS DE DESTAQUE DE JUROS:
            - Como o PDF Banestes mostra o mesmo "Valor Total" para todas as parcelas, marque carneSemJuros, cartaoCreditoSemJuros e debitoContaSemJuros como true quando houver valor.
            - Se o valor estiver vazio, marque como false.

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
            1. Use somente dados da cotação Banestes.
            2. Não invente valores.
            3. Não misture "Valor" com "Valor Total".
            4. Retorne somente JSON válido.
            """;
    }
}