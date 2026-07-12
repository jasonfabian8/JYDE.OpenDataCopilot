# Fuentes de datos

> Fuentes: [ADR-0002](adr/0002-socrata-sin-scraping.md),
> [ADR-0005](adr/0005-estrategia-datos-hibrida.md), [SAD §9](architecture/SAD.md#9-estrategia-de-datos-híbrida),
> `src/JYDE.OpenDataCopilot.Infrastructure/Socrata/`.

## Fuente única: datos.gov.co (plataforma Socrata)

Todos los datos del sistema provienen del **Portal de Datos Abiertos de Colombia**:

| Ítem | Valor |
|---|---|
| Portal | <https://www.datos.gov.co> |
| Plataforma | Socrata (SODA) |
| API de catálogo | `https://www.datos.gov.co/api/catalog/v1` — metadatos de todos los datasets (nombre, descripción, columnas, tipos, categoría, tags) |
| API de datos | por dataset, consultable con **SoQL** (SQL sobre HTTP), respuesta JSON/CSV |
| Identificador de dataset | formato "4x4" de Socrata (p. ej. `ddau-8cy9`) |
| Volumen | 8.000+ datasets publicados |

La `BaseAddress`, el tamaño de página de ingesta y el timeout se configuran en
`src/JYDE.OpenDataCopilot.Api/appsettings.json → Socrata`.

## Política de acceso: solo API oficial, sin scraping

Decisión registrada en [ADR-0002](adr/0002-socrata-sin-scraping.md):

- El catálogo y los datos se obtienen **exclusivamente** por las APIs oficiales de Socrata.
- **No se hace web scraping**: datos trazables, términos de uso respetados y sin fragilidad ante
  cambios de HTML.
- Resiliencia en los adaptadores (`SocrataCatalogClient`, `SocrataDataQuery`): timeouts y manejo
  de errores; el paginado de ingesta está acotado en la frontera de la API (defensa CWE-834).

## Estrategia híbrida: amplitud + profundidad

Decisión registrada en [ADR-0005](adr/0005-estrategia-datos-hibrida.md):

1. **Amplitud (descubrimiento).** Se ingieren e indexan **metadatos** del catálogo — todo o
   filtrado por categorías elegidas por el usuario — y se vectorizan para búsqueda semántica
   (ver [`data/rag_embeddings.md`](../data/rag_embeddings.md)). Barato y escala lineal.
2. **Profundidad (respuestas con datos).** Los datos reales se consultan **on-demand vía SoQL**
   sobre la fuente oficial en el momento de la pregunta; el agente de cifras genera la consulta,
   el backend la ejecuta y tabula/grafica el resultado.
3. **Honestidad.** Si los datos no soportan la respuesta, el sistema lo declara; **nunca inventa
   cifras**.

> Los datos **nunca se copian a ciegas** al repositorio ni a la base de datos: se consultan en
> vivo sobre la fuente oficial y se citan en la respuesta.

## Trazabilidad y citación

- Cada dataset conserva su `SourceUrl` (URL pública en el portal) desde la ingesta
  (`Dataset.SourceUrl` en el dominio) hasta la respuesta (`Citation` con dataset + enlace +
  relevancia).
- Toda respuesta del Copilot basada en datos **cita su fuente**; la interfaz muestra el enlace al
  dataset original. Sin fuente no hay respuesta (guardrail del
  [ADR-0015](adr/0015-arquitectura-multiagente.md)).

## Qué se almacena localmente (y qué no)

| Se almacena | Dónde | Para qué |
|---|---|---|
| Metadatos del catálogo | MongoDB Atlas, colección `datasets` | Descubrimiento, esquema de columnas para los agentes |
| Vectores (embeddings de metadatos) | MongoDB Atlas, colección `dataset_vectors` | Búsqueda semántica (RAG) |
| Conversaciones (transcripción, memoria, artefactos, auditoría) | MongoDB Atlas, colección `conversations` | Continuidad y trazabilidad ([ADR-0017](adr/0017-persistencia-conversaciones.md)) |
| **Datos de los datasets** | **No se replican** | Se consultan en vivo vía SoQL; los artefactos guardan solo las filas ya tabuladas de una respuesta |

Detalle de las estructuras: [`data/README.md`](../data/README.md).
