using QuoteMapper.Api.Dtos;

namespace QuoteMapper.Api.Interfaces;
public interface IFipeService
{
    Task<string> GetFipeValueAsync(FipeRequestDto request);
}