using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class YelumPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Yelum";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotacao de seguro Yelum e retorne SOMENTE um JSON valido.

            REGRAS GERAIS:
            - Leia todas as paginas do PDF.
            - Retorne apenas JSON valido.
            - Nao escreva explicacoes.
            - Nao use markdown.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Yelum normalmente possui apenas um plano/orcamento principal.
            - Ignore o parametro CoverageType para Yelum.

            REGRAS DE IDENTIFICACAO DOS DADOS PRINCIPAIS:
            - proponente = valor de "Nome do Segurado(a)".
            - vehicle = valor de "Marca/Tipo do Veiculo".
            - plate = valor de "Placa".
            - condutorPrincipal = valor de "Nome do Principal Condutor".
            - usoVeiculo = valor de "Utilizacao" ou "Uso do Veiculo".
            - estadoCivil = valor de "Estado Civil".
            - cepPernoite = valor de "CEP de Pernoite".
            - resideEm = se nao existir claramente, retorne string vazia.
            - condutores18a25 = valor de "Residente 18/24 anos".
            - combustivel = texto entre parenteses no veiculo, por exemplo "Flex", "Gasolina" ou "Diesel".

            REGRAS FIPE:
            - fipeCode = valor de "Codigo FIPE".
            - anoModelo = segundo ano de "Ano Fabricacao/Modelo".
            - Exemplo: "2014/2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor de "RESP CIVIL FACULTATIVA VEICULOS - DANOS MATERIAIS".
            - danosCorporais = valor de "RESP CIVIL FACULTATIVA VEICULOS - DANOS CORPORAIS".
            - danosMorais = valor de "RESP CIVIL FACULTATIVA VEICULOS - DANOS MORAIS E ESTETICOS".
            - appMorte = valor de "ACIDENTES PESSOAIS PASSAGEIROS - LMI POR PASSAGEIRO - MORTE".
            - appInvalidez = valor de "ACIDENTES PESSOAIS PASSAGEIROS - LMI POR PASSAGEIRO - INVALIDEZ PERMANENTE".
            - IMPORTANTE: nessas linhas use o valor de LMI/R$ da cobertura, nunca o premio e nunca a franquia.
            - assistenciaGuincho = valor de "ASSISTENCIA", por exemplo "INTERMEDIARIO" ou "SUPERIOR".
            - carroReserva = extraia a quantidade de dias de "CARRO RESERVA", por exemplo "15 DIAS".
            - tipoCarroReserva = extraia o tipo de "CARRO RESERVA", por exemplo "BASICO COM AR".
            - franquiaVeiculo = valor monetario da franquia da cobertura "BASICA - 01-COMPREENSIVA".
            - tipoFranquiaVeiculo = valor de "Tipo de Franquia", por exemplo "0,5 - FACULTATIVA".
            - IMPORTANTE: na linha "BASICA - 01-COMPREENSIVA", franquiaVeiculo deve ser o valor monetario da franquia, nao o percentual/tipo.

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa".
            - franquiaVidroLateral = valor de "Laterais".
            - franquiaFarolConvencional = valor de "Farois".
            - franquiaLanternaConvencional = valor de "Lanternas".
            - franquiaFarolXenonLed = valor de "Farois de LED ou Xenon".
            - franquiaLanternaLed = valor de "Lanternas LED".
            - franquiaRetrovisor = valor de "Retrovisores".
            - franquiaLanternaAuxiliar = valor de "Farol Auxiliar"; se nao existir claramente, retorne string vazia.
            - franquiaPneuRoda = valor de "Protecao de Roda e Pneu", se existir claramente.
            - franquiaPequenosReparos = valor de "PROTECAO PEQUENOS REPAROS".
            - IMPORTANTE: para vidros e pequenos reparos, use os valores de franquia descritos em "INFORMACOES COMPLEMENTARES", nao o premio da cobertura.

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a secao "FORMA DE PAGAMENTO".
            - Existem colunas:
              1. CARNE
              2. DEBITO C/C
              3. CARTAO DE CREDITO
              4. QR Code PIX
            - Para carne, use a coluna "CARNE".
            - Para debitoConta, use a coluna "DEBITO C/C".
            - Para cartaoCredito, use a coluna "CARTAO DE CREDITO".
            - Ignore a coluna "QR Code PIX".
            - Use somente os valores de parcela.
            - No campo parcela, converta:
              - "A vista" = "01"
              - "1 + 1" = "02"
              - "1 + 2" = "03"
              - "1 + 3" = "04"
              - ate "1 + 11" = "12"
            - Preserve os valores exatamente como aparecem.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.
            - Se uma forma de pagamento nao tiver valor para a parcela, retorne string vazia.
            - IMPORTANTE: a ordem correta das colunas e CARNE, DEBITO C/C, CARTAO DE CREDITO, QR Code PIX.
            - IMPORTANTE: debitoConta deve usar a coluna DEBITO C/C e cartaoCredito deve usar a coluna CARTAO DE CREDITO.

            REGRAS DE DESTAQUE DE JUROS:
            - Se nao houver indicacao clara de juros na linha, marque o booleano como true quando houver valor.
            - Se houver indicacao de juros maior que zero, marque como false.
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
            1. Use somente dados da cotacao Yelum.
            2. Nao invente valores.
            3. Nao use a coluna "QR Code PIX".
            4. Retorne somente JSON valido.
            """;
    }
}
