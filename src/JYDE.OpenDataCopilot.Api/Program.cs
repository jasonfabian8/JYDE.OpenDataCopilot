using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Mongo;
using JYDE.OpenDataCopilot.Infrastructure.Socrata;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Opciones del adaptador de Socrata (sección "Socrata" de appsettings; defaults si no existe).
SocrataCatalogOptions socrataOptions =
    builder.Configuration.GetSection(SocrataCatalogOptions.SectionName).Get<SocrataCatalogOptions>()
    ?? new SocrataCatalogOptions();
builder.Services.AddSingleton(socrataOptions);

// Composition root: selección de adaptadores por puerto (ver ADR-0003).
// Catálogo: fuente = Socrata.
// Nota (deuda técnica TD-001, ver docs/tech-debt.md): Catalog aún no entra en la matriz `Providers`
// del ADR-0003 (hay un único adaptador por puerto). La selección por configuración se añadirá al
// incorporar el segundo adaptador (el ADR contempla implementación incremental de los contratos).
builder.Services.AddHttpClient<ICatalogSource, SocrataCatalogClient>(
    client => client.Timeout = TimeSpan.FromSeconds(socrataOptions.TimeoutSeconds));

// Repositorio del catálogo: InMemory (por defecto) o Mongo (Atlas), según configuración.
string catalogRepository = builder.Configuration["Providers:CatalogRepository"] ?? "InMemory";
if (string.Equals(catalogRepository, "Mongo", StringComparison.OrdinalIgnoreCase))
{
    MongoOptions mongoOptions =
        builder.Configuration.GetSection(MongoOptions.SectionName).Get<MongoOptions>() ?? new MongoOptions();
    builder.Services.AddSingleton(mongoOptions);
    builder.Services.AddSingleton<ICatalogRepository, MongoCatalogRepository>();
}
else
{
    builder.Services.AddSingleton<ICatalogRepository, InMemoryCatalogRepository>();
}

builder.Services.AddTransient<IngestCatalogService>();
builder.Services.AddTransient<CatalogQueryService>();

WebApplication app = builder.Build();

app.MapControllers();

await app.RunAsync();
