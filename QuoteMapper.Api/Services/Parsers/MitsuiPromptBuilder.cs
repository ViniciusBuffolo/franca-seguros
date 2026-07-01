using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services.Parsers;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class MitsuiPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Mitsui";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Mitsui Sumitomo e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF.
            - O PDF Mitsui normalmente possui apenas um plano/orçamento principal.
            - Ignore o parâmetro CoverageType para Mitsui.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = valor de "Proponente / Segurado(a)".
            - vehicle = descrição do campo "Veículo", removendo código inicial se necessário.
            - plate = valor do campo "Placa".
            - condutorPrincipal = valor após "Nome do principal Condutor:".
            - usoVeiculo = valor após "Tipo do Uso:".
            - estadoCivil = se não existir claramente, retorne string vazia.
            - cepPernoite = valor após "CEP PERNOITE:".
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = se não existir claramente, retorne string vazia.

            REGRAS FIPE:
            - fipeCode = valor do campo "Fipe".
            - No PDF Mitsui o código FIPE pode aparecer sem hífen, por exemplo "150908".
            - Preserve exatamente como aparece.
            - anoModelo = segundo ano do campo "Ano Fabricação / Modelo".
            - Exemplo: "2014 / 2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da linha "RCF-V DANOS MATERIAIS".
            - danosCorporais = valor da linha "RCF-V DANOS CORPORAIS".
            - danosMorais = valor da linha "DANOS MORAIS E ESTÉTICOS".
            - appMorte = valor da linha "ACIDENTES PESSOAIS PASSAGEIROS".
            - appInvalidez = valor da linha "ACIDENTES PESSOAIS PASSAGEIROS".
            - assistenciaGuincho = valor textual de "REDE REFERENCIADA - KM ILIMITADO".
            - carroReserva = extraia a quantidade de dias da linha "CARRO RESERVA", por exemplo "15 DIAS".
            - tipoCarroReserva = extraia o porte/tipo da linha "CARRO RESERVA", por exemplo "PORTE BÁSICO".
            - tipoFranquiaVeiculo = valor após "Franquia:", por exemplo "50% da Obrigatória".
            - franquiaVeiculo = valor monetário da franquia do casco na linha "Casco".

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Vidros Para-Brisa, Teto Solar ou Panorâmico".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Faróis Convencionais, Milha e Neblina".
            - franquiaLanternaConvencional = valor de "Lanternas Convencionais".
            - franquiaFarolXenonLed = valor de "Faróis de LED" ou "Faróis de Xenon".
            - franquiaLanternaLed = valor de "Lanternas de LED".
            - franquiaRetrovisor = valor de "Retrovisores".
            - franquiaLanternaAuxiliar = se não existir claramente, retorne string vazia.
            - franquiaPneuRoda = valor de "Roda, Pneu e Suspensão".
            - franquiaPequenosReparos = valor de "Reparo Rápido".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a seção "FORMAS DE PAGAMENTO".
            - Para cartaoCredito, use a tabela "TODAS CARTÃO DE CRÉDITO - DEMAIS BANDEIRAS".
            - Para debitoConta, use a tabela "TODAS DÉBITO C. CORRENTE" ou "1 DEBITO C. CORRENTE / DEMAIS CARNÊ".
            - Para carne, use a tabela "1 BOLETO / DEMAIS CARNÊ".
            - Use somente o valor da parcela.
            - Não inclua o valor de juros entre parênteses.
            - No campo parcela, use "01", "02", "03" ... "12".
            - Se a parcela aparecer como "-", retorne string vazia.
            - Preserve os valores exatamente como aparecem, incluindo "R$".

            REGRAS DE DESTAQUE DE JUROS:
            - Se abaixo do valor estiver escrito "(s/juros)" ou "s/juros", marque o booleano correspondente como true.
            - Se estiver escrito "juros", marque como false.
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
            1. Use somente dados da cotação Mitsui.
            2. Não invente valores.
            3. Não use "Cartão de Crédito Porto Bank" para preencher cartaoCredito.
            4. Não misture valores de parcela com valores de juros entre parênteses.
            5. Retorne somente JSON válido.
            """;
    }
}