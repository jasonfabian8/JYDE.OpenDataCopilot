# Catálogo de datasets (colección `datasets`)

Almacena los **metadatos** de los datasets de datos.gov.co ingeridos vía la API de catálogo de
Socrata (`/api/catalog/v1`). Es la base del descubrimiento: de aquí salen el esquema de columnas
que usan los agentes y el texto que se vectoriza para la búsqueda semántica.

> Fuente: `src/JYDE.OpenDataCopilot.Domain/Catalog/` (agregado) y
> `src/JYDE.OpenDataCopilot.Infrastructure/Mongo/DatasetDocument.cs` (persistencia).
> Adaptadores del repositorio (`ICatalogRepository`): `InMemoryCatalogRepository` (dev) y
> `MongoCatalogRepository` (Atlas) — [ADR-0012](../docs/adr/0012-persistencia-mongodb-atlas.md).

## Documento (`DatasetDocument` ↔ agregado `Dataset`)

| Campo | Tipo | Descripción |
|---|---|---|
| `_id` | string | Identificador **4x4** de Socrata (p. ej. `ddau-8cy9`); validado por el VO `DatasetId` (`^[a-z0-9]{4}-[a-z0-9]{4}$`). |
| `Name` | string | Nombre del dataset (obligatorio; invariante del agregado). |
| `Description` | string? | Descripción del dataset. |
| `Category` | string? | Categoría temática asignada en el portal (p. ej. "Salud y Protección Social"). Permite la ingesta selectiva por categorías. |
| `Tags` | string[] | Etiquetas del portal. |
| `Columns` | `DatasetColumnDocument[]` | Esquema del dataset (ver abajo). |
| `SourceUrl` | string? | URL pública del dataset en datos.gov.co — **la fuente que se cita** en cada respuesta. |
| `UpdatedAt` | DateTimeOffset? | Última actualización de los datos, si el portal la reporta. |

### Columna (`DatasetColumnDocument` ↔ VO `DatasetColumn`)

| Campo | Tipo | Descripción |
|---|---|---|
| `Name` | string | Nombre legible de la columna. |
| `FieldName` | string | Nombre técnico del campo en la API — el que se usa en las consultas **SoQL**. |
| `DataType` | string | Tipo declarado por Socrata (`text`, `number`, `calendar_date`, …). |
| `Description` | string? | Descripción de la columna, si existe. |

## Quién lo usa

| Consumidor | Uso |
|---|---|
| `SearchController → IndexCatalogService` | Vectoriza los metadatos para construir el índice RAG ([`rag_embeddings.md`](rag_embeddings.md)). |
| `dataset-analyst-agent` | Trae el esquema completo (columnas) para explicar estructuras y evaluar cruces/correlaciones. |
| `figures-agent` | Usa `FieldName`/`DataType` de las columnas para que el LLM escriba SoQL válido y consulte datos reales. |
| `category-recommender-agent` | Compara categorías disponibles vs. cargadas (`GetLoadedCategoriesAsync`) para recomendar qué ingerir. |
| `CatalogController` | `count`, `categories`, `GET /catalog/{id}`. |

## Invariantes y saneamiento

- El agregado `Dataset` valida id y nombre en el constructor; no existen documentos sin id 4x4
  válido ni sin nombre.
- La ingesta se acota en la frontera de la API (`CatalogIngestRequest`: límite ≤ 10.000,
  categorías ≤ 50) para que datos de usuario no gobiernen los bucles de paginación (CWE-834).
