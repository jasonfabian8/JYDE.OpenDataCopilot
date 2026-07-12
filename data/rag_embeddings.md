# Estructura RAG: embeddings e índice vectorial (colección `dataset_vectors`)

Soporte de la **búsqueda semántica** (retrieval del RAG): cada dataset del catálogo se representa
como un vector (embedding de sus metadatos) y se recupera por similitud coseno para darle
contexto al LLM. El LLM responde **sólo** con datasets recuperados de este índice.

> Fuente: `src/JYDE.OpenDataCopilot.Infrastructure/Mongo/DatasetVectorDocument.cs`,
> `MongoDatasetSearchIndex.cs`, `MongoOptions.cs`;
> [ADR-0013](../docs/adr/0013-embeddings-foundry-y-local-dev.md) (embeddings) y
> [ADR-0014](../docs/adr/0014-atlas-vector-search.md) (índice).

## Documento (`DatasetVectorDocument`)

| Campo | Tipo | Descripción |
|---|---|---|
| `_id` | string | Id 4x4 del dataset (mismo id del catálogo — une ambas colecciones). |
| `name` | string | Nombre del dataset (para mostrar el hit sin abrir el catálogo). |
| `category` | string? | Categoría temática. |
| `sourceUrl` | string? | URL pública — viaja hasta la cita de la respuesta. |
| `embedding` | float[] | Vector del dataset; **campo indexado** por Atlas Vector Search. |

## Índice vectorial (Atlas Vector Search)

| Parámetro | Valor (configurable en `appsettings → Mongo`) |
|---|---|
| Nombre del índice | `dataset_vector_index` |
| Tipo | `vectorSearch` (etapa de agregación `$vectorSearch`) |
| Similitud | coseno |
| Dimensiones | `VectorDimensions` — **debe coincidir con el generador de embeddings configurado** |
| Candidatos (ANN) | `VectorNumCandidates` (100 por defecto) |

La creación del índice se intenta por driver (*best-effort*); en tiers que no lo permiten se crea
desde la consola de Atlas ([ADR-0014](../docs/adr/0014-atlas-vector-search.md)).

## Generadores de embeddings (puerto `IEmbeddingGenerator`)

| Adaptador | Proveedor | Uso | Configuración |
|---|---|---|---|
| `FoundryEmbeddingGenerator` | Azure AI Foundry — deployment `text-embedding-3-small` | Calidad (demo/producción) | `Providers:Embeddings = Foundry`; `Foundry:Embeddings` (`Deployment`, `ApiVersion`, `Dimensions: 256`) |
| `LocalHashingEmbeddingGenerator` | Local (hashing/bag-of-words determinista) | Desarrollo y pruebas: $0, sin red, determinista | `Providers:Embeddings = Local` |

Reglas clave ([ADR-0013](../docs/adr/0013-embeddings-foundry-y-local-dev.md)):

- **El índice y la consulta usan el mismo generador**, así las dimensiones siempre concuerdan.
- Cambiar de generador (o de dimensión) implica **reindexar** (`POST /search/index`).

## Flujo RAG completo

1. **Indexación** (`POST /search/index`): metadatos del catálogo → embedding por dataset →
   upsert en `dataset_vectors`.
2. **Consulta**: la pregunta del ciudadano → embedding → `$vectorSearch` → top-k
   `DatasetSearchHit` (id, nombre, categoría, fuente, score).
3. **Generación**: los candidatos van como contexto al agente; el LLM **recalcula la relevancia**
   de cada uno (re-ranking por JSON) y sólo se citan los que superan el umbral (0.5 por defecto)
   — evita citar datasets cercanos por embedding pero fuera de tema
   ([ADR-0015](../docs/adr/0015-arquitectura-multiagente.md)).

## Adaptadores del índice (puerto `IDatasetSearchIndex`)

| Adaptador | Uso |
|---|---|
| `MongoDatasetSearchIndex` | Atlas Vector Search (persistente, escalable). |
| `InMemorySearchIndex` | Desarrollo/pruebas: $0, volátil, un proceso. |

Selección por `Providers:SearchIndex = InMemory | Mongo`.
