using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Embeddings;
using JYDE.OpenDataCopilot.Infrastructure.Mongo;
using JYDE.OpenDataCopilot.Infrastructure.Search;
using JYDE.OpenDataCopilot.Infrastructure.Socrata;

const string WebCorsPolicy = "web";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS para el frontend (en desarrollo se usa además un proxy de Vite).
builder.Services.AddCors(options => options.AddPolicy(
    WebCorsPolicy,
    policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Opciones compartidas.
SocrataCatalogOptions socrataOptions =
    builder.Configuration.GetSection(SocrataCatalogOptions.SectionName).Get<SocrataCatalogOptions>()
    ?? new SocrataCatalogOptions();
builder.Services.AddSingleton(socrataOptions);

MongoOptions mongoOptions =
    builder.Configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>() ?? new MongoOptions();
builder.Services.AddSingleton(mongoOptions);

// Composition root: selección de adaptadores por puerto (ver ADR-0003).
// Catálogo: fuente = Socrata.
builder.Services.AddHttpClient<ICatalogSource, SocrataCatalogClient>();

// Repositorio del catálogo: InMemory (por defecto) o Mongo (Atlas).
string catalogRepository = builder.Configuration["Providers:CatalogRepository"] ?? "InMemory";
if (IsMongo(catalogRepository))
{
    builder.Services.AddSingleton<ICatalogRepository, MongoCatalogRepository>();
}
else
{
    builder.Services.AddSingleton<ICatalogRepository, InMemoryCatalogRepository>();
}

builder.Services.AddTransient<IngestCatalogService>();
builder.Services.AddTransient<CatalogQueryService>();

// Search: embeddings (Local por ahora; Foundry pendiente) + índice (InMemory o Atlas Vector Search).
// La dimensión del embedding local se ata a la del índice vectorial para que siempre concuerden.
builder.Services.AddSingleton<IEmbeddingGenerator>(_ => new LocalHashingEmbeddingGenerator(mongoOptions.VectorDimensions));

string searchIndex = builder.Configuration["Providers:SearchIndex"] ?? "InMemory";
if (IsMongo(searchIndex))
{
    builder.Services.AddSingleton<IDatasetSearchIndex, MongoDatasetSearchIndex>();
}
else
{
    builder.Services.AddSingleton<IDatasetSearchIndex, InMemorySearchIndex>();
}

builder.Services.AddTransient<IndexCatalogService>();
builder.Services.AddTransient<SearchDatasetsService>();

WebApplication app = builder.Build();

app.UseCors(WebCorsPolicy);
app.MapControllers();

app.Run();

static bool IsMongo(string provider) => string.Equals(provider, "Mongo", StringComparison.OrdinalIgnoreCase);
