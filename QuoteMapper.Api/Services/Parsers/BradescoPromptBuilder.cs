using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class BradescoPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Bradesco";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Bradesco e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Não use blocos de código.
            - Não escreva nada antes ou depois do JSON.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Bradesco normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Bradesco.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor do campo "Nome" em "DADOS DO PROPONENTE".
            - vehicle = valor do campo "Tipo do Veículo" em "OBJETO DO SEGURO".
            - plate = valor do campo "Placa".
            - condutorPrincipal = nome em "Características do principal condutor" ou, se não existir, use o proponente.
            - usoVeiculo = valor do campo "Uso Veículo".
            - estadoCivil = valor do campo "Estado Civil".
            - cepPernoite = valor do campo "CEP de Pernoite".
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = resposta da pergunta sobre cobertura para outro condutor entre 18 e 25 anos.

            REGRAS FIPE:
            - fipeCode = valor do campo "Código FIPE".
            - Preserve exatamente como aparece no PDF.
            - anoModelo = valor do campo "Ano Mod.".
            - fipeValue = sempre string vazia.
            - NÃO extrair valores monetários para fipeCode.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor de "D.M." na seção "LIMITES MÁXIMOS DE INDENIZAÇÃO - LMI (R$)".
            - danosCorporais = valor de "D.C." na seção "LIMITES MÁXIMOS DE INDENIZAÇÃO - LMI (R$)".
            - danosMorais = valor de "D. Morais.".
            - appMorte = valor de "Morte p/ Passageiro".
            - appInvalidez = valor de "Invalidez p/ Passageiro".
            - assistenciaGuincho = extrair de "Assist Auto Dia/Noite - Passeio Ilimitado" ou "Assis. Dia Noite"; se existir "Ilimitado", retorne "Passeio Ilimitado".
            - carroReserva = extrair da cláusula "Auto Reserva Plus - 15 dias", por exemplo "15 dias".
            - tipoCarroReserva = se não existir claramente, retorne string vazia.
            - tipoFranquiaVeiculo = texto entre parênteses no campo "Veículo" da seção "FRANQUIAS (R$)", por exemplo "Reduzida".
            - franquiaVeiculo = valor monetário antes do parênteses no campo "Veículo" da seção "FRANQUIAS (R$)".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-Brisa".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Faróis".
            - franquiaLanternaConvencional = valor de "Lanternas".
            - franquiaFarolXenonLed = valor de "Faróis de Xenon" ou "Faróis de Led".
            - franquiaLanternaLed = valor de "Lanternas de LED".
            - franquiaRetrovisor = valor de "Retrovisores".
            - franquiaLanternaAuxiliar = valor de "Lanternas Auxiliares".
            - franquiaPneuRoda = valor de "Rodas, Pneus e Suspensão".
            - franquiaPequenosReparos = valor de "Reparo Rápido".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção "PAGAMENTO (R$)".
            - Existem 4 tabelas:
              1. Débito em Conta
              2. Cartão de Crédito Bradesco
              3. Cartão de Crédito
              4. Carnê
            - Para debitoConta, use a tabela "Débito em Conta".
            - Para cartaoCredito, use a tabela "Cartão de Crédito", não use "Cartão de Crédito Bradesco".
            - Para carne, use a tabela "Carnê".
            - Use somente a coluna "Parcelas".
            - Não use a coluna "Total".
            - No campo parcela, converta:
              - "1x" = "01"
              - "2x" = "02"
              - "3x" = "03"
              - até "12x" = "12"
            - Preserve os valores exatamente como aparecem, incluindo "R$".
            - Se a forma de pagamento não existir para uma parcela, retorne string vazia.
            - Monte linhas de 01 até 12 somente quando existirem no PDF.

            REGRAS DE DESTAQUE DE JUROS:
            - Se o "Total" da parcela for igual ao total à vista, marque o booleano correspondente como true.
            - Se o "Total" for maior que o total à vista, marque como false.
            - Se o valor estiver vazio, marque como false.
            - carneSemJuros corresponde à tabela "Carnê".
            - cartaoCreditoSemJuros corresponde à tabela "Cartão de Crédito".
            - debitoContaSemJuros corresponde à tabela "Débito em Conta".

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
            1. Use somente dados da cotação Bradesco.
            2. Não invente valores.
            3. Não use "Cartão de Crédito Bradesco" para preencher cartaoCredito.
            4. Não misture "Parcelas" com "Total".
            5. Retorne somente JSON válido.
            """;
    }
}