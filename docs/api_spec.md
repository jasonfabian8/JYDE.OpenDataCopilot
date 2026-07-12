# Especificación de la API

> Fuente: controladores reales en `src/JYDE.OpenDataCopilot.Api/Controllers/`
> ([ADR-0010](adr/0010-api-con-controladores.md): controladores MVC;
> [ADR-0011](adr/0011-api-no-referencia-dominio.md): la API consume DTOs de Application).
> Formato: JSON/REST + **SSE** para el chat. Sin autenticación aún (alcance de concurso; ver
> [ADR-0017 §Consecuencias](adr/0017-persistencia-conversaciones.md)).

## Índice

| Método | Ruta | Propósito |
|---|---|---|
| GET | `/` | Verificación de vida de la API |
| POST | `/catalog/ingest` | Ingerir el catálogo (todo o por categorías) |
| GET | `/catalog/count` | Cantidad de datasets almacenados |
| GET | `/catalog/categories` | Categorías del catálogo con su conteo |
| GET | `/catalog/{id}` | Dataset por identificador 4x4 |
| POST | `/search/index` | Construir el índice de búsqueda |
| GET | `/search?q=&top=` | Búsqueda semántica de datasets |
| POST | `/chat` | Conversar con el Copilot (respuesta SSE) |
| GET | `/conversations` | Listar conversaciones guardadas |
| GET | `/conversations/{id}` | Recuperar una conversación completa |
| PUT | `/conversations/{id}` | Guardar (upsert) una conversación |
| DELETE | `/conversations/{id}` | Eliminar una conversación completa |

---

## Raíz

### `GET /`
Mensaje de bienvenida / verificación rápida de que la API responde.

**200 OK** — `"OpenData Copilot API"`

---

## Catalog (`CatalogController`)

### `POST /catalog/ingest`
Ingiere el catálogo de datasets desde la fuente configurada (Socrata). Cuerpo vacío = todo el
catálogo. El request se sanea en la frontera (límite ≤ 10.000, categorías ≤ 50).

```jsonc
// Request (opcional)
{
  "categories": ["Salud y Protección Social", "Educación"],  // nula/vacía = todas
  "limit": 500                                               // nulo = sin límite
}
```

**200 OK** — resumen con la cantidad de datasets ingeridos (`IngestCatalogResult`).

### `GET /catalog/count`
**200 OK** — `{ "count": 1234 }`

### `GET /catalog/categories`
Lista las categorías temáticas del catálogo (con su conteo), para acotar la ingesta.

**200 OK** — lista de `CatalogCategory` (nombre + conteo).

### `GET /catalog/{id}`
Obtiene un dataset por su identificador 4x4 (p. ej. `ddau-8cy9`).

**200 OK** — `DatasetDto` · **400** id inválido · **404** no existe.

---

## Search (`SearchController`)

### `POST /search/index`
Construye el índice de búsqueda (embeddings) a partir del catálogo almacenado.

**200 OK** — `{ "indexed": 1234 }`

### `GET /search?q={consulta}&top={n}`
Busca los datasets más relevantes para una consulta en lenguaje natural (`top` por defecto: 5).

**200 OK** — lista de `DatasetSearchHit` (id, nombre, categoría, fuente, score) · **400** consulta inválida.

---

## Chat (`ChatController`) — streaming SSE

### `POST /chat`
Conversa con el Copilot multiagente. La respuesta es un flujo `text/event-stream` (UTF-8).

```jsonc
// Request
{
  "question": "¿Qué municipios tienen mayor desempleo?",  // obligatoria
  "top": 5,                          // opcional: nº máx. de datasets a considerar
  "conversationId": "resp_abc123",   // opcional: id del turno anterior (hilo)
  "objective": "…",                  // opcional: objetivo acumulado (memoria)
  "selectedDatasets": [               // opcional: datasets fijados por el usuario
    { "id": "ddau-8cy9", "name": "…" }
  ],
  "context": "…"                     // opcional: respuesta anterior, para desambiguar
}
```

**400** — pregunta vacía. **200** — stream de eventos, en este orden típico
(`agent` → `sources`/`categories`/`table`/`chart` → `token`… → `conversation` → `objective` → `audit` → `done`):

| Evento | Payload | Significado |
|---|---|---|
| `agent` | `{ "agent": "dataset-recommender-agent" }` | Agente que atiende el turno |
| `sources` | `{ "sources": [ { "datasetId", "name", "sourceUrl", "score" } ] }` | Fuentes citadas (solo las relevantes) |
| `categories` | `{ "query": "…", "categories": [ … ] }` | Categorías recomendadas a cargar (botones) |
| `table` | `{ "table": { "title", "columns", "rows" } }` | Artefacto de tabla con datos reales |
| `chart` | `{ "chart": { "title", "type", "xColumn", "yColumn", "columns", "rows" } }` | Artefacto de gráfico (`bar`/`line`) |
| `token` | `{ "text": "…" }` | Fragmento de la respuesta (streaming) |
| `conversation` | `{ "conversationId": "…" }` | Id para continuar el hilo en el siguiente turno |
| `objective` | `{ "objective": "…" }` | Objetivo actualizado (memoria) |
| `audit` | `{ "interactions": [ { "agent", "request", "response" } ] }` | Interacciones crudas del turno (auditoría) |
| `done` | `{}` | Fin del turno |

---

## Conversations (`ConversationsController`)

Persistencia de conversaciones: transcripción + memoria + artefactos + auditoría
([ADR-0017](adr/0017-persistencia-conversaciones.md)). El guardado es **manual** (lo dispara el
usuario).

### `GET /conversations`
**200 OK** — lista de `ConversationSummary` (`id`, `title`, `updatedAtUtc`), más reciente primero.

### `GET /conversations/{id}`
**200 OK** — `ConversationRecord` completo · **400** id vacío · **404** no existe.

### `PUT /conversations/{id}`
Guarda (inserta o reemplaza) una conversación; el id de la ruta es el autoritativo y el servidor
sella `updatedAtUtc`.

```jsonc
// Request: ConversationRecord
{
  "id": "…", "title": "…", "threadId": "…",
  "messages":  [ { "id", "role", "content", "agent", "sources" } ],
  "objective": "…",
  "selectedDatasets": [ { "id", "name" } ],
  "artifacts": [ { "id", "kind", "title", "columns", "rows", "type", "xColumn", "yColumn" } ],
  "auditLog":  [ { "id", "userMessage", "interactions" } ]
}
```

**204 No Content** · **400** id o cuerpo inválidos.

### `DELETE /conversations/{id}`
Elimina la conversación completa (incluida memoria/artefactos/auditoría).

**204 No Content** · **400** id vacío.

---

## Notas transversales

- **Estructuras de datos persistidas**: ver [`data/README.md`](../data/README.md) y
  [`data_dictionary.md`](data_dictionary.md).
- **Selección de proveedores** (chat, embeddings, índice, repositorio, store) por
  `appsettings → Providers` ([ADR-0003](adr/0003-ports-adapters-intercambiables.md)).
- **Swagger/OpenAPI generado**: no está habilitado aún; esta especificación se mantiene a mano
  desde los controladores (pendiente registrado en [`tech-debt.md`](tech-debt.md)).
