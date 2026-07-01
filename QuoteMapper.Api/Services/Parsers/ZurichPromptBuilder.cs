using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class ZurichPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Zurich";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Zurich e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Zurich normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Zurich.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor de "Nome completo/Razão social".
            - vehicle = valor de "Veículo".
            - plate = se não existir claramente, retorne string vazia.
            - condutorPrincipal = valor de "Condutor principal".
            - usoVeiculo = valor de "Uso veículo".
            - estadoCivil = valor de "Estado Civil".
            - cepPernoite = valor de "CEP do local de pernoite do veículo".
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = resposta de "Deseja cobertura do seguro para outros condutores habilitados com menos de 25 anos?".

            REGRAS FIPE:
            - fipeCode = valor de "Código: FIPE".
            - No PDF Zurich o código FIPE pode aparecer sem hífen, por exemplo "0150908".
            - anoModelo = valor de "Ano Modelo".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor de "RCV - Danos Materiais".
            - danosCorporais = valor de "RCV - Danos Corporais".
            - danosMorais = valor de "RCV - Danos Morais".
            - appMorte = valor de "APP - Acidentes Pessoais de Passageiros".
            - appInvalidez = valor de "APP - Acidentes Pessoais de Passageiros".
            - assistenciaGuincho = valor de "Assistência 24 Horas" ou texto de "400 Km de Reboque e Km ilimitado para Sinistro".
            - carroReserva = extraia de "CARRO RESERVA BÁSICO 15 DIAS", por exemplo "15 DIAS".
            - tipoCarroReserva = extraia o tipo de carro reserva, por exemplo "BÁSICO".
            - franquiaVeiculo = valor da franquia da linha "Veículo - Colisão/Incêndio/Roubo".
            - tipoFranquiaVeiculo = se existir uma letra/tipo junto da franquia, preserve, por exemplo "A".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Farol Convencional".
            - franquiaLanternaConvencional = valor de "Lanterna Convencional".
            - franquiaFarolXenonLed = valor de "Farol de Xenon/LED" ou "Farol Matrix".
            - franquiaLanternaLed = valor de "Lanterna de LED".
            - franquiaRetrovisor = valor de "Retrovisor Externo".
            - franquiaLanternaAuxiliar = se não existir claramente, retorne string vazia.
            - franquiaPneuRoda = valor de "Pneu e Roda".
            - franquiaPequenosReparos = valor de "Pequenos Reparos" ou "Reparo de Arranhoes".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção com "Valor de Entrada", "Parcelas", "Valor das Demais Parcelas" e "Prêmio Total".
            - O PDF Zurich deste modelo pode possuir somente pagamento à vista.
            - Use a linha de pagamento existente.
            - Para parcela "01", use o valor de "Valor de Entrada" ou "Prêmio Total".
            - Como o PDF não separa forma de pagamento por carnê, cartão e débito, preencha o mesmo valor em:
              - carne
              - cartaoCredito
              - debitoConta
            - No campo parcela, use "01".
            - Preserve o valor exatamente como aparece.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.

            REGRAS DE DESTAQUE DE JUROS:
            - Se "Valor dos Juros" for "0,00", marque carneSemJuros, cartaoCreditoSemJuros e debitoContaSemJuros como true.
            - Se houver juros maior que zero, marque como false.
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
            1. Use somente dados da cotação Zurich.
            2. Não invente valores.
            3. Não crie parcelas que não existem no PDF.
            4. Retorne somente JSON válido.
            """;
    }
}