# Changelog — OpenData Copilot

Registro cronológico de versiones del proyecto. Formato basado en
[Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versionamiento
[SemVer](https://semver.org/lang/es/) `0.x` (producto en desarrollo pre-1.0).

## [0.7.0] — **Persistencia de conversaciones y entregables del concurso**

### Added
- **Persistencia de conversaciones** ([ADR-0017](docs/adr/0017-persistencia-conversaciones.md)):
  transcripción + memoria + artefactos + auditoría en BD. Puerto `IConversationStore` con
  adaptadores `InMemory` (dev, $0) y `Mongo` (Atlas, colección `conversations`); guardado manual
  vía `GET/PUT/DELETE /conversations`.
- Reestructuración documental alineada con la guía del concurso: `data/`, `models/`,
  `docs/requirements/`, `docs/estructura_repositorio.md` y documentos de problema, metodología,
  fuentes, API, impacto, conclusiones y validación.
- **Módulo `reports/`** (salda la deuda TD-003): `generar_reporte.py` produce
  `reporte_final.pdf` + `reports/figures/` (categorías del catálogo, relevancia de fuentes
  citadas, distribución de agentes según auditoría, latencias y métricas de ingeniería)
  consumiendo el **sistema real** por sus endpoints (`/catalog/*`, `/chat` SSE) —
  regenerable con datos frescos en cada corrida.
- **`resources/portada.png`** (salda la deuda TD-002): diapositiva principal de la
  presentación exportada a 1920×1080.
- **Demo automatizada `demo/`**: grabación reproducible con Playwright
  (`record-demo.js`) sobre el sistema con IA real + narración TTS en español
  (`narrar.py`), video final `OpenDataCopilot_Demo_ID241.mp4` (1080p) y capturas
  Full HD para la presentación.

### Changed
- Refinamiento de los **system prompts** de los seis agentes (`models/*.md`): instrucciones
  más claras y guía de comportamiento para `router`, `category-recommender`,
  `dataset-recommender`, `dataset-analyst`, `figures` y `objective-tracker`.


## [0.6.1] — PR [#8](../../pull/8)

### Added
- Recursos del concurso en `resources/`: presentación del proyecto (`.pptx` y `.pdf`).

## [0.6.0] — PR [#7](../../pull/7) — **Copilot multiagente completo**

La versión mayor del producto: de un chat con un agente a un **sistema de 6 agentes** con datos
reales, artefactos, memoria y auditoría, con su aplicación web estilo copiloto.

### Added
- **Aplicación Copilot** (frontend): chat con streaming, panel derecho acoplado con **memoria**
  (objetivo editable + datasets fijados), **artefactos** (tablas y gráficos SVG) y **auditoría**;
  gestión de categorías con reintento automático; ajustes de ingesta (`useSettingsStore`).
- **`router-agent`** (enrutador LLM, `LlmAgentRouter`): decide qué agente atiende cada mensaje;
  degrada a reglas si falla.
- **`category-recommender-agent`**: recomienda qué categorías del catálogo cargar; botones de un
  clic + re-pregunta automática (evento SSE `categories`).
- **`dataset-analyst-agent`**: explica esquemas (columnas) y evalúa cruces/correlaciones entre
  datasets desde sus metadatos.
- **`figures-agent`** + adaptador **`SocrataDataQuery`** (API SODA): cifras con **datos reales
  vía SoQL** (límite 200 filas) y artefactos de tabla/gráfico (eventos `table`/`chart`).
- **`objective-tracker-agent`** (`ObjectiveTracker`): memoria conversacional — resume el objetivo
  por turno (evento `objective`).
- **Auditoría por turno** (`AuditingChatCompletion` + `InteractionRecorder`): interacciones
  crudas con cada agente (evento `audit`).
- Saneamiento de frontera en la ingesta (`CatalogIngestRequest`: límite ≤ 10.000, categorías ≤ 50).

### Changed
- Prompts de usuario reducidos a **sólo datos** (las reglas viven versionadas en Foundry): menor
  carga de tokens por turno.
- Priorización de los **datasets fijados** por el usuario en el contexto de analista y cifras.
- Parseo JSON defensivo en los agentes (robusto ante prosa, vallas ``` y objetos duplicados).

### Fixed
- Serialización SSE en UTF-8 real (acentos/ñ legibles) conservando el escape seguro de `<>&`.

### Evolución de agentes (appsettings → `Foundry:Chat:Agents`)
| Agente | Antes | Después | Nota |
|---|---|---|---|
| dataset-recommender-agent | v1 · gpt-4o-mini | **v5 · gpt-4.1-mini** | Upgrade de modelo (v2) e iteración de instrucciones hasta v5 (re-ranking por JSON) |
| router-agent | — | **v4 · gpt-4o-mini** | Nace v1 e itera hasta v4 al integrar los nuevos agentes |
| category-recommender-agent | — | **v2 · gpt-4o-mini** | Nace v1; v2 reduce tokens de entrada |
| dataset-analyst-agent | — | **v3 · gpt-4.1-mini** | Nace v1 e itera hasta v3 |
| objective-tracker-agent | — | **v1 · gpt-4o-mini** | Nuevo (memoria) |
| figures-agent | — | **v1 · gpt-4.1-mini** | Nuevo (SoQL sobre datos reales) |

## [0.5.1] — PR [#6](../../pull/6)

### Added
- Badge del quality gate de SonarCloud en el README.

## [0.5.0] — PR [#5](../../pull/5) — **Nace la conversación multiagente**

### Added
- **Funcionalidad de chat** con el primer agente: `dataset-recommender-agent` — recomienda
  datasets citando la fuente, sobre streaming SSE (`agent` → `sources` → `token` → `done`).
- [ADR-0015](docs/adr/0015-arquitectura-multiagente.md): arquitectura multiagente
  (Copilot orquestador + agentes especializados + `IChatCompletion` con adaptadores
  `Fake`/`Foundry`).
- Límite `MaxResults` en el cliente de Socrata; mejoras del `OperationsPanel`.
- Cobertura en el workflow de SonarCloud (reporte + exclusiones).

### Evolución de agentes
| Agente | Versión | Modelo |
|---|---|---|
| dataset-recommender-agent | **v1** (primer agente del sistema) | gpt-4o-mini |

## [0.4.0] — PR [#4](../../pull/4) — **Búsqueda semántica (RAG)**

### Added
- **Generación de embeddings** con Azure AI Foundry (`text-embedding-3-small`, 256 dims) y
  adaptador local determinista para desarrollo
  ([ADR-0013](docs/adr/0013-embeddings-foundry-y-local-dev.md)).
- **Índice vectorial con MongoDB Atlas Vector Search** (colección `dataset_vectors`, similitud
  coseno) ([ADR-0014](docs/adr/0014-atlas-vector-search.md)).
- Funcionalidad de búsqueda end-to-end: indexación (`POST /search/index`) + búsqueda semántica
  (`GET /search`).

### Changed
- Refactor del agregado `Dataset` hacia `DatasetMetadata` (organización y claridad del dominio).
- Umbrales de cobertura en `Directory.Build.props`/`.editorconfig`.

## [0.3.0] — PR [#3](../../pull/3) — **Persistencia del catálogo en Atlas**

### Added
- Persistencia de metadatos del catálogo en **MongoDB Atlas** (`MongoCatalogRepository`,
  `DatasetDocument`) ([ADR-0012](docs/adr/0012-persistencia-mongodb-atlas.md)).

### Fixed
- Manejo del token de SonarQube por variable de entorno (buenas prácticas de seguridad).
- Contraseña de PostgreSQL consistente en `docker-compose.yml`; `Program.cs` con `RunAsync`.

## [0.2.0] — PR [#2](../../pull/2) — **Fundación del producto**

### Added
- **Estructura hexagonal + DDD**: capas Domain / Application / Infrastructure / Api, Docker y
  proyectos de prueba por capa.
- **Gobierno del proyecto**: SAD, estándar de codificación, plantilla y primeros **11 ADRs**;
  `CLAUDE.md` por capa + instrucciones espejo de GitHub Copilot; skills `/new-context`,
  `/new-adapter`, `/adr`.
- **Bounded context Catalog**: dominio (`Dataset`, `DatasetId`, columnas), cliente de **Socrata**
  (`SocrataCatalogClient`), servicio de ingesta, repositorio en memoria y `CatalogController`.
- **Frontend**: landing de OpenData Copilot (React + Vite + Zustand + Tailwind,
  [ADR-0008](docs/adr/0008-stack-frontend-vite-zustand.md)/[0009](docs/adr/0009-estilos-tailwind.md)).
- Pruebas unitarias de la API y mejora de cobertura; registro de deuda técnica
  (`docs/tech-debt.md`).

## [0.1.1] — PR [#1](../../pull/1)

### Added
- Workflow de análisis **SonarCloud** en GitHub Actions.

## [0.1.0] — **Inicio**

### Added
- Repositorio inicial y primer `README.md` con la visión del proyecto.

[0.7.0]: ../../compare/main...feature/docs
