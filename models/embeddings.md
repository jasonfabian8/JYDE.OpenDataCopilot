# embeddings — Vectorización para la búsqueda semántica (RAG)

| Ítem | Valor |
|---|---|
| **Modelo (producción)** | `text-embedding-3-small` — deployment en Azure AI Foundry |
| **Dimensiones** | 256 (`appsettings → Foundry:Embeddings:Dimensions`; el modelo permite reducir dimensión, lo que abarata índice y búsqueda) |
| **API Version** | `2024-02-01` |
| **Adaptador local (dev)** | `LocalHashingEmbeddingGenerator` — hashing/bag-of-words determinista, $0, sin red |
| **Código** | `src/JYDE.OpenDataCopilot.Infrastructure/Foundry/FoundryEmbeddingGenerator.cs` · `…/Embeddings/LocalHashingEmbeddingGenerator.cs` |
| **Decisión** | [ADR-0013](../docs/adr/0013-embeddings-foundry-y-local-dev.md) |

## Rol

No es un agente conversacional: es la capacidad de **vectorización** que sostiene el retrieval
del RAG. Convierte en vectores tanto los metadatos de cada dataset (al indexar) como la pregunta
del ciudadano (al buscar), para recuperarlos por similitud coseno en el índice vectorial
([`data/rag_embeddings.md`](../data/rag_embeddings.md), [ADR-0014](../docs/adr/0014-atlas-vector-search.md)).

## Filosofía de costo

- `text-embedding-3-small` es el modelo de embeddings **más económico** de la familia con calidad
  suficiente para metadatos ([ADR-0004](../docs/adr/0004-azure-foundry-gpt41mini.md)).
- Dimensión reducida a **256**: menos almacenamiento y búsquedas más rápidas en el tier gratuito
  de Atlas.
- En desarrollo, el adaptador local elimina el costo por completo (pruebas deterministas, sin
  credenciales).

## Reglas operativas

- **Índice y consulta usan siempre el mismo generador** (las dimensiones deben concordar con
  `Mongo:VectorDimensions`).
- Cambiar de generador o de dimensión implica **reindexar** (`POST /search/index`).
- Selección por configuración: `Providers:Embeddings = Local | Foundry`.
