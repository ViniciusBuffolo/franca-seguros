using MyPdfApi.Models;
using MyPdfApi.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class TokioMarinePromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "TokioMarine";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Tokio Marine e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Tokio Marine normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Tokio Marine.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor do campo "Proponente".
            - vehicle = valor do campo "Veículo".
            - plate = valor do campo "Placa".
            - condutorPrincipal = se aparecer "Próprio Segurado", use o nome do proponente.
            - usoVeiculo = valor do campo "Tipo de utilização".
            - estadoCivil = valor de "Estado Civil principal condutor".
            - cepPernoite = valor de "CEP de pernoite".
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = resposta da pergunta sobre condutores de 18 a 25 anos.

            REGRAS FIPE:
            - fipeCode = valor do campo "Código FIPE".
            - anoModelo = valor do campo "Ano modelo".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da linha "RCF-V - Danos Materiais".
            - danosCorporais = valor da linha "RCF-V - Danos Corporais".
            - danosMorais = valor da linha "RCF-V - Danos Morais".
            - appMorte = valor da linha "APP - Morte (por passageiro)".
            - appInvalidez = valor da linha "APP - Invalidez permanente (por passageiro)".
            - assistenciaGuincho = use o valor de "Km adicional de reboque", por exemplo "Ilimitado".
            - carroReserva = valor de "Carro reserva", por exemplo "15 diárias".
            - tipoCarroReserva = complemento de "Carro reserva", por exemplo "Básico (Mecânico)".
            - franquiaVeiculo = valor de "Indenização Parcial do Veículo".
            - tipoFranquiaVeiculo = texto da franquia, por exemplo "50% da Básica".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Parabrisa".
            - franquiaVidroLateral = valor de "Lateral".
            - franquiaFarolConvencional = valor de "Farol Halógeno".
            - franquiaLanternaConvencional = valor de "Lanterna Halógena".
            - franquiaFarolXenonLed = valor de "Farol xenon/led".
            - franquiaLanternaLed = valor de "Lanterna led".
            - franquiaRetrovisor = valor de "Retrovisor externo".
            - franquiaLanternaAuxiliar = valor de "Lanterna auxiliar".
            - franquiaPneuRoda = valor de "Roda, Pneu e Suspensão".
            - franquiaPequenosReparos = valor de "Lataria e pintura" ou "Para-choque (reparo)".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure as seções de pagamento "Débito/Pix Aut.", "Ficha" e "Cartão".
            - Para debitoConta, use a tabela "Débito/Pix Aut.".
            - Para carne, use a tabela "Ficha".
            - Para cartaoCredito, use a tabela "Cartão".
            - Use somente o valor da coluna "Parcela (R$)".
            - Não use a coluna "Total (R$)".
            - No campo parcela, use "01", "02", "03" ... "12".
            - Preserve os valores exatamente como aparecem.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.
            - A linha com juros "Antecipado*" deve ser tratada como pagamento à vista/parcela "01" apenas quando for a opção com desconto à vista.
            - Não duplique a parcela "01"; prefira a linha "Sem Juros" para parcela 01, salvo se a tabela só tiver "Antecipado*".

            REGRAS DE DESTAQUE DE JUROS:
            - Se a coluna "Juros (%)" for "Sem Juros" ou "Antecipado*", marque o booleano correspondente como true.
            - Se a coluna "Juros (%)" tiver percentual numérico maior que zero, marque como false.
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
            1. Use somente dados da cotação Tokio Marine.
            2. Não invente valores.
            3. Não use a coluna "Total (R$)" como valor da parcela.
            4. Não duplique a parcela "01".
            5. Retorne somente JSON válido.
            """;
    }
}