namespace JYDE.OpenDataCopilot.Application.Conversation;

/// <summary>
/// Dataset que el usuario mantiene FIJADO (seleccionado) en la memoria de la conversación. Se lleva
/// con su identificador para poder resolver su esquema (columnas) en el catálogo aunque la búsqueda
/// semántica no lo devuelva, y con su nombre para mostrarlo en el contexto.
/// </summary>
/// <param name="Id">Identificador del dataset (para resolver su esquema en el catálogo).</param>
/// <param name="Name">Nombre del dataset (para mostrarlo en el contexto de memoria).</param>
public sealed record SelectedDataset(string Id, string Name);
