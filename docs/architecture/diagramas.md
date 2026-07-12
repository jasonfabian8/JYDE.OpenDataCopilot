# Diagramas de arquitectura (Mermaid)

Complemento visual del [SAD](SAD.md) (que contiene los diagramas C4 niveles 1–3). Todos los
diagramas reflejan el **código real** del repositorio; los nombres de clases, puertos y agentes
son los del código.

Índice:
1. [Arquitectura DDD: capas hexagonales](#1-arquitectura-ddd-capas-hexagonales)
2. [Mapa de bounded contexts (context map)](#2-mapa-de-bounded-contexts-context-map)
3. [Integración entre agentes](#3-integración-entre-agentes)
4. [Secuencia de una conversación](#4-secuencia-de-una-conversación)
5. [Clases del dominio y del modelo de conversación](#5-clases-del-dominio-y-del-modelo-de-conversación)
6. [Estados de un turno de conversación](#6-estados-de-un-turno-de-conversación)
7. [Despliegue](#7-despliegue)

---

## 1. Arquitectura DDD: capas hexagonales

Regla de dependencias **sólo hacia adentro** (ver [SAD §4](SAD.md#4-estilo-arquitectónico-hexagonal--ddd)):
Application define los puertos, Infrastructure los implementa, Api compone por configuración.

```mermaid
flowchart TB
    subgraph Api["Api — composition root (ASP.NET Core, controladores MVC)"]
        CTRL["ChatController · CatalogController<br/>SearchController · ConversationsController"]
        DI["Program.cs — DI por configuración<br/>(appsettings → Providers)"]
    end

    subgraph Infra["Infrastructure — adaptadores"]
        SOC["Socrata<br/>SocrataCatalogClient · SocrataDataQuery"]
        FOU["Foundry<br/>FoundryChatCompletion · FoundryEmbeddingGenerator"]
        MON["Mongo<br/>MongoCatalogRepository · MongoDatasetSearchIndex · MongoConversationStore"]
        LOC["Locales ($0)<br/>FakeChatCompletion · LocalHashingEmbeddingGenerator · InMemory*"]
    end

    subgraph App["Application — casos de uso y puertos"]
        UC["CopilotOrchestrator · IngestCatalogService<br/>IndexCatalogService · SearchDatasetsService<br/>ConversationArchiveService"]
        PORTS["Puertos: ICatalogSource · ICatalogRepository<br/>IDatasetSearchIndex · IEmbeddingGenerator<br/>IChatCompletion · IDataQuery · IConversationStore"]
    end

    subgraph Dom["Domain — núcleo puro (0 dependencias)"]
        AGG["Dataset (agregado raíz)<br/>DatasetId · DatasetMetadata · DatasetColumn"]
    end

    Api --> Infra
    Infra --> App
    App --> Dom
    Infra -. "usa transitivamente" .-> Dom
```

## 2. Mapa de bounded contexts (context map)

Los 4 contextos del [SAD §5](SAD.md#5-bounded-contexts) y cómo se relacionan:

```mermaid
flowchart LR
    subgraph Catalog["Catalog — qué existe"]
        C1["Ingesta de metadatos<br/>(ICatalogSource → Socrata)"]
        C2[("datasets<br/>ICatalogRepository")]
    end

    subgraph Search["Search — encontrar"]
        S1["Embeddings<br/>(IEmbeddingGenerator)"]
        S2[("dataset_vectors<br/>IDatasetSearchIndex")]
    end

    subgraph Conversation["Conversation — responder"]
        V1["CopilotOrchestrator<br/>+ 6 agentes"]
        V2[("conversations<br/>IConversationStore")]
    end

    subgraph DataCache["DataCache — profundidad"]
        D1["IDataQuery<br/>(SoQL en vivo · SocrataDataQuery)"]
    end

    EXT1["datos.gov.co<br/>(API Socrata)"]
    EXT2["Azure AI Foundry<br/>(LLM + embeddings)"]

    EXT1 --> C1 --> C2
    C2 -->|"metadatos"| S1 --> S2
    S2 -->|"top-k candidatos (RAG)"| V1
    C2 -->|"esquemas (columnas)"| V1
    V1 -->|"cifras"| D1 --> EXT1
    V1 <-->|"chat"| EXT2
    S1 <-->|"vectores"| EXT2
    V1 -->|"guardar/recuperar"| V2
```

## 3. Integración entre agentes

Los 6 agentes reales ([`models/`](../../models/README.md)) y su colaboración en un turno
([ADR-0015](../adr/0015-arquitectura-multiagente.md)):

```mermaid
flowchart TB
    U(("👤 Ciudadano")) -->|"pregunta (SSE POST /chat)"| O["🧭 CopilotOrchestrator"]

    O -->|"1 · ¿quién atiende?"| R["router-agent<br/>v4 · gpt-4o-mini"]
    R -. "si falla: reglas CanHandle<br/>(DefaultAgentRouter)" .-> O

    O -->|"2 · delega el turno"| REC["dataset-recommender-agent<br/>v5 · gpt-4.1-mini"]
    O -->|"2"| ANA["dataset-analyst-agent<br/>v3 · gpt-4.1-mini"]
    O -->|"2"| CAT["category-recommender-agent<br/>v2 · gpt-4o-mini"]
    O -->|"2"| FIG["figures-agent<br/>v1 · gpt-4.1-mini"]

    O -->|"3 · actualiza memoria"| OBJ["objective-tracker-agent<br/>v1 · gpt-4o-mini"]
    O -->|"4 · anexa auditoría"| AUD["AuditingChatCompletion<br/>+ InteractionRecorder"]

    EMB["embeddings<br/>text-embedding-3-small · 256d"]
    IDX[("Índice vectorial<br/>dataset_vectors")]
    REPO[("Catálogo<br/>datasets")]
    SODA["datos.gov.co<br/>SoQL en vivo (IDataQuery)"]

    REC -->|"retrieval RAG"| EMB --> IDX
    ANA -->|"candidatos + esquema"| IDX
    ANA --> REPO
    CAT -->|"categorías vs. cargadas"| REPO
    FIG -->|"elige dataset + esquema"| REPO
    FIG -->|"ejecuta SoQL"| SODA

    REC -->|"sources + tokens"| U
    ANA -->|"sources + tokens"| U
    CAT -->|"categories (botones)"| U
    FIG -->|"table + chart + tokens"| U
```

- **gpt-4o-mini** (clasificar/resumir: router, objetivo, categorías) y **gpt-4.1-mini**
  (razonar: recomendar, analizar, SoQL) — modelos de bajo costo por diseño
  ([ADR-0004](../adr/0004-azure-foundry-gpt41mini.md)).
- Todos los agentes reciben la memoria (`ContextHeader`: objetivo + datasets fijados) y sólo
  citan lo que supera el umbral de relevancia re-calculada (0.5).

## 4. Secuencia de una conversación

Turno completo, de la pregunta a la respuesta citada con auditoría y memoria
(ejemplo con el agente de cifras; eventos SSE en negrita):

```mermaid
sequenceDiagram
    actor U as Ciudadano
    participant W as Web (React, SSE)
    participant CC as ChatController
    participant O as CopilotOrchestrator
    participant R as router-agent (LLM)
    participant F as figures-agent
    participant E as IEmbeddingGenerator
    participant X as IDatasetSearchIndex
    participant REP as ICatalogRepository
    participant L as IChatCompletion (Foundry)
    participant Q as IDataQuery (SoQL)
    participant T as ObjectiveTracker

    U->>W: "¿Cuántos casos hay por municipio?"
    W->>CC: POST /chat {question, objective, selectedDatasets, conversationId}
    CC->>O: AskAsync(...)
    O->>R: RouteAsync(pregunta + catálogo de agentes)
    R-->>O: {"agente": "figures-agent"}
    O->>F: HandleAsync(contexto)
    F-->>W: event: agent
    F->>E: embedding(pregunta)
    F->>X: top-k candidatos
    F->>REP: esquema completo (columnas SoQL)
    F->>L: memoria + pregunta + candidatos con esquema
    L-->>F: {"datasetId", "soql", "explicacion", "chart"}
    F->>Q: ejecutar SoQL sobre datos.gov.co
    Q-->>F: filas reales
    F-->>W: event: sources (cita del dataset)
    F-->>W: event: table · event: chart
    F-->>W: event: token (…streaming…)
    F-->>W: event: conversation (id del hilo)
    O->>T: UpdateAsync(objetivo, pregunta)
    T-->>O: objetivo actualizado
    O-->>W: event: objective
    O-->>W: event: audit (interacciones crudas del turno)
    O-->>W: event: done
    W-->>U: respuesta citada + tabla/gráfico + memoria
    Note over U,W: Guardado manual: PUT /conversations/{id}<br/>(transcripción + memoria + artefactos + auditoría)
```

## 5. Clases del dominio y del modelo de conversación

Dominio del contexto Catalog (código real de `src/JYDE.OpenDataCopilot.Domain/Catalog/`):

```mermaid
classDiagram
    class Dataset {
        <<aggregate root>>
        +DatasetId Id
        +string Name
        +string? Description
        +string? Category
        +IReadOnlyList~string~ Tags
        +IReadOnlyList~DatasetColumn~ Columns
        +Uri? SourceUrl
        +DateTimeOffset? UpdatedAt
    }
    class DatasetId {
        <<value object>>
        +string Value
        +ToString() string
    }
    note for DatasetId "Formato 4x4 de Socrata validado (^[a-z0-9]{4}-[a-z0-9]{4}$)"
    class DatasetMetadata {
        <<value object>>
        +string? Description
        +string? Category
        +IReadOnlyList~string~ Tags
        +IReadOnlyList~DatasetColumn~ Columns
        +Uri? SourceUrl
        +DateTimeOffset? UpdatedAt
    }
    class DatasetColumn {
        <<value object>>
        +string Name
        +string FieldName
        +string DataType
        +string? Description
    }
    Dataset *-- DatasetId
    Dataset *-- DatasetMetadata
    DatasetMetadata *-- "0..*" DatasetColumn
```

Modelo de conversación persistida (DTOs de Application,
[ADR-0017](../adr/0017-persistencia-conversaciones.md)):

```mermaid
classDiagram
    class ConversationRecord {
        +string Id
        +string Title
        +string? ThreadId
        +string Objective
        +DateTimeOffset UpdatedAtUtc
    }
    class ConversationMessageRecord {
        +string Id
        +string Role
        +string Content
        +string? Agent
    }
    class Citation {
        +string DatasetId
        +string Name
        +string? SourceUrl
        +double Score
    }
    class ConversationArtifactRecord {
        +string Id
        +string Kind
        +string Title
        +string[] Columns
        +string[][] Rows
        +string? Type
        +string? XColumn
        +string? YColumn
    }
    class ConversationAuditEntryRecord {
        +string Id
        +string UserMessage
    }
    class AgentInteraction {
        +string Agent
        +string Request
        +string Response
    }
    class SelectedDataset {
        +string Id
        +string Name
    }
    ConversationRecord *-- "0..*" ConversationMessageRecord : Messages
    ConversationRecord *-- "0..*" ConversationArtifactRecord : Artifacts
    ConversationRecord *-- "0..*" ConversationAuditEntryRecord : AuditLog
    ConversationRecord *-- "0..*" SelectedDataset : memoria
    ConversationMessageRecord *-- "0..*" Citation : Sources
    ConversationAuditEntryRecord *-- "1..*" AgentInteraction : Interactions
```

## 6. Estados de un turno de conversación

Máquina de estados de un turno en `CopilotOrchestrator` (con las degradaciones explícitas —
guardrails de resiliencia):

```mermaid
stateDiagram-v2
    [*] --> Recibida : POST /chat (pregunta válida)
    Recibida --> Enrutada : router-agent elige agente
    Recibida --> EnrutadaPorReglas : LLM falla → CanHandle()
    EnrutadaPorReglas --> EnAtencion
    Enrutada --> EnAtencion : agente especializado

    state EnAtencion {
        [*] --> Recuperando : RAG / esquema / categorías
        Recuperando --> Generando : LLM (JSON estructurado)
        Generando --> Citando : relevancia ≥ umbral
        Generando --> Degradada : JSON inválido → texto rescatado, sin citas
        Citando --> Emitiendo : sources / table / chart / tokens
        Degradada --> Emitiendo
        Emitiendo --> [*]
    }

    EnAtencion --> ObjetivoActualizado : objective-tracker-agent
    ObjetivoActualizado --> Auditada : interacciones crudas del turno
    Auditada --> Completada : event done
    Completada --> Guardada : PUT /conversations/{id} (manual)
    Completada --> [*] : siguiente turno (memoria viaja con el request)
    Guardada --> [*]
```

## 7. Despliegue

Según [SAD §12](SAD.md#12-despliegue-objetivo): desarrollo 100 % local y gratuito; producción en
servicios gestionados con capas free.

```mermaid
flowchart TB
    subgraph DEV["Desarrollo (costo $0, sin credenciales)"]
        DAPI["dotnet run — API<br/>Providers: Fake · Local · InMemory"]
        DWEB["Vite dev server — web"]
        DOCK["docker compose (opcional)<br/>pgvector · qdrant"]
        DWEB --> DAPI
        DAPI -.-> DOCK
    end

    subgraph PROD["Producción objetivo (Azure + capas free)"]
        SWA["Azure Static Web Apps<br/>(React)"]
        ACA["Azure Container Apps / App Service<br/>(API .NET, stateless)"]
        ATLAS[("MongoDB Atlas M0<br/>datasets · dataset_vectors · conversations<br/>+ Vector Search")]
        FOUND["Azure AI Foundry<br/>agentes versionados + embeddings"]
        SWA -->|"JSON/REST + SSE"| ACA
        ACA --> ATLAS
        ACA --> FOUND
    end

    SODA2["datos.gov.co (API Socrata)<br/>catálogo + SoQL"]
    DAPI --> SODA2
    ACA --> SODA2
```
