using MyPdfApi.Models;
using MyPdfApi.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class DarwinPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Darwin";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Darwin e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Não use blocos de código.
            - Não escreva nada antes ou depois do JSON.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Darwin normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Darwin.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = nome do segurado/proponente, normalmente aparece junto ao CPF.
            - vehicle = descrição completa do veículo.
            - plate = placa do veículo.
            - condutorPrincipal = nome do principal condutor.
            - usoVeiculo = valor de uso do veículo, por exemplo "Uso pessoal".
            - estadoCivil = se não existir claramente, retorne string vazia.
            - cepPernoite = CEP de pernoite.
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = resposta relacionada a condutores de 18 a 25 anos; se não existir claramente, retorne string vazia.

            REGRAS FIPE:
            - fipeCode = código FIPE do veículo.
            - Exemplo: "015090-8".
            - anoModelo = segundo ano do campo ano fabricação/modelo.
            - Exemplo: "2014 / 2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.
            - NÃO extrair valores monetários para fipeCode.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da cobertura de danos materiais.
            - danosCorporais = valor da cobertura de danos corporais.
            - danosMorais = valor da cobertura de danos morais.
            - appMorte = valor da cobertura APP/morte, acidentes pessoais ou morte por passageiro.
            - appInvalidez = valor da cobertura APP/invalidez.
            - assistenciaGuincho = valor textual da assistência, por exemplo "Guincho 1000km".
            - carroReserva = quantidade de dias da cobertura "Carro reserva", por exemplo "15 dias".
            - tipoCarroReserva = se não existir claramente, retorne string vazia.
            - franquiaVeiculo = valor da franquia do casco/veículo.
            - tipoFranquiaVeiculo = se não existir claramente, retorne string vazia.

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Parabrisa".
            - franquiaVidroLateral = valor de "Lateral".
            - franquiaFarolConvencional = valor de "Farol Conv.".
            - franquiaLanternaConvencional = valor de "Lanterna Conv.".
            - franquiaFarolXenonLed = valor de "Farol Xenon/Led".
            - franquiaLanternaLed = valor de "Lanterna Led".
            - franquiaRetrovisor = valor de "Retrovisor".
            - franquiaLanternaAuxiliar = valor de "Lanterna Aux.".
            - franquiaPneuRoda = valor de "RPS (Rodas, Pneus e Suspensão)".
            - franquiaPequenosReparos = valor de "RLP" ou reparo leve/rápido, se existir claramente.

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção de parcelamento/pagamento.
            - O PDF Darwin pode apresentar valores em sequência, sem uma tabela visual clara no texto extraído.
            - Use os valores de parcelas do prêmio total.
            - Ignore linhas de prêmio líquido, IOF e adicional de fracionamento.
            - Monte paymentRows para as parcelas encontradas, normalmente de 01 até 10.
            - No campo parcela, use "01", "02", "03" ... conforme a quantidade de parcelas.
            - Se houver somente uma tabela de parcelamento, preencha o mesmo valor em:
              - carne
              - cartaoCredito
              - debitoConta
            - Se o PDF separar formas de pagamento, respeite cada tabela separadamente.
            - Preserve os valores exatamente como aparecem, incluindo "R$".
            - Se uma forma de pagamento não existir, retorne string vazia.

            REGRAS DE DESTAQUE DE JUROS:
            - Se o total parcelado for igual ao prêmio total à vista, marque o booleano correspondente como true.
            - Se o total parcelado for maior que o prêmio total à vista, marque como false.
            - Se não for possível identificar o total parcelado, marque como true apenas quando não houver indicação de juros.
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
            1. Use somente dados da cotação Darwin.
            2. Não invente valores.
            3. Não use valores de IOF como parcelas.
            4. Não use prêmio líquido como parcela.
            5. Retorne somente JSON válido.
            """;
    }
}