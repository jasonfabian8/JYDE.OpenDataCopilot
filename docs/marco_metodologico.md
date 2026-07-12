# Marco metodológico

> Fuentes: [SAD](architecture/SAD.md), [ADRs](adr/README.md),
> [`coding-standards.md`](architecture/coding-standards.md), presentación del equipo
> ([`resources/`](../resources/)).

El ejemplo del concurso propone CRISP-ML como marco. Este proyecto no entrena modelos propios —
consume LLMs y embeddings gestionados—, así que el equipo adaptó ese espíritu (iterativo, guiado
por datos, con validación continua) a la construcción de un **producto conversacional de IA**:
arquitectura evolutiva gobernada por decisiones registradas, TDD y validación automática en CI.

## 1. Comprensión del problema y de los datos

- **Planteamiento del problema** documentado en
  [`planteamiento_problema.md`](planteamiento_problema.md); audiencias y objetivo medibles.
- **Exploración de la fuente**: análisis de la API de Socrata (catálogo + SoQL) que llevó a la
  decisión de no hacer scraping ([ADR-0002](adr/0002-socrata-sin-scraping.md)) y a la estrategia
  híbrida de datos ([ADR-0005](adr/0005-estrategia-datos-hibrida.md)).
- **Requerimientos** derivados de los drivers de calidad del SAD:
  [`requirements/requerimientos_iniciales.md`](requirements/requerimientos_iniciales.md).

## 2. Diseño: arquitectura hexagonal + DDD

- **Puertos y adaptadores** ([ADR-0001](adr/0001-stack-dotnet-hexagonal-ddd.md),
  [ADR-0003](adr/0003-ports-adapters-intercambiables.md)): toda dependencia externa (Socrata,
  Foundry, Mongo) vive detrás de una interfaz; los proveedores se intercambian por configuración.
- **DDD** con 4 bounded contexts (Catalog, Search, Conversation, DataCache) y lenguaje ubicuo
  (glosario en [SAD §4](architecture/SAD.md#4-estilo-arquitectónico-hexagonal--ddd)).
- **Toda decisión de arquitectura queda registrada como ADR** (una decisión = un archivo
  numerado); los diferimientos conscientes se documentan como deuda técnica con condición de
  disparo ([`tech-debt.md`](tech-debt.md)).

## 3. Construcción: TDD y estándares

- **TDD por convención** ([ADR-0006](adr/0006-tdd-por-convencion.md)): test primero en
  Domain/Application; xUnit + Shouldly en backend, Vitest + React Testing Library en frontend
  ([ADR-0016](adr/0016-testing-frontend-vitest.md)).
- **Clean Code + SOLID obligatorios** ([ADR-0007](adr/0007-estandar-clean-code-solid.md),
  detalle en [`coding-standards.md`](architecture/coding-standards.md)): un tipo por archivo,
  métodos cortos, nombres reveladores, documentación XML en todo el código de producción con
  `TreatWarningsAsErrors`.
- **Desarrollo asistido por IA gobernado**: el mismo estándar se espeja para los asistentes de
  código (Claude Code en `CLAUDE.md`, GitHub Copilot en `.github/`), con skills que automatizan
  el patrón (`/new-context`, `/new-adapter`, `/adr`).

## 4. Validación y evaluación continua

- **Cobertura ≥ 95 % por proyecto** (líneas/ramas/métodos), verificada con coverlet localmente y
  en CI: `dotnet test /p:CollectCoverage=true`.
- **CI en cada push/PR** (`.github/workflows/sonarcloud.yml`): build + tests + cobertura +
  análisis SonarCloud (bugs, vulnerabilidades, code smells, deuda). El umbral bloquea regresiones.
- **Guardrails de IA verificados por diseño y pruebas**: re-ranking por JSON con umbral de
  relevancia antes de citar; parseo defensivo de las respuestas del LLM; degradación explícita
  ("no pude…") en vez de inventar ([ADR-0015](adr/0015-arquitectura-multiagente.md)).
- **Auditoría**: cada interacción cruda con los agentes queda registrada por turno
  ([ADR-0017](adr/0017-persistencia-conversaciones.md)) — trazabilidad y materia prima de mejora.
- **Guía reproducible de validación por pares**: [`validacion_guide.md`](validacion_guide.md).

### Seguridad (DevSecOps)

| Capacidad | Estado | Herramienta |
|---|---|---|
| ✅ SAST | Implementado | SonarCloud |
| ⏳ DAST | Pendiente | — |
| ✅ SCA | Implementado | Dependabot |
| ⏳ Escaneo de imágenes Docker | Pendiente | — |
| ⚠️ Detección de secretos | Parcial | SonarCloud |

Los pendientes están registrados con su condición de disparo en [`tech-debt.md`](tech-debt.md).

## 5. Operación y mejora continua (LLMOps)

- **Versionamiento de la capa de IA**: prompts, agentes y modelos versionados en **Azure AI
  Foundry**; cada agente declara su modelo y versión en configuración
  (`appsettings.json → Foundry:Chat:Agents`, ver [`models/README.md`](../models/README.md)).
- **Observabilidad**: eventos SSE por etapa (agente, fuentes, tokens, auditoría) y bitácora de
  interacciones por turno.
- **Feedback loop**: uso real → observar y auditar → analizar → refinar prompts/agentes/RAG →
  nueva versión (ciclo descrito en la presentación del equipo y habilitado por la auditoría
  persistida).

## Flujo de trabajo del equipo

- Rama por feature; commits pequeños y descriptivos; **no se commitea sin build verde**.
- Pull requests hacia `main` con CI obligatoria; el historial de versiones se refleja en
  [`Changelog.md`](../Changelog.md).
- Gobierno de librerías: ninguna dependencia nueva sin concertarla y registrarla en SAD + ADR.
