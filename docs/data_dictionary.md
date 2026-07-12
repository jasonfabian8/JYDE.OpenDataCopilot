# Diccionario de datos

Resumen del modelo de datos del sistema. El detalle campo a campo de cada almacén vive en la
carpeta [`data/`](../data/README.md); este documento es la puerta de entrada.

> Fuente: código real del repositorio — dominio (`src/JYDE.OpenDataCopilot.Domain/Catalog/`),
> DTOs de Application (`…Application/Conversation/`), documentos de persistencia
> (`…Infrastructure/Mongo/`) y `appsettings.json → Mongo`.

## Almacenes (MongoDB Atlas, base `odc_BD`)

| Colección | Contenido | Detalle |
|---|---|---|
| `datasets` | Metadatos del catálogo de datos.gov.co (agregado `Dataset`) | [`data/catalogo_datasets.md`](../data/catalogo_datasets.md) |
| `dataset_vectors` | Embeddings de los datasets para búsqueda semántica (RAG) | [`data/rag_embeddings.md`](../data/rag_embeddings.md) |
| `conversations` | Conversaciones: transcripción + memoria + artefactos + auditoría | [`data/conversaciones.md`](../data/conversaciones.md) |

En desarrollo, cada almacén tiene un adaptador `InMemory` equivalente (costo $0, sin red),
seleccionable por configuración ([ADR-0003](adr/0003-ports-adapters-intercambiables.md)).

## Entidades principales

| Entidad / DTO | Capa | Campos clave |
|---|---|---|
| `Dataset` (agregado raíz) | Domain | `Id` (4x4), `Name`, `Description`, `Category`, `Tags`, `Columns`, `SourceUrl`, `UpdatedAt` |
| `DatasetColumn` (VO) | Domain | `Name`, `FieldName` (campo SoQL), `DataType`, `Description` |
| `DatasetId` (VO) | Domain | formato `xxxx-xxxx` de Socrata, validado |
| `ConversationRecord` | Application | `Id`, `Title`, `ThreadId`, `Messages`, `Objective`, `SelectedDatasets`, `Artifacts`, `AuditLog`, `UpdatedAtUtc` |
| `Citation` | Application | `DatasetId`, `Name`, `SourceUrl`, `Score` — la cita obligatoria de cada respuesta |
| `AgentInteraction` | Application | `Agent`, `Request`, `Response` — auditoría cruda por turno |

## Reglas transversales

- **El dato identificador** de todo el sistema es el id 4x4 de Socrata; enlaza catálogo, índice
  vectorial, citas y artefactos.
- **Los datos de los datasets no se replican**: se consultan en vivo vía SoQL
  ([`fuentes_datos.md`](fuentes_datos.md)); sólo los artefactos conservan las filas ya tabuladas
  de una respuesta concreta.
- **`SourceUrl` viaja de extremo a extremo** (ingesta → índice → cita) para garantizar la
  trazabilidad de cada respuesta.
