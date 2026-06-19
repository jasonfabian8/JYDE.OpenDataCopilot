using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Socrata;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Opciones del adaptador de Socrata (sección "Socrata" de appsettings; defaults si no existe).
SocrataCatalogOptions socrataOptions =
    builder.Configuration.GetSection(SocrataCatalogOptions.SectionName).Get<SocrataCatalogOptions>()
    ?? new SocrataCatalogOptions();
builder.Services.AddSingleton(socrataOptions);

// Composition root: selección de adaptadores por puerto (ver ADR-0003).
// Catálogo: fuente = Socrata; repositorio = en memoria (desarrollo/demo, singleton).
builder.Services.AddHttpClient<ICatalogSource, SocrataCatalogClient>();
builder.Services.AddSingleton<ICatalogRepository, InMemoryCatalogRepository>();
builder.Services.AddTransient<IngestCatalogService>();
builder.Services.AddTransient<CatalogQueryService>();

WebApplication app = builder.Build();

app.MapControllers();

app.Run();
