using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Chat;
using JYDE.OpenDataCopilot.Infrastructure.Embeddings;
using JYDE.OpenDataCopilot.Infrastructure.Foundry;
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

// Proveedores seleccionados por configuración (ver ADR-0003).
string catalogRepository = builder.Configuration["Providers:CatalogRepository"] ?? "InMemory";
string searchIndex = builder.Configuration["Providers:SearchIndex"] ?? "InMemory";
string embeddingsProvider = builder.Configuration["Providers:Embeddings"] ?? "Local";
string chatProvider = builder.Configuration["Providers:Chat"] ?? "Fake";

// Cliente Mongo ÚNICO compartido por los adaptadores Mongo (el cliente gestiona el pool).
if (IsMongo(catalogRepository) || IsMongo(searchIndex))
{
    builder.Services.AddSingleton<MongoContext>();
}

// Opciones de Foundry (compartidas por chat y embeddings); se registran una sola vez.
if (IsFoundry(embeddingsProvider) || IsFoundry(chatProvider))
{
    FoundryOptions foundryOptions =
        builder.Configuration.GetSection(FoundryOptions.SectionName).Get<FoundryOptions>() ?? new FoundryOptions();
    builder.Services.AddSingleton(foundryOptions);
}

// Catálogo: fuente = Socrata.
// Nota (deuda técnica TD-001, ver docs/tech-debt.md): Catalog aún no entra en la matriz `Providers`
// del ADR-0003 (hay un único adaptador por puerto). La selección por configuración se añadirá al
// incorporar el segundo adaptador (el ADR contempla implementación incremental de los contratos).
builder.Services.AddHttpClient<ICatalogSource, SocrataCatalogClient>(
    client => client.Timeout = TimeSpan.FromSeconds(socrataOptions.TimeoutSeconds));

// Catálogo: repositorio = InMemory (por defecto) o Mongo (Atlas).
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

// Search: embeddings = Local (por defecto, $0) o Foundry (Azure AI Foundry).
if (IsFoundry(embeddingsProvider))
{
    builder.Services.AddHttpClient<IEmbeddingGenerator, FoundryEmbeddingGenerator>();
}
else
{
    builder.Services.AddSingleton<IEmbeddingGenerator>(_ => new LocalHashingEmbeddingGenerator(mongoOptions.VectorDimensions));
}

// Search: índice = InMemory (por defecto) o Atlas Vector Search.
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

// Conversación (Copilot multiagente, ver ADR-0015). LLM: Fake ($0) o Foundry (agentes publicados).
if (IsFoundry(chatProvider))
{
    builder.Services.AddHttpClient<IChatCompletion, FoundryChatCompletion>();
}
else
{
    builder.Services.AddSingleton<IChatCompletion, FakeChatCompletion>();
}

builder.Services.AddSingleton<IConversationAgent, DatasetRecommenderAgent>();
builder.Services.AddSingleton<IAgentRouter, DefaultAgentRouter>();
builder.Services.AddTransient<CopilotOrchestrator>();

WebApplication app = builder.Build();

app.UseCors(WebCorsPolicy);
app.MapControllers();

await app.RunAsync();

static bool IsMongo(string provider) => string.Equals(provider, "Mongo", StringComparison.OrdinalIgnoreCase);

static bool IsFoundry(string provider) => string.Equals(provider, "Foundry", StringComparison.OrdinalIgnoreCase);
