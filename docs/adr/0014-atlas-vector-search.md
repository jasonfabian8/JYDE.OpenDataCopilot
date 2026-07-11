# ADR 0014 — Índice de búsqueda con MongoDB Atlas Vector Search

- **Estado:** Aceptado
- **Fecha:** 2026-06-24
- **Decisores:** Equipo OpenData Copilot

## Contexto

El bounded context Search necesita un índice **persistente y escalable** además del adaptador en
memoria (que se pierde al reiniciar y no escala más allá de un proceso). Ya usamos **MongoDB Atlas**
como almacén ([ADR-0012](0012-persistencia-mongodb-atlas.md)); Atlas ofrece **Vector Search**
(`$vectorSearch`), lo que permite **un único motor** para documentos + búsqueda vectorial, sin
añadir otra pieza de infraestructura.

## Decisión

- Implementar `MongoDatasetSearchIndex` como adaptador de `IDatasetSearchIndex` usando
  **Atlas Vector Search** (etapa de agregación `$vectorSearch`, similitud coseno).
- Seleccionable por configuración (`Providers:SearchIndex = InMemory | Mongo`). En desarrollo se usa
  Mongo; el adaptador en memoria queda como opción offline/$0.
- Los vectores se guardan en una colección dedicada (`dataset_vectors`); el campo `embedding` se
  indexa con un índice de tipo *vectorSearch*. La **dimensión del índice se ata a la del generador
  de embeddings** configurado (local = 256) para que siempre concuerden.
- La creación del índice se intenta por driver (*best-effort*); si el tier no lo permite, se crea
  desde la consola de Atlas.

## Consecuencias

- **Positivas:** búsqueda persistente y escalable sin nueva infraestructura; un solo motor
  (documentos + vector + futuro keyword/híbrido); intercambiable con el adaptador en memoria.
- **Negativas / trade-offs:** el índice de Atlas tarda en quedar *queryable* tras crearse (las
  primeras búsquedas pueden devolver vacío); límites del tier M0 (latencia, nº de índices); reindexar
  al cambiar el generador de embeddings o su dimensión.
- **Seguimiento:** al integrar Foundry embeddings, ajustar `VectorDimensions` (p. ej. 1536) y
  reindexar; evaluar búsqueda **híbrida** (Atlas Search keyword + vector).

## Alternativas consideradas

- **Solo índice en memoria** — simple y $0, pero volátil y limitado a un proceso. Se conserva para dev.
- **Qdrant / pgvector (Docker)** — buen rendimiento, pero añade infraestructura a operar; Atlas ya
  está disponible y gestionado.
