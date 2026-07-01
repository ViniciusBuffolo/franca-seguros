using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class JustosPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Justos";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Justos e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Não use blocos de código.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Justos normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Justos.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor do campo "Nome" em "SEUS DADOS".
            - vehicle = combine "Marca", "Modelo" e "Ano", se necessário.
            - plate = valor do campo "Placa".
            - condutorPrincipal = use o nome do segurado/proponente, se não houver campo específico.
            - usoVeiculo = texto da seção "USO E ESPECIFICAÇÕES DO VEÍCULO".
            - estadoCivil = se não existir claramente, retorne string vazia.
            - cepPernoite = valor do campo "CEP Pernoite".
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = valor/texto relacionado a pessoas abaixo de 24 anos dirigirem o veículo.

            REGRAS FIPE:
            - fipeCode = se não existir claramente, retorne string vazia.
            - anoModelo = valor do campo "Ano".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor de "DANOS MATERIAIS PARA TERCEIROS".
            - danosCorporais = valor de "DANOS CORPORAIS PARA TERCEIROS".
            - danosMorais = valor de "DANOS MORAIS".
            - appMorte = valor de "ACIDENTES DE PASSAGEIROS".
            - appInvalidez = valor de "ACIDENTES DE PASSAGEIROS".
            - assistenciaGuincho = valor textual de "Guincho", por exemplo "KM ilimitado".
            - carroReserva = valor da seção "CARRO RESERVA", por exemplo "21 dias".
            - tipoCarroReserva = se não existir claramente, retorne string vazia.
            - franquiaVeiculo = valor da franquia da cobertura de colisão/perda parcial.
            - tipoFranquiaVeiculo = se não existir claramente, retorne string vazia.

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa".
            - franquiaVidroLateral = valor de "Lateral".
            - franquiaFarolConvencional = valor de "Farol Convencional".
            - franquiaLanternaConvencional = valor de "Lanterna Convencional".
            - franquiaFarolXenonLed = valor de "Farol Xenon/LED".
            - franquiaLanternaLed = valor de "Lanterna LED".
            - franquiaRetrovisor = valor de "Retrovisor LED" ou "Retrovisor".
            - franquiaLanternaAuxiliar = valor de "Lanterna Auxiliar".
            - franquiaPneuRoda = valor de "CONTRA BURACOS".
            - franquiaPequenosReparos = valor de "LATARIA E PINTURA".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção "CONDIÇÕES DE PAGAMENTO PARA O PLANO ANUAL".
            - A Justos normalmente possui pagamento apenas por "Cartão de crédito".
            - Preencha somente cartaoCredito.
            - Para carne e debitoConta, retorne string vazia.
            - Use o valor da coluna "Parcelas".
            - Exemplo: "1x de R$ 2.422,68" => parcela "01" e cartaoCredito "R$ 2.422,68".
            - No campo parcela, use "01", "02", "03" ... "10".
            - Preserve os valores exatamente como aparecem, incluindo "R$".
            - Não use o valor da coluna "Valor total" como valor da parcela.
            - Não use a coluna "Desconto" como valor da parcela.

            REGRAS DE DESTAQUE DE JUROS:
            - Se a coluna "Desconto" tiver valor ou "Sem desconto", marque cartaoCreditoSemJuros como true, pois a tabela não indica juros.
            - carneSemJuros e debitoContaSemJuros devem ser false quando os campos estiverem vazios.

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
            1. Use somente dados da cotação Justos.
            2. Não invente valores.
            3. Não use "Valor total" como valor da parcela.
            4. Não preencha carne ou debitoConta se o PDF só mostrar cartão de crédito.
            5. Retorne somente JSON válido.
            """;
    }
}