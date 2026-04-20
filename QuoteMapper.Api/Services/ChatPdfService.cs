using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MyPdfApi.Models;

namespace MyPdfApi.Services;

public sealed class ChatPdfService : IChatPdfService
{
    private readonly HttpClient _httpClient;
    private readonly ChatPdfOptions _options;

    public ChatPdfService(HttpClient httpClient, IOptions<ChatPdfOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
    }

    public async Task<string> UploadPdfAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("No PDF file was provided.");

        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var streamContent = new StreamContent(stream);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/pdf");
        form.Add(streamContent, "file", file.FileName);

        using var response = await _httpClient.PostAsync("/v1/sources/add-file", form, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ChatPDF upload failed: {content}");

        using var json = JsonDocument.Parse(content);

        if (!json.RootElement.TryGetProperty("sourceId", out var sourceIdElement))
            throw new InvalidOperationException("ChatPDF did not return sourceId.");

        var sourceId = sourceIdElement.GetString();
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new InvalidOperationException("ChatPDF returned an empty sourceId.");

        return sourceId!;
    }

    public async Task<QuoteTemplateExtractionResult> ExtractTemplateFieldsAsync(
        string sourceId,
        string extractionPrompt,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            sourceId,
            referenceSources = true,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = extractionPrompt
                }
            }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        using var stringContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("/v1/chats/message", stringContent, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ChatPDF extraction failed: {responseText}");

        using var json = JsonDocument.Parse(responseText);

        var rawContent = json.RootElement.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString() ?? string.Empty
            : string.Empty;

        var pages = new List<int>();
        if (json.RootElement.TryGetProperty("references", out var referencesElement) &&
            referencesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in referencesElement.EnumerateArray())
            {
                if (item.TryGetProperty("pageNumber", out var pageNumberElement) &&
                    pageNumberElement.TryGetInt32(out var page))
                {
                    pages.Add(page);
                }
            }
        }

        return ParseTemplateFields(rawContent, pages);
    }

    private static QuoteTemplateExtractionResult ParseTemplateFields(string rawResponse, List<int> pages)
    {
        try
        {
            var cleanJson = CleanupJson(rawResponse);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<QuoteTemplateExtractionResult>(cleanJson, options);

            if (result == null)
                throw new InvalidOperationException("Could not deserialize ChatPDF JSON.");

            result.PaginasReferencia = pages;
            result.RawResponse = rawResponse;
            result.PaymentRows ??= new List<PaymentRowData>();

            return result;
        }
        catch
        {
            return new QuoteTemplateExtractionResult
            {
                PaginasReferencia = pages,
                RawResponse = rawResponse
            };
        }
    }

    private static string CleanupJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "{}";

        var cleaned = raw.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[7..].Trim();

        if (cleaned.StartsWith("```"))
            cleaned = cleaned[3..].Trim();

        if (cleaned.EndsWith("```"))
            cleaned = cleaned[..^3].Trim();

        return cleaned;
    }
}