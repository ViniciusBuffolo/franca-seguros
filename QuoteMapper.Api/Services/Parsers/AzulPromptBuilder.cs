using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class AzulPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Azul";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotação de seguro Azul e retorne SOMENTE um JSON válido.

            REGRAS GERAIS:
            - Leia todas as páginas do PDF.
            - Retorne apenas JSON válido.
            - Não escreva explicações.
            - Não use markdown.
            - Não use blocos de código.
            - Não escreva nada antes ou depois do JSON.
            - Se um campo não existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF, inclusive formato monetário, texto descritivo e percentuais.
            - O PDF da Azul normalmente possui apenas um plano/orçamento principal. Não tente escolher entre planos Allianz.
            - Ignore o parâmetro CoverageType para Azul.

            REGRAS DE IDENTIFICAÇÃO DOS DADOS PRINCIPAIS:
            - proponente = nome do campo "Proponente / Segurado(a)".
            - vehicle = descrição completa do veículo na seção "VEÍCULO".
            - plate = valor do campo "Placa".
            - condutorPrincipal = valor após "Nome do principal Condutor:".
            - usoVeiculo = valor após "Tipo do Uso:".
            - cepPernoite = valor após "CEP PERNOITE:".
            - estadoCivil = se não existir claramente, retorne string vazia.
            - resideEm = se não existir claramente, retorne string vazia.
            - condutores18a25 = se não existir claramente, retorne string vazia.

            REGRAS FIPE:
            - fipeCode = extrair o valor do campo "Fipe".
            - No PDF Azul o código FIPE pode aparecer sem hífen, por exemplo "150908".
            - Preserve exatamente como aparece no PDF.
            - anoModelo = extrair o segundo ano do campo "Ano Fabricação / Modelo".
            - Exemplo: "2014 / 2014" => anoModelo = "2014".
            - fipeValue = sempre string vazia.
            - NÃO extrair valores monetários para fipeCode.
            - NÃO usar valores da tabela de cobertura como FIPE.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor da linha "RCF-V DANOS MATERIAIS".
            - danosCorporais = valor da linha "RCF-V DANOS CORPORAIS".
            - danosMorais = valor da linha "DANOS MORAIS E ESTÉTICOS".
            - appMorte = valor da linha "ACIDENTES PESSOAIS PASSAGEIROS".
            - appInvalidez = valor da linha "ACIDENTES PESSOAIS PASSAGEIROS".
            - assistenciaGuincho = valor textual da linha "ASSISTÊNCIA", por exemplo "ILIMITADA".
            - carroReserva = extrair a quantidade de dias da linha "CARRO RESERVA", por exemplo "15 Dias".
            - tipoCarroReserva = extrair o porte/tipo da linha "CARRO RESERVA", por exemplo "Básico".
            - tipoFranquiaVeiculo = valor após "Franquia:", por exemplo "50% da Obrigatória".
            - franquiaVeiculo = valor monetário da franquia do casco, se existir claramente. Se não existir, retorne string vazia.

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Vidros (Para-Brisa e Traseiro)".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Faróis/Lanternas".
            - franquiaLanternaConvencional = valor de "Faróis/Lanternas".
            - franquiaFarolXenonLed = valor de "Faróis de Xenônio".
            - franquiaLanternaLed = valor de "Lanternas de LED".
            - franquiaRetrovisor = valor de "Retrovisores".
            - franquiaLanternaAuxiliar = se não existir claramente, retorne string vazia.
            - franquiaPneuRoda = valor de "Roda, Pneu e Suspensão".
            - franquiaPequenosReparos = valor de "Reparo Rápido".

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure as seções "FORMAS DE PAGAMENTO".
            - O PDF Azul possui tabelas como:
              - "TODAS CARTÃO DE CRÉDITO PORTO BANK (AQUISIÇÃO)"
              - "TODAS CARTÃO DE CRÉDITO PORTO BANK SEM DESCONTO (OUTRO TITULAR)"
              - "TODAS CARTÃO DE CRÉDITO - DEMAIS BANDEIRAS"
              - "TODAS DÉBITO C. CORRENTE"
              - "1 BOLETO / DEMAIS CARNÊ"
              - "1 BOLETO / DEMAIS C. CORRENTE"
              - "1 DEBITO C. CORRENTE / DEMAIS CARNÊ"
            - Para cartaoCredito, prefira a tabela "TODAS CARTÃO DE CRÉDITO - DEMAIS BANDEIRAS".
            - Se ela não estiver completa, use "TODAS CARTÃO DE CRÉDITO PORTO BANK SEM DESCONTO (OUTRO TITULAR)".
            - Para debitoConta, use a tabela "TODAS DÉBITO C. CORRENTE" ou "1 DEBITO C. CORRENTE / DEMAIS CARNÊ".
            - Para carne, use a tabela "1 BOLETO / DEMAIS CARNÊ" ou "1 BOLETO / DEMAIS C. CORRENTE".
            - Monte uma linha para cada parcela de 1x até 12x encontrada.
            - No campo parcela, use "01", "02", "03" ... "12".
            - Se a parcela aparecer como "-", retorne string vazia no valor correspondente.
            - Se uma forma de pagamento não existir para uma parcela, retorne string vazia.
            - Preserve os valores exatamente como aparecem, por exemplo "R$ 1.749,32".
            - Não inclua texto de juros dentro do valor. O valor deve ser apenas o valor monetário da parcela.

            REGRAS DE DESTAQUE DE JUROS:
            - Se abaixo do valor estiver escrito "(s/juros)" ou "s/juros", marque o booleano correspondente como true.
            - Se estiver escrito "juros", marque como false.
            - Se o valor estiver vazio ou "-", marque como false.
            - carneSemJuros corresponde à tabela de boleto/carnê.
            - cartaoCreditoSemJuros corresponde à tabela de cartão de crédito.
            - debitoContaSemJuros corresponde à tabela de débito em conta.

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
            1. Use somente dados da cotação Azul.
            2. Não invente valores.
            3. Não misture valores de tabelas diferentes sem respeitar carne, cartão e débito.
            4. Retorne somente JSON válido.
            """;
    }
}