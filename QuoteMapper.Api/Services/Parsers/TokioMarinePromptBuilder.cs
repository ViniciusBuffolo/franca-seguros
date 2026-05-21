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
            - combustivel = valor do campo "Combustível", exemplo "Gasolina", "Flex", "Diesel".

            REGRAS FIPE:
            - fipeCode = valor exato do campo "Código FIPE", exemplo "093011-3".
            - anoModelo = valor exato do campo "Ano modelo", exemplo "2024".
            - plate = valor exato do campo "Placa", exemplo "SFW-6F27".
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
            - Existem 3 formas de pagamento:
              1. "Débito/Pix Aut." = campo debitoConta
              2. "Ficha" = campo carne
              3. "Cartão" = campo cartaoCredito

            - Para debitoConta, use EXCLUSIVAMENTE a tabela cujo cabeçalho é "Débito/Pix Aut.".
            - Para carne, use EXCLUSIVAMENTE a tabela cujo cabeçalho é "Ficha".
            - Para cartaoCredito, use EXCLUSIVAMENTE a tabela cujo cabeçalho é "Cartão".

            - NUNCA copie valores de "Cartão" para "Débito/Pix Aut.".
            - NUNCA copie valores de "Débito/Pix Aut." para "Cartão".
            - NUNCA copie valores de "Ficha" para outra forma de pagamento.

            - A tabela "Ficha" pode ter menos parcelas que as outras formas.
            - Se "Ficha" existir apenas até a parcela 10, então carne deve ficar vazio nas parcelas 11 e 12.
            - Não invente parcelas ausentes para carne.
            - Não preencha carne 11 ou carne 12 usando valores de Cartão ou Débito/Pix Aut.

            - Use somente a coluna "Parcela (R$)".
            - Não use a coluna "Total (R$)".
            - Use a coluna "Juros (%)" da mesma tabela para definir se é sem juros.

            REGRAS DE DESTAQUE DE JUROS:
            - Para cada forma de pagamento, leia a coluna "Juros (%)" da mesma tabela.
            - carneSemJuros deve usar a coluna "Juros (%)" da tabela "Ficha".
            - cartaoCreditoSemJuros deve usar a coluna "Juros (%)" da tabela "Cartão".
            - debitoContaSemJuros deve usar a coluna "Juros (%)" da tabela "Débito/Pix Aut.".
            - Se a coluna "Juros (%)" for "Sem Juros" ou "Antecipado*", marque true.
            - Se a coluna "Juros (%)" tiver percentual numérico maior que zero, marque false.

            REGRAS IMPORTANTES PARA TABELAS TOKIO MARINE:
            - As tabelas podem ser divididas visualmente em duas metades lado a lado.
            - Quando o mesmo cabeçalho aparecer duas vezes na mesma linha, isso NÃO significa duas tabelas diferentes.
            - Exemplo:
              "Ficha Parcela (R$) Juros (%) Total (R$) | Ficha Parcela (R$) Juros (%) Total (R$)"
              significa UMA única tabela "Ficha" dividida visualmente em duas partes.
            - Nesse caso:
              - a parte esquerda contém parcelas iniciais.
              - a parte direita contém continuação das parcelas.
            - Una ambas as partes em uma única coleção.
            - O mesmo vale para:
              - "Débito/Pix Aut."
              - "Cartão"
              - "Ficha"

            REGRA CRÍTICA:
            - Nunca considere a segunda metade visual da tabela como outra forma de pagamento.
            - O tipo da tabela é definido exclusivamente pelo cabeçalho repetido.

            REGRA CRÍTICA DE VALIDAÇÃO DOS PAGAMENTOS:
            Antes de retornar o JSON, valide:
            - "Ficha" no PDF deste modelo vai somente até 10 parcelas, então carne das parcelas 11 e 12 deve ser "".
            - "Débito/Pix Aut." deve conter valores próprios da tabela Débito/Pix Aut., não os valores do Cartão.
            - Se uma forma de pagamento não tiver uma parcela no PDF, deixe o campo vazio.

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
            1. Use somente dados da cotação Tokio Marine.
            2. Não invente valores.
            3. Não use a coluna "Total (R$)" como valor da parcela.
            4. Não duplique a parcela "01".
            5. Retorne somente JSON válido.
            """;
    }
}