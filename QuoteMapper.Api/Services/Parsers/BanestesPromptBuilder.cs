using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class BanestesPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Banestes";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotacao de seguro Banestes e retorne SOMENTE um JSON valido.

            REGRAS GERAIS:
            - Leia todas as paginas do PDF.
            - Retorne apenas JSON valido.
            - Nao escreva explicacoes.
            - Nao use markdown.
            - Nao use blocos de codigo.
            - Nao escreva nada antes ou depois do JSON.
            - Se um campo nao existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Banestes normalmente possui apenas um plano/orcamento principal.
            - Ignore o parametro CoverageType para Banestes.

            REGRAS DE IDENTIFICACAO DOS DADOS PRINCIPAIS:
            - proponente = valor do campo "Proponente".
            - vehicle = descricao completa do campo "Veiculo", removendo o codigo FIPE do inicio se necessario.
            - plate = se nao existir placa claramente identificada, retorne string vazia.
            - condutorPrincipal = valor do campo "Condutor Principal".
            - usoVeiculo = valor do campo "Tipo de uso?".
            - cepPernoite = valor do campo "CEP Pernoite".
            - estadoCivil = se nao existir claramente, retorne string vazia.
            - resideEm = use "Cidade Pernoite" e "UF" quando existir, por exemplo "CACHOEIRO DE ITAPEMIRIM - ES".
            - condutores18a25 = valor do campo "Contratada cobertura para condutores menores de 25 anos?".
            - combustivel = texto no fim do campo "Veiculo", por exemplo "FLEX", "GASOLINA" ou "DIESEL".

            REGRAS FIPE:
            - fipeCode = extrair o codigo no inicio do campo "Veiculo".
            - Exemplo: "014074-0 CIVIC SEDAN..." => fipeCode = "014074-0".
            - anoModelo = extrair o segundo ano do campo "Fabricacao/Modelo".
            - Exemplo: "2014 / 2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.
            - NAO extrair valores monetarios para fipeCode.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da coluna "Limite Maximo de Indenizacao" da linha "Cobertura RCFV/Danos Materiais".
            - danosCorporais = valor da coluna "Limite Maximo de Indenizacao" da linha "Cobertura RCFV/Danos Corporais".
            - danosMorais = valor da coluna "Limite Maximo de Indenizacao" da linha "Cobertura RCFV/Danos Morais".
            - appMorte = valor da coluna "Limite Maximo de Indenizacao" da linha "Cobertura APP/ Morte Acidental por Passageiro".
            - appInvalidez = valor da coluna "Limite Maximo de Indenizacao" da linha "Cobertura APP/ Invalidez Permanente Total ou Parcial por Acidente por Passageiro".
            - IMPORTANTE: nao use a coluna "Premio" para preencher coberturas.
            - assistenciaGuincho = extrair o texto de guincho do servico "ATENDIMENTO 24H", por exemplo "Guincho km ilimitado".
            - carroReserva = extrair a quantidade de dias de "Carro Reserva", por exemplo "15 DIAS".
            - tipoCarroReserva = extrair o tipo antes da quantidade de dias, por exemplo "MANUAL".
            - tipoFranquiaVeiculo = valor da coluna "Franquia" da linha "Cobertura Casco Compreensiva".
            - franquiaVeiculo = valor da coluna "Valor da Franquia" da linha "Cobertura Casco Compreensiva".
            - IMPORTANTE: nao use a coluna "Premio" para preencher franquiaVeiculo.

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa Dianteiro".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Farol Convencional".
            - franquiaLanternaConvencional = valor de "Lanterna Convencional".
            - franquiaFarolXenonLed = valor de "Farol Xenon/LED".
            - franquiaLanternaLed = valor de "Lanterna LED".
            - franquiaRetrovisor = valor de "Retrovisor Convencional" ou "Retrovisor LED".
            - franquiaLanternaAuxiliar = valor de "Lanterna Auxiliar".
            - franquiaPneuRoda = se nao existir claramente, retorne string vazia.
            - franquiaPequenosReparos = se nao existir claramente, retorne string vazia.

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a secao "Parcelamento (R$)".
            - Existem 3 tabelas:
              1. Carne
              2. Cartao de Credito
              3. Debito em Conta Corrente - BANESTES S.A.
            - A coluna "carne" deve receber os valores da tabela "Carne".
            - A coluna "cartaoCredito" deve receber os valores da tabela "Cartao de Credito".
            - A coluna "debitoConta" deve receber os valores da tabela "Debito em Conta Corrente - BANESTES S.A.".
            - Use o campo "Valor", nao use "Valor Total".
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
            - Preserve o valor monetario exatamente como aparece.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.
            - Nao inclua "Valor Total" em paymentRows.
            - Monte paymentRows somente com as parcelas existentes no PDF.

            REGRAS DE DESTAQUE DE JUROS:
            - Para o Banestes, parcelas 01 ate 10 de carne, cartaoCredito e debitoConta devem ficar sem juros quando houver valor.
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
              "combustivel": "",
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
            1. Use somente dados da cotacao Banestes.
            2. Nao invente valores.
            3. Nao misture "Valor" com "Valor Total".
            4. Retorne somente JSON valido.
            """;
    }
}
