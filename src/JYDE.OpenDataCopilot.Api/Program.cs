using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Application.Figures;
using JYDE.OpenDataCopilot.Application.Search;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Chat;
using JYDE.OpenDataCopilot.Infrastructure.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Embeddings;
using JYDE.OpenDataCopilot.Infrastructure.Foundry;
using JYDE.OpenDataCopilot.Infrastructure.Mongo;
using JYDE.OpenDataCopilot.Infrastructure.Search;
using JYDE.OpenDataCopilot.Infrastructure.Socrata;
using Microsoft.AspNetCore.Cors.Infrastructure;

const string WebCorsPolicy = "web";
const string CorsAllowedOriginsSection = "Cors:AllowedOrigins";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS para el frontend: en Development se acepta cualquier origen (desarrollo local); fuera de
// Development se restringe a los orígenes declarados en `Cors:AllowedOrigins` (ver ConfigureCorsPolicy).
string[] corsAllowedOrigins =
    builder.Configuration.GetSection(CorsAllowedOriginsSection).Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(
    WebCorsPolicy,
    policy => ConfigureCorsPolicy(policy, builder.Environment, corsAllowedOrigins)));

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
string conversationStore = builder.Configuration["Providers:ConversationStore"] ?? "InMemory";

// Cliente Mongo ÚNICO compartido por los adaptadores Mongo (el cliente gestiona el pool).
if (AnyMongo(catalogRepository, searchIndex, conversationStore))
{
    builder.Services.AddSingleton<MongoContext>();
}

// Opciones de Foundry (compartidas por chat y embeddings); se registran una sola vez.
if (AnyFoundry(embeddingsProvider, chatProvider))
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
builder.Services.AddTransient<ListCatalogCategoriesService>();

// Cifras: consulta de datos reales (SoQL) vía la API SODA de Socrata.
builder.Services.AddHttpClient<IDataQuery, SocrataDataQuery>(
    client => client.Timeout = TimeSpan.FromSeconds(socrataOptions.TimeoutSeconds));

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

