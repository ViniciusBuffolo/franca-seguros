using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Models;
using QuoteMapper.Api.Services;
using QuoteMapper.Api.Services.Parsers;
using System.Text.Json.Serialization;

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

// PromptBuilders
builder.Services.AddScoped<QuotePromptBuilderFactory>();
builder.Services.AddScoped<IQuotePromptBuilder, AllianzPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, AzulPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, BanestesPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, BradescoPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, DarwinPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, ItauPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, JustosPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, MapfrePromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, MitsuiPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, PortoPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, SuhaiPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, TokioMarinePromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, YelumPromptBuilder>();
builder.Services.AddScoped<IQuotePromptBuilder, ZurichPromptBuilder>();

// External services
builder.Services.AddHttpClient<IFipeService, FipeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuoteMapper API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("Frontend");

// Serve wwwroot (index.html, css, js, etc)
app.UseDefaultFiles();
app.UseStaticFiles();

// Serve Templates/Logos as /logos
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
