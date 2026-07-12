# Conversaciones: transcripción, memoria, artefactos y auditoría (colección `conversations`)

Persistencia de la unidad completa de una conversación con el Copilot: los mensajes, la memoria
(objetivo + datasets fijados), los artefactos generados (tablas/gráficos) y la bitácora de
auditoría de los agentes. El guardado es **manual** (decisión de producto: lo dispara el usuario)
y eliminar borra todo el rastro.

> Fuente: `src/JYDE.OpenDataCopilot.Application/Conversation/Conversation*.cs` (records),
> `src/JYDE.OpenDataCopilot.Infrastructure/Mongo/ConversationDocument.cs` (persistencia),
> [ADR-0017](../docs/adr/0017-persistencia-conversaciones.md).
> Adaptadores del puerto `IConversationStore`: `InMemoryConversationStore` (dev, $0) y
> `MongoConversationStore` (Atlas), por `Providers:ConversationStore`.

## Documento Mongo (`ConversationDocument`)

Cabecera consultable + payload completo. El interior no se consulta en Mongo (sólo se lista y se
recupera completo), por eso la conversación viaja como JSON:

| Campo | Tipo | Descripción |
|---|---|---|
| `_id` | string | Id de la conversación. |
| `Title` | string | Título (consultable, para listar sin abrir el payload). |
| `UpdatedAtUtc` | DateTime | Última actualización (UTC) — **la sella el servidor** al guardar; ordena la lista. |
| `Payload` | string (JSON) | La conversación completa (`ConversationRecord`) serializada. |

## Payload (`ConversationRecord`)

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | string | Identificador de la conversación. |
| `title` | string | Título mostrado en la barra lateral. |
| `threadId` | string? | Id del hilo del proveedor de chat (threading en Foundry); nulo si es nuevo. |
| `messages` | `ConversationMessageRecord[]` | Transcripción (ver abajo). |
| `objective` | string | **Memoria**: objetivo acumulado, resumido por el `objective-tracker-agent` y editable por el usuario. |
| `selectedDatasets` | `SelectedDataset[]` | **Memoria**: datasets (id + nombre) fijados por el usuario. |
| `artifacts` | `ConversationArtifactRecord[]` | Tablas y gráficos generados (ver abajo). |
| `auditLog` | `ConversationAuditEntryRecord[]` | **Auditoría** por turno (ver abajo). |
| `updatedAtUtc` | DateTimeOffset | Marca de última actualización. |

### Transcripción — `ConversationMessageRecord`

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | string | Id estable del mensaje. |
| `role` | string | `user` o `assistant`. |
| `content` | string | Texto del mensaje. |
| `agent` | string? | Agente que respondió (solo mensajes del asistente). |
| `sources` | `Citation[]?` | Fuentes citadas: `datasetId`, `name`, `sourceUrl`, `score` (solo asistente). |

### Artefactos — `ConversationArtifactRecord`

Llevan sus **datos incrustados** (columnas y filas) para redibujarse al recuperar la conversación
sin volver a consultar la fuente:

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | string | Id estable del artefacto. |
| `kind` | string | `table` o `chart`. |
| `title` | string | Título. |
| `columns` | string[] | Columnas de los datos. |
| `rows` | string[][] | Filas (celdas de texto). |
| `type` | string? | Tipo de gráfico (`bar`/`line`); nulo en tablas. |
| `xColumn` / `yColumn` | string? | Ejes del gráfico (solo `chart`). |

### Auditoría — `ConversationAuditEntryRecord`

Bitácora cruda por turno: qué se le envió a cada agente y qué respondió, tal cual. Es la base de
la trazabilidad y del ciclo de mejora (refinar prompts/agentes con uso real):

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | string | Id estable de la entrada (un turno). |
| `userMessage` | string | Mensaje del usuario que originó el turno. |
| `interactions` | `AgentInteraction[]` | Por agente invocado: `agent`, `request` (input tal cual), `response` (texto crudo). |

La captura la hace `AuditingChatCompletion` (decorador del puerto `IChatCompletion`) +
`InteractionRecorder` durante el turno; el orquestador la emite como evento SSE `audit` y el
cliente la incluye al guardar.

## Memoria conversacional (cómo se usa)

- El **objetivo** se actualiza en cada turno (`ObjectiveTracker` → evento `objective`) y viaja de
  vuelta en el siguiente request (`ChatRequest.objective`).
- Los **datasets fijados** (`selectedDatasets`) se anteponen al input de los agentes
  (`ContextHeader`) para que analista y cifras siempre consideren lo que el usuario eligió.
- El backend permanece **sin estado**: la memoria viaja con el request y se persiste sólo al
  guardar la conversación.

## Operaciones (API)

`GET /conversations` (lista `ConversationSummary`: id, título, fecha) ·
`GET /conversations/{id}` · `PUT /conversations/{id}` (upsert) · `DELETE /conversations/{id}`
— ver [`docs/api_spec.md`](../docs/api_spec.md).