// Archivo de conversaciones (persistencia, ver ADR-0017): InMemory (por defecto, $0) o Mongo (Atlas),
// elegible por `Providers:ConversationStore`. El guardado es manual (lo dispara el usuario).
if (IsMongo(conversationStore))
{
    builder.Services.AddSingleton<IConversationStore, MongoConversationStore>();
}
else
{
    builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddTransient<ConversationArchiveService>();

// Conversación (Copilot multiagente, ver ADR-0015). LLM: Fake ($0) o Foundry (agentes publicados),
// envuelto por un decorador de auditoría que registra cada interacción (para el panel de auditoría).
// El grabador y el chat auditado son SCOPED (por petición): el orquestador (iterador asíncrono) y el
// decorador comparten la misma instancia del turno. AsyncLocal no sirve porque no sobrevive los yield.
builder.Services.AddScoped<IInteractionRecorder, InteractionRecorder>();
if (IsFoundry(chatProvider))
{
    builder.Services.AddHttpClient<FoundryChatCompletion>();
    builder.Services.AddScoped<IChatCompletion>(provider => new AuditingChatCompletion(
        provider.GetRequiredService<FoundryChatCompletion>(),
        provider.GetRequiredService<IInteractionRecorder>()));
}
else
{
    builder.Services.AddSingleton<FakeChatCompletion>();
    builder.Services.AddScoped<IChatCompletion>(provider => new AuditingChatCompletion(
        provider.GetRequiredService<FakeChatCompletion>(),
        provider.GetRequiredService<IInteractionRecorder>()));
}

// Agente de categorías: recomienda qué categorías cargar. Se registra PRIMERO para que, en la
// reserva por reglas, capture las consultas de intención "categorías/cargar".
double categoryRelevanceThreshold =
    builder.Configuration.GetValue<double?>("Conversation:CategoryRelevanceThreshold")
    ?? CategoryRecommenderAgent.DefaultRelevanceThreshold;
builder.Services.AddScoped<IConversationAgent>(provider => new CategoryRecommenderAgent(
    provider.GetRequiredService<ICatalogSource>(),
    provider.GetRequiredService<ICatalogRepository>(),
    provider.GetRequiredService<IChatCompletion>(),
    categoryRelevanceThreshold));

// Umbral de relevancia RECALCULADA por el LLM (configurable), compartido por analista y recomendador.
double relevanceThreshold =
    builder.Configuration.GetValue<double?>("Search:RelevanceThreshold")
    ?? DatasetRecommenderAgent.DefaultRelevanceThreshold;

// Agente analista: describe columnas y evalúa cruces/correlaciones (metadatos del catálogo). Se
// registra antes del recomendador para que la reserva por reglas capture "columnas/cruzar/correlación".
builder.Services.AddScoped<IConversationAgent>(provider => new DatasetAnalystAgent(
    provider.GetRequiredService<IEmbeddingGenerator>(),
    provider.GetRequiredService<IDatasetSearchIndex>(),
    provider.GetRequiredService<ICatalogRepository>(),
    provider.GetRequiredService<IChatCompletion>(),
    relevanceThreshold));

// Agente de cifras: consulta datos reales (SoQL) para tabular y graficar. Antes del recomendador.
builder.Services.AddScoped<IConversationAgent>(provider => new FiguresAgent(
    provider.GetRequiredService<IEmbeddingGenerator>(),
    provider.GetRequiredService<IDatasetSearchIndex>(),
    provider.GetRequiredService<ICatalogRepository>(),
    provider.GetRequiredService<IChatCompletion>(),
    provider.GetRequiredService<IDataQuery>()));

// Agente recomendador de datasets: recomienda entre candidatos del índice.
builder.Services.AddScoped<IConversationAgent>(provider => new DatasetRecommenderAgent(
    provider.GetRequiredService<IEmbeddingGenerator>(),
    provider.GetRequiredService<IDatasetSearchIndex>(),
    provider.GetRequiredService<IChatCompletion>(),
    relevanceThreshold));

// Enrutador: basado en LLM cuando el chat es Foundry (agente enrutador publicado); por reglas en
// local/Fake. El enrutador LLM degrada a reglas si el agente no está disponible.
if (IsFoundry(chatProvider))
{
    string routerAgent = builder.Configuration["Conversation:RouterAgent"] ?? LlmAgentRouter.DefaultRouterAgent;
    builder.Services.AddScoped<IAgentRouter>(provider =>
        new LlmAgentRouter(provider.GetRequiredService<IChatCompletion>(), routerAgent));
}
else
{
    builder.Services.AddScoped<IAgentRouter, DefaultAgentRouter>();
}

// Rastreador del objetivo de la conversación (memoria): resume el propósito para no perder el hilo.
string objectiveAgent = builder.Configuration["Conversation:ObjectiveAgent"] ?? ObjectiveTracker.DefaultAgent;
builder.Services.AddScoped(provider =>
    new ObjectiveTracker(provider.GetRequiredService<IChatCompletion>(), objectiveAgent));

builder.Services.AddScoped<CopilotOrchestrator>();

WebApplication app = builder.Build();

app.UseCors(WebCorsPolicy);
app.MapControllers();

await app.RunAsync();

static bool IsMongo(string provider) => string.Equals(provider, "Mongo", StringComparison.OrdinalIgnoreCase);

static bool IsFoundry(string provider) => string.Equals(provider, "Foundry", StringComparison.OrdinalIgnoreCase);

static bool AnyMongo(params string[] providers) => Array.Exists(providers, IsMongo);

static bool AnyFoundry(params string[] providers) => Array.Exists(providers, IsFoundry);

// En Development se acepta cualquier origen (desarrollo local); fuera de Development sólo los
// orígenes declarados en configuración. AllowAnyOrigin no admite credenciales, coherente con el
// frontend (peticiones sin cookies).
static void ConfigureCorsPolicy(
    CorsPolicyBuilder policy, IWebHostEnvironment environment, string[] allowedOrigins)
{
    policy.AllowAnyHeader().AllowAnyMethod();
    if (environment.IsDevelopment())
    {
        policy.AllowAnyOrigin();
        Console.WriteLine("CORS: Development mode, allowing any origin.");
    }
    else
    {
        policy.WithOrigins(allowedOrigins);
    }
}
