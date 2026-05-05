namespace MyPdfApi.Services.Parsers;

public sealed class QuotePromptBuilderFactory
{
    private readonly IEnumerable<IQuotePromptBuilder> _builders;

    public QuotePromptBuilderFactory(IEnumerable<IQuotePromptBuilder> builders)
    {
        _builders = builders;
    }

    public IQuotePromptBuilder GetByInsurer(string insurer)
    {
        var builder = _builders.FirstOrDefault(x =>
            x.InsurerKey.Equals(insurer, StringComparison.OrdinalIgnoreCase));

        if (builder == null)
            throw new InvalidOperationException($"PromptBuilder não encontrado para a seguradora: {insurer}");

        return builder;
    }
}