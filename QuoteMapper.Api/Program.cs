using Microsoft.OpenApi.Models;
using MyPdfApi.Models;
using MyPdfApi.Services;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Services;
using QuoteMapper.Api.Services.Parsers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuoteMapper API",
        Version = "v1"
    });
});

// Core services
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IQuoteParserFactory, QuoteParserFactory>();

// Parsers
builder.Services.AddScoped<IQuoteParser, AllianzQuoteParser>();
builder.Services.AddScoped<IQuoteParser, PortoFamilyQuoteParser>();
builder.Services.AddScoped<IQuoteParser, TokioMarineQuoteParser>();
builder.Services.AddScoped<IQuoteParser, SuhaiQuoteParser>();

// Existing project services
builder.Services.AddScoped<IQuoteHtmlTemplateService, QuoteHtmlTemplateService>();

// New template services
builder.Services.AddScoped<IQuoteTemplateMapper, QuoteTemplateMapper>();
builder.Services.AddScoped<IQuoteTemplateRenderService, QuoteTemplateRenderService>();

// ChatPDF
builder.Services.Configure<ChatPdfOptions>(
    builder.Configuration.GetSection("ChatPdf"));

builder.Services.AddHttpClient<IChatPdfService, ChatPdfService>();

// External services
builder.Services.AddHttpClient<IFipeService, FipeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuoteMapper API V1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors("Frontend");

var logosPath = Path.Combine(app.Environment.ContentRootPath, "Templates", "Logos");

if (Directory.Exists(logosPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(logosPath),
        RequestPath = "/logos"
    });
}

app.UseAuthorization();
app.MapControllers();
app.Run();