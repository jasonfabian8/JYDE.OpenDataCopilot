# Requerimientos iniciales

Requerimientos del proyecto derivados de la visión y los drivers de calidad del
[SAD §1–2](../architecture/SAD.md#1-visión-y-contexto) y de las decisiones registradas en los
[ADRs](../adr/README.md). Las historias de usuario que los materializan están en
[`user_stories.md`](user_stories.md).

## Visión

Que cualquier persona consulte los datos abiertos de Colombia (datos.gov.co, 8.000+ datasets)
**con una pregunta en lenguaje natural** y reciba una respuesta clara, verificable y **siempre
citando su fuente** ([`planteamiento_problema.md`](../planteamiento_problema.md)).

## Requerimientos funcionales

| ID | Requerimiento | Origen / referencia |
|----|---------------|---------------------|
| RF-01 | Ingerir los metadatos del catálogo de datos.gov.co vía la API oficial de Socrata, completo o filtrado por categorías y con límite acotado. | [ADR-0002](../adr/0002-socrata-sin-scraping.md), [ADR-0005](../adr/0005-estrategia-datos-hibrida.md) |
| RF-02 | Listar las categorías temáticas del catálogo con su conteo, para acotar la ingesta. | `CatalogController` |
| RF-03 | Vectorizar los metadatos (embeddings) y construir un índice de búsqueda semántica. | [ADR-0013](../adr/0013-embeddings-foundry-y-local-dev.md), [ADR-0014](../adr/0014-atlas-vector-search.md) |
| RF-04 | Buscar los datasets más relevantes para una consulta en lenguaje natural (top-k). | `SearchController` |
| RF-05 | Conversar con el ciudadano en lenguaje natural con respuesta en streaming (SSE). | [ADR-0015](../adr/0015-arquitectura-multiagente.md) |
| RF-06 | Enrutar cada mensaje al agente especializado adecuado (multiagente), con degradación a reglas si el enrutador LLM falla. | [ADR-0015](../adr/0015-arquitectura-multiagente.md) |
| RF-07 | **Citar la fuente en toda respuesta basada en datos** (dataset + enlace); si los datos no la soportan, declararlo — nunca inventar cifras. | Regla no negociable ([CLAUDE.md](../../CLAUDE.md), SAD §2) |
| RF-08 | Responder preguntas de cifras consultando **datos reales** vía SoQL y generando artefactos de tabla y gráfico. | [`models/figures-agent.md`](../../models/figures-agent.md) |
| RF-09 | Recomendar qué categorías del catálogo cargar cuando el catálogo actual no cubre la necesidad, con acción de un clic y re-pregunta automática. | [`models/category-recommender-agent.md`](../../models/category-recommender-agent.md) |
| RF-10 | Explicar el esquema (columnas) de un dataset y evaluar la factibilidad de cruces/correlaciones entre datasets. | [`models/dataset-analyst-agent.md`](../../models/dataset-analyst-agent.md) |
| RF-11 | Mantener memoria conversacional: objetivo acumulado (editable) y datasets fijados por el usuario, sin estado en el backend. | [ADR-0015 §Memoria](../adr/0015-arquitectura-multiagente.md) |
| RF-12 | Persistir conversaciones completas (transcripción + memoria + artefactos + auditoría) con guardado manual: guardar, listar, recuperar y eliminar. | [ADR-0017](../adr/0017-persistencia-conversaciones.md) |
| RF-13 | Registrar la auditoría de cada turno: interacciones crudas (input/output) con cada agente. | [ADR-0017](../adr/0017-persistencia-conversaciones.md) |

## Requerimientos no funcionales

| ID | Atributo | Requerimiento | Cómo se logra |
|----|----------|---------------|----------------|
| RNF-01 | **Costo** (restricción dura) | Desarrollo cercano a $0; producción en capas gratuitas y modelos económicos. | Adaptadores locales (`Fake`, `Local`, `InMemory`), gpt-4o-mini/gpt-4.1-mini, Atlas M0 ([ADR-0004](../adr/0004-azure-foundry-gpt41mini.md)) |
| RNF-02 | Intercambiabilidad | Cambiar de proveedor (LLM, embeddings, índice, BD) sin tocar dominio/aplicación. | Puertos + adaptadores seleccionados por `appsettings → Providers` ([ADR-0003](../adr/0003-ports-adapters-intercambiables.md)) |
| RNF-03 | Confiabilidad de la respuesta | No alucinar; trazabilidad de cada afirmación. | RAG sobre metadatos + re-ranking con umbral + cita obligatoria + guardrails de "no sé" |
| RNF-04 | Legalidad/trazabilidad de datos | Datos sólo vía API oficial; **sin web scraping**. | [ADR-0002](../adr/0002-socrata-sin-scraping.md) |
| RNF-05 | Calidad de código | Cobertura ≥ 95 % por proyecto; build limpio (`TreatWarningsAsErrors`); análisis continuo. | TDD ([ADR-0006](../adr/0006-tdd-por-convencion.md)), coverlet, SonarCloud en CI |
| RNF-06 | Escalabilidad | De demo a nivel nacional sin rediseño. | Backend stateless + servicios gestionados de Azure (SAD §12) |
| RNF-07 | Mantenibilidad | Equipo pequeño + asistentes de IA sin degradar el diseño. | Hexagonal/DDD, límites de capa estrictos, ADRs, estándar Clean Code/SOLID ([ADR-0007](../adr/0007-estandar-clean-code-solid.md)) |
| RNF-08 | Auditabilidad de la IA | Toda interacción con agentes queda registrada y es consultable. | `AuditingChatCompletion` + persistencia ([ADR-0017](../adr/0017-persistencia-conversaciones.md)) |
| RNF-09 | Time-to-market | Demo funcional dentro del calendario del concurso. | API Socrata directa, vertical slices, skills de automatización |
| RNF-10 | Experiencia de usuario | Respuesta percibida en tiempo real; accesibilidad y responsive. | Streaming SSE token a token; roles/ARIA y teclado (SAD §11) |

## Fuera de alcance (por ahora)

Diferimientos conscientes registrados con su condición de disparo:

- **Autenticación/multiusuario** — alcance de concurso; ver seguimiento del
  [ADR-0017](../adr/0017-persistencia-conversaciones.md).
- **Roadmap de evolución** (gestión de usuarios, sesiones por usuario, gestión de tokens, agente
  de autoaprendizaje) — backlog en [`user_stories.md §Backlog`](user_stories.md#épica-6--backlog-evolución-roadmap).
- Deudas técnicas activas: [`tech-debt.md`](../tech-debt.md).
