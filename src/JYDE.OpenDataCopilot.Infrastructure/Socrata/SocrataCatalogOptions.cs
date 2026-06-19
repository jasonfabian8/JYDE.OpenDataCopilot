namespace JYDE.OpenDataCopilot.Infrastructure.Socrata;

/// <summary>Opciones de configuración del adaptador de catálogo de Socrata.</summary>
public sealed class SocrataCatalogOptions
{
    /// <summary>Clave de configuración asociada a estas opciones.</summary>
    public const string SectionName = "Socrata";

    /// <summary>URL base del portal Socrata (por defecto <c>https://www.datos.gov.co</c>).</summary>
    public Uri BaseAddress { get; set; } = new("https://www.datos.gov.co");

    /// <summary>Tamaño de página para paginar el catálogo (por defecto 1000).</summary>
    public int PageSize { get; set; } = 1000;

    /// <summary>App Token opcional de Socrata para elevar los límites de tasa (cabecera X-App-Token).</summary>
    public string? AppToken { get; set; }
}
