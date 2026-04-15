using Microsoft.AspNetCore.Http;

namespace QuoteMapper.Api.Dtos
{
    public class UploadQuoteRequestDto
    {
        public IFormFile? File { get; set; }
        public string? InsurerHint { get; set; }
    }
}