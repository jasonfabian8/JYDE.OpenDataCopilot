# ADR 0003 — Puertos con adaptadores intercambiables por configuración

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

Varias capacidades de la solución (búsqueda vectorial, almacenamiento/cache, inferencia de modelos) admiten múltiples tecnologías equivalentes en función, pero con perfiles distintos de rendimiento, madurez, operación y costo. Buscamos deliberadamente una solución **agnóstica del proveedor**: que el núcleo de negocio dependa de un **contrato estable** y no de un SDK concreto, de modo que se puedan **incorporar varios proveedores** y **mutar entre tecnologías sin reescribir la aplicación**.

Esto persigue objetivos técnicos concretos:

- **Inversión de dependencias (DIP):** dominio y aplicación dependen de abstracciones; los detalles de cada tecnología quedan confinados en adaptadores reemplazables.
- **Evaluación y adopción de la mejor tecnología:** poder comparar implementaciones en paralelo (A/B) sobre el mismo contrato y adoptar la que mejor rinda en cada escenario.
- **Portabilidad por entorno:** ejecutar implementaciones embebidas/locales en desarrollo y servicios gestionados en producción, **seleccionables por configuración**, sin cambios de código.
- **Optimización costo/beneficio:** elegir, por capacidad y por entorno, la opción con mejor relación costo/rendimiento, y migrar a otra si las condiciones cambian, evitando *vendor lock-in*.

## Decisión

Cada dependencia externa se define como un **puerto** (interfaz en `Application`/`Domain`) con **múltiples adaptadores** en `Infrastructure`, seleccionables por **configuración** (`appsettings` → sección `Providers`). El composition root (`Program.cs`) registra el adaptador
según el valor configurado.

- `IDatasetSearchIndex`: `PgVector` | `AzureAISearch` | `Qdrant`
- `IDatasetCache`: `DuckDb`/`Postgres` (local) | `MongoAtlas` (prod)
- `IChatCompletion`, `IEmbeddingGenerator`: `Foundry`

Los contratos se crean primero y se implementan **incrementalmente**.

## Consecuencias

- **Positivas:** sin lock-in; local gratis / prod robusto sin tocar dominio; A/B de proveedores; testeable con dobles.
- **Negativas / trade-offs:** más interfaces y registro DI; riesgo de abstraer de más → se mitiga manteniendo los puertos pequeños y orientados al caso de uso.
- **Seguimiento:** en producción la preferencia inicial es Azure AI Search + MongoDB Atlas (free).

## Alternativas consideradas

- **Acoplar directo a un proveedor** — descartado: lock-in y difícil de comparar/cambiar.
