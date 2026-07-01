namespace QuoteMapper.Api.Models;

public sealed class ChatPdfOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.chatpdf.com";
}