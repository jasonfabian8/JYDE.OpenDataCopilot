# ADR 0013 — Embeddings: Foundry (objetivo) + adaptador local determinista para desarrollo

- **Estado:** Aceptado
- **Fecha:** 2026-06-24
- **Decisores:** Equipo OpenData Copilot

## Contexto

El bounded context **Search** necesita vectorizar texto (metadatos de datasets y la consulta del
usuario) para la búsqueda semántica. El puerto `IEmbeddingGenerator` aísla esa capacidad. Hay dos
necesidades distintas: **calidad** (para el demo/producción) y **trabajar sin costo ni red** (loop
de desarrollo y pruebas deterministas).

## Decisión

- **Objetivo (calidad):** generar embeddings vía **Azure AI Foundry**, modelo
  **`text-embedding-3-small`** (1536 dims) por su relación costo/calidad
  ([ADR-0004](0004-azure-foundry-gpt41mini.md)). **Implementado** como `FoundryEmbeddingGenerator`
  (REST); se activa con `Providers:Embeddings = Foundry` + sección `Foundry` (endpoint/clave/deployment).
- **Desarrollo/pruebas (costo cero, offline):** adaptador **`LocalHashingEmbeddingGenerator`** —
  embedding determinista *hashing/bag-of-words* (dimensión configurable, p. ej. 256), sin red ni
  costo. Da similitud por términos compartidos; suficiente para cablear y probar el flujo.
- Selección por configuración (`Providers:Embeddings = Local | Foundry`). El **índice y la consulta
  usan el mismo generador**, de modo que las dimensiones siempre concuerdan.

## Consecuencias

- **Positivas:** Search funciona **end-to-end desde ya** sin Foundry ni costo; pruebas
  deterministas; cambio a Foundry sin tocar dominio/aplicación.
- **Negativas / trade-offs:** el embedding local es un *baseline* (semántica limitada, sobre todo
  sinónimos); la calidad real llega con Foundry. Reindexar al cambiar de generador (cambian los
  vectores).
- **Seguimiento:** implementar `FoundryEmbeddingGenerator`; confirmar dimensiones/costos reales;
  decidir el índice vectorial de producción (Atlas Vector Search) en un ADR aparte.

## Alternativas consideradas

- **Solo Foundry desde ya** — bloquea el desarrollo/pruebas en credenciales y añade costo en cada
  iteración. Descartado como única opción.
- **TF-IDF/keyword puro** — útil, pero no encaja en el puerto de *embeddings*; el hashing vectorial
  da un vector denso comparable por coseno, homogéneo con el camino Foundry.
