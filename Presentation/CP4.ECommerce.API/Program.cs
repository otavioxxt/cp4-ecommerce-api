using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using CP4.ECommerce.Infrastructure.Data.AppData;
using CP4.ECommerce.Infrastructure.Data.Seed;
using CP4.ECommerce.Infrastructure.Ioc;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// LOGGING - Serilog no console e em arquivo, com rotacao diaria
// ---------------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "logs", "api-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Services.AddSerilog();

// ---------------------------------------------------------------------------
// TRACING E METRICAS - Application Insights
// O flag permite desligar a telemetria nos testes automatizados.
// ---------------------------------------------------------------------------
if (builder.Configuration.GetValue("ApplicationInsights:Habilitado", true))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        });
}

// ---------------------------------------------------------------------------
// INJECAO DE DEPENDENCIA - Bootstrap da camada de Infrastructure
// ---------------------------------------------------------------------------
builder.Services.ConfigureServices(builder.Configuration);

// ---------------------------------------------------------------------------
// COMPRESSAO DE DADOS - Brotli e Gzip
// ---------------------------------------------------------------------------
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});

// Nivel Fastest: prioriza a velocidade de resposta e o uso de CPU do
// servidor, em vez da maxima compactacao.
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// ---------------------------------------------------------------------------
// RATE LIMIT - janela fixa, contada por IP do cliente
//
// Cada IP tem a sua propria cota: o IP e a chave da particao, entao um cliente
// que estoura o limite nao atrapalha os outros.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("rateLimitePolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    options.AddPolicy("rateLimitePolicy2", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromSeconds(5),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ---------------------------------------------------------------------------
// HEALTH CHECKS - liveness (self) e readiness (banco Oracle)
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    // Liveness
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("API respondendo."),
        tags: new[] { "live" })
    // Readiness
    .AddOracle(
        connectionString: builder.Configuration.GetConnectionString("Oracle") ?? "",
        name: "oracle",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db" });

// ---------------------------------------------------------------------------
// MVC E SWAGGER
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CP4 - E-Commerce API",
        Version = "v1",
        Description = """
            API de **Produtos e Pedidos** - CP4 de *Advanced Business Development with .NET* (FIAP).

            Clean Architecture em quatro camadas, Repository Pattern, DTOs, Mappers,
            paginacao com indices, compressao de dados, rate limiting, health checks
            e logging estruturado.
            """
    });

    c.EnableAnnotations();
    c.ExampleFilters();

    var xml = Path.Combine(AppContext.BaseDirectory, "CP4.ECommerce.API.xml");
    if (File.Exists(xml))
        c.IncludeXmlComments(xml, includeControllerXmlComments: true);
});

// Registra as classes de exemplo (Doc/Samples)
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.UseResponseCompression();   // Habilitando a compressao
app.UseRateLimiter();           // Habilitando o Rate Limiter

app.MapControllers();

// Visao geral: roda todos os checks registrados (self + Oracle).
app.MapHealthChecks("/health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// ---------------------------------------------------------------------------
// BANCO - cria as tabelas e popula os dados de demonstracao
// ---------------------------------------------------------------------------
if (builder.Configuration.GetValue("Database:InicializarNaSubida", true))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

    var tabelasDoProjeto = new[] { "tb_item_pedido", "tb_pedido", "tb_produto" };

    try
    {
        // Quais tabelas do projeto ja existem no schema. O EnsureCreated nao
        // serve aqui: ele so cria o schema quando o banco esta totalmente
        // vazio, e o schema da FIAP ja tem tabelas de outros trabalhos.
        var existentes = await context.Database
            .SqlQueryRaw<string>(
                "SELECT table_name AS \"Value\" FROM user_tables " +
                "WHERE table_name IN ('tb_item_pedido', 'tb_pedido', 'tb_produto')")
            .ToListAsync();

        var recriar = builder.Configuration.GetValue("Database:RecriarTabelas", false);
        var incompleto = existentes.Count > 0 && existentes.Count < tabelasDoProjeto.Length;

        if (recriar || incompleto)
        {
            foreach (var tabela in tabelasDoProjeto.Where(existentes.Contains))
            {
                await context.Database.ExecuteSqlRawAsync(
                    "DROP TABLE \"" + tabela + "\" CASCADE CONSTRAINTS");

                Log.Warning("Tabela {Tabela} removida.", tabela);
            }

            existentes.Clear();
        }

        if (existentes.Count == 0)
        {
            await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();

            Log.Information("Tabelas criadas no banco de dados.");
        }

        await DataSeeder.SeedAsync(context);

        Log.Information("Banco de dados inicializado com sucesso.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Falha ao inicializar o banco de dados. A API sobe mesmo assim.");
    }
}

Log.Information("CP4 E-Commerce API iniciada no ambiente {Ambiente}", app.Environment.EnvironmentName);

app.Run();

/// <summary>
/// Exposta para os testes funcionais usarem WebApplicationFactory&lt;Program&gt;.
/// </summary>
public partial class Program
{
}
