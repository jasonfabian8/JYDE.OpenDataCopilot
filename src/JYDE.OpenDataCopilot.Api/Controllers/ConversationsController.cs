using JYDE.OpenDataCopilot.Application.Conversation;
using Microsoft.AspNetCore.Mvc;

namespace JYDE.OpenDataCopilot.Api.Controllers;

/// <summary>
/// Endpoints HTTP para archivar conversaciones (transcripción + memoria + artefactos + auditoría):
/// listar, recuperar, guardar (upsert) y eliminar. El guardado es manual (lo dispara el usuario).
/// </summary>
[ApiController]
[Route("conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly ConversationArchiveService _archive;

    /// <summary>Crea el controlador de conversaciones.</summary>
    /// <param name="archive">Caso de uso de archivo de conversaciones.</param>
    public ConversationsController(ConversationArchiveService archive)
    {
        ArgumentNullException.ThrowIfNull(archive);
        _archive = archive;
    }

    /// <summary>Lista los resúmenes de las conversaciones guardadas (más reciente primero).</summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<ConversationSummary> summaries = await _archive.ListAsync(cancellationToken);
        return Ok(summaries);
    }

    /// <summary>Recupera una conversación completa por su id.</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("El id no puede estar vacío.");
        }

        ConversationRecord? conversation = await _archive.GetAsync(id, cancellationToken);
        return conversation is null ? NotFound() : Ok(conversation);
    }

    /// <summary>Guarda (inserta o reemplaza) una conversación. El id de la ruta es el autoritativo.</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="conversation">Conversación completa a persistir.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> Save(string id, [FromBody] ConversationRecord? conversation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("El id no puede estar vacío.");
        }

        if (conversation is null)
        {
            return BadRequest("El cuerpo de la conversación es obligatorio.");
        }

        await _archive.SaveAsync(conversation with { Id = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Elimina una conversación completa (transcripción + memoria + artefactos + auditoría).</summary>
    /// <param name="id">Identificador de la conversación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("El id no puede estar vacío.");
        }

        await _archive.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
