using QuoteMapper.Api.Models;

namespace QuoteMapper.Api.Services.Parsers;

public sealed class AzulPromptBuilder : IQuotePromptBuilder
{
    public string InsurerKey => "Azul";

    public string BuildPrompt(CoverageType coverageType)
    {
        return """
            Analise o documento PDF da cotacao de seguro Azul e retorne SOMENTE um JSON valido.

            REGRAS GERAIS:
            - Leia todas as paginas do PDF.
            - Retorne apenas JSON valido.
            - Nao escreva explicacoes.
            - Nao use markdown.
            - Nao use blocos de codigo.
            - Nao escreva nada antes ou depois do JSON.
            - Se um campo nao existir, retorne string vazia.
            - Preserve os valores exatamente como aparecem no PDF, inclusive formato monetario, texto descritivo e percentuais.
            - O PDF da Azul normalmente possui apenas um plano/orcamento principal.
            - Ignore o parametro CoverageType para Azul.

            REGRAS DE IDENTIFICACAO DOS DADOS PRINCIPAIS:
            - proponente = nome do campo "Segurado(a)".
            - vehicle = descricao completa do veiculo na secao "Veiculo".
            - plate = valor do campo "Placa".
            - IMPORTANTE: o campo "Placa" vem antes do "Chassi"; use somente a placa e nunca copie o chassi.
            - condutorPrincipal = valor do condutor 1.
            - usoVeiculo = valor do campo "Tipo de uso".
            - cepPernoite = valor do campo "CEP de pernoite".
            - estadoCivil = se nao existir claramente, retorne string vazia.
            - resideEm = se nao existir claramente, retorne string vazia.
            - condutores18a25 = se nao existir claramente, retorne string vazia.
            - combustivel = valor do campo "Combustivel".

            REGRAS FIPE:
            - fipeCode = extrair o valor do campo "Fipe".
            - Preserve exatamente como aparece no PDF.
            - anoModelo = extrair o segundo ano do campo "Ano Fabricacao / Modelo".
            - Exemplo: "2025 / 2025" => anoModelo = "2025".
            - fipeValue = sempre string vazia.
            - NAO extrair valores monetarios para fipeCode.
            - NAO usar valores da tabela de cobertura como FIPE.

            REGRAS DE COBERTURAS:
            - danosMateriais = valor de LMI da linha "RCF-V Danos Materiais".
            - danosCorporais = valor de LMI da linha "RCF-V Danos Corporais".
            - danosMorais = valor de LMI da linha "Danos Morais e Esteticos".
            - appMorte = valor de LMI da linha "Acidentes Pessoais Passageiros".
            - appInvalidez = valor de LMI da linha "Acidentes Pessoais Passageiros".
            - IMPORTANTE: use o valor de LMI/indenizacao; nunca use "Valor do Premio" para preencher coberturas.
            - assistenciaGuincho = valor textual da assistencia, por exemplo "ILIMITADA".
            - carroReserva = extrair a quantidade de dias da linha "Carro reserva", se existir claramente.
            - tipoCarroReserva = extrair o porte/tipo da linha "Carro reserva", se existir claramente.
            - tipoFranquiaVeiculo = texto entre parenteses da franquia da cobertura compreensiva, por exemplo "25% da Obrigatoria".
            - franquiaVeiculo = valor monetario da franquia da cobertura compreensiva.

            REGRAS DE FRANQUIAS:
            - franquiaParabrisa = valor de "Vidros (Para-Brisa e Traseiro)".
            - franquiaVidroLateral = valor de "Vidros Laterais".
            - franquiaFarolConvencional = valor de "Farois/Lanternas".
            - franquiaLanternaConvencional = valor de "Farois/Lanternas".
            - franquiaFarolXenonLed = valor de "Farois de Xenonio".
            - franquiaLanternaLed = valor de "Lanternas de LED".
            - franquiaRetrovisor = valor de "Retrovisores".
            - franquiaLanternaAuxiliar = se nao existir claramente, retorne string vazia.
            - franquiaPneuRoda = se nao existir claramente, retorne string vazia.
            - franquiaPequenosReparos = se nao existir claramente, retorne string vazia.

            REGRAS DE FORMAS DE PAGAMENTO:
            - Procure a secao "Formas de pagamento".
            - Para cartaoCredito, use a tabela "TODAS CARTAO DE CREDITO - DEMAIS BANDEIRAS".
            - Para debitoConta, use a tabela "TODAS DEBITO C. CORRENTE".
            - Para carne, use a tabela "1 BOLETO / DEMAIS CARNE".
            - Ignore outras tabelas alternativas se a tabela principal correspondente estiver completa.
            - Monte uma linha para cada parcela de 1x ate 12x encontrada.
            - No campo parcela, use "01", "02", "03" ... "12".
            - Se a parcela aparecer como "-", retorne string vazia no valor correspondente.
            - Se uma forma de pagamento nao existir para uma parcela, retorne string vazia.
            - Preserve os valores exatamente como aparecem, por exemplo "R$ 1.555,01".
            - Nao inclua texto de juros dentro do valor. O valor deve ser apenas o valor monetario da parcela.

            REGRAS DE DESTAQUE DE JUROS:
            - Se abaixo do valor estiver escrito "(s/juros)" ou "s/juros", marque o booleano correspondente como true.
            - Se houver texto "juros" sem "s/juros", marque como false.
            - Se o valor estiver vazio ou "-", marque como false.
            - carneSemJuros corresponde a tabela de boleto/carne.
            - cartaoCreditoSemJuros corresponde a tabela de cartao de credito.
            - debitoContaSemJuros corresponde a tabela de debito em conta.

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
            1. Use somente dados da cotacao Azul.
            2. Nao invente valores.
            3. Nao misture valores de tabelas diferentes sem respeitar carne, cartao e debito.
            4. Retorne somente JSON valido.
            """;
    }
}
