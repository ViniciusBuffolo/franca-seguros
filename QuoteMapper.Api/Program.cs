using QuoteMapper.Api.Interfaces;
using QuoteMapper.Api.Services;
using QuoteMapper.Api.Services.Parsers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Core services
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IQuoteParserFactory, QuoteParserFactory>();

// Parsers
builder.Services.AddScoped<IQuoteParser, AllianzQuoteParser>();
builder.Services.AddScoped<IQuoteParser, PortoFamilyQuoteParser>();
builder.Services.AddScoped<IQuoteParser, TokioMarineQuoteParser>();
builder.Services.AddScoped<IQuoteParser, SuhaiQuoteParser>();

// Register FipeService with HttpClient support
builder.Services.AddHttpClient<IFipeService, FipeService>();

// Existing project services
builder.Services.AddScoped<IQuoteHtmlTemplateService, QuoteHtmlTemplateService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();