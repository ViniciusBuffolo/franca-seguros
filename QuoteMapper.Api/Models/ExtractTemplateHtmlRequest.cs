using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace QuoteMapper.Api.Models;

public sealed class ExtractTemplateHtmlRequest
{
    [Required]
    public IFormFile File { get; set; } = default!;

    [Required]
    public CoverageType CoverageType { get; set; }
    public string Insurer { get; set; } = string.Empty;
}