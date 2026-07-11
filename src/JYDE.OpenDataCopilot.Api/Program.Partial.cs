using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Punto de entrada de la aplicación (composition root). Se excluye de la cobertura por ser
/// arranque/configuración; su comportamiento se valida con pruebas de integración cuando aplique.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program;
