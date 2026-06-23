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
builder.Services.AddHttpClient<ICatalogSource, SocrataCatalogClient>();

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

app.Run();
