using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class YelumPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Yelum";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Yelum e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Yelum normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Yelum.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor de "Nome do Segurado(a)".
            - vehicle = valor de "Marca/Tipo do Veículo".
            - plate = valor de "Placa".
            - condutorPrincipal = valor de "Nome do Principal Condutor".
            - usoVeiculo = valor de "Utilização" ou "Uso do Veículo".
            - estadoCivil = valor de "Estado Civil".
            - cepPernoite = valor de "CEP de Pernoite".
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = valor de "Residente 18/24 anos" ou "Deseja estender cobertura p/ residentes habilitados com idade 18 a 24 anos?".

            REGRAS FIPE:
            - fipeCode = valor de "Código FIPE".
            - anoModelo = segundo ano de "Ano Fabricação/Modelo".
            - Exemplo: "2014/2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor de "RESP CIVIL FACULTATIVA VEÍCULOS - DANOS MATERIAIS".
            - danosCorporais = valor de "RESP CIVIL FACULTATIVA VEÍCULOS - DANOS CORPORAIS".
            - danosMorais = valor de "RESP CIVIL FACULTATIVA VEÍCULOS - DANOS MORAIS E ESTÉTICOS".
            - appMorte = valor de "ACIDENTES PESSOAIS PASSAGEIROS - LMI POR PASSAGEIRO - MORTE".
            - appInvalidez = valor de "ACIDENTES PESSOAIS PASSAGEIROS - LMI POR PASSAGEIRO - INVALIDEZ PERMANENTE".
            - assistenciaGuincho = valor de "ASSISTENCIA", por exemplo "SUPERIOR".
            - carroReserva = extraia a quantidade de dias de "CARRO RESERVA", por exemplo "15 DIAS".
            - tipoCarroReserva = extraia o tipo de "CARRO RESERVA", por exemplo "BÁSICO COM AR".
            - franquiaVeiculo = valor da franquia da cobertura "BASICA - 01-COMPREENSIVA".
            - tipoFranquiaVeiculo = valor de "Tipo de Franquia", por exemplo "0.5 - FACULTATIVA".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Para-brisa".
            - franquiaVidroLateral = valor de "Laterais".
            - franquiaFarolConvencional = valor de "Farois".
            - franquiaLanternaConvencional = valor de "Lanternas".
            - franquiaFarolXenonLed = valor de "Farois de LED ou Xenon".
            - franquiaLanternaLed = valor de "Lanternas LED".
            - franquiaRetrovisor = valor de "Retrovisores".
            - franquiaLanternaAuxiliar = se não existir claramente, retorne string vazia.
            - franquiaPneuRoda = valor de "Protecao de Roda e Pneu", por exemplo "R$120,00".
            - franquiaPequenosReparos = valor de "PROTECAO PEQUENOS REPAROS".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção "FORMA DE PAGAMENTO".
            - Existem colunas:
              1. CARNÊ
              2. DÉBITO C/C
              3. CARTÃO DE CRÉDITO
              4. QR Code PIX
            - Para carne, use a coluna "CARNÊ".
            - Para debitoConta, use a coluna "DÉBITO C/C".
            - Para cartaoCredito, use a coluna "CARTÃO DE CRÉDITO".
            - Ignore a coluna "QR Code PIX".
            - Use somente os valores de parcela.
            - No campo parcela, converta:
              - "À vista" = "01"
              - "1 + 1" = "02"
              - "1 + 2" = "03"
              - "1 + 3" = "04"
              - até "1 + 11" = "12"
            - Preserve os valores exatamente como aparecem.
            - Se o valor vier sem "R$", adicione "R$ " antes do valor.
            - Se uma forma de pagamento não tiver valor para a parcela, retorne string vazia.

            REGRAS DE DESTAQUE DE JUROS:
            - Se não houver indicação clara de juros na linha, marque o booleano como true quando houver valor.
            - Se houver indicação de juros maior que zero, marque como false.
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
            1. Use somente dados da cotação Yelum.
            2. Não invente valores.
            3. Não use a coluna "QR Code PIX".
            4. Retorne somente JSON válido.
            """;
    }
}