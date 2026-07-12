# Estructura del repositorio — mapeo con la guía del concurso

Guía de navegación del repositorio para evaluadores y colaboradores. Mapea la estructura sugerida
por el concurso **Datos al Ecosistema 2026** (nivel Avanzado-IA; ver
[`docs/example/`](example/Sugerencia_EstructuraRepositorio_Avanzado.txt)) contra la estructura
real de este proyecto, que es una solución **.NET + React con arquitectura hexagonal/DDD** (no un
proyecto Python/ML como el del ejemplo). Donde la carpeta sugerida no aplica tal cual, se indica
**dónde vive el equivalente** y por qué.

> Fuente única de verdad de la arquitectura: [`docs/architecture/SAD.md`](architecture/SAD.md) y
> los [ADRs](adr/README.md).

## Estructura real

```
├── README.md            Guía principal: ficha técnica, funcionalidades y resultados
├── LICENSE              Licencia abierta (MIT)
├── .gitignore           Exclusión de binarios, credenciales y temporales
├── Changelog.md         Registro cronológico de versiones (reconstruido del historial git)
├── docker-compose.yml   Dependencias locales de desarrollo (equivale a deployments/docker)
├── resources/           Material visual del concurso (presentación .pptx y .pdf)
├── data/                Documentación de las estructuras de datos (catálogo, RAG, conversaciones)
├── models/              Documentación de los agentes de IA (modelo, versión, rol, prompts)
├── docs/                Documentación del proyecto (ver detalle abajo)
├── src/                 Backend .NET por capas (Domain, Application, Infrastructure, Api)
├── web/                 Frontend React (Vite + Zustand + Tailwind)
├── tests/               Proyectos de prueba por capa (xUnit + Shouldly; Vitest en web/)
├── .github/             CI (SonarCloud) e instrucciones de GitHub Copilot
└── .claude/             Skills de Claude Code que automatizan el patrón del proyecto
```

## Mapeo elemento a elemento

| Estructura sugerida (concurso) | En este repositorio | Notas |
|---|---|---|
| `RECURSOS/` (pptx, pdf, portada) | [`resources/`](../resources/) | Mismo propósito, nombre en minúsculas. Contiene la presentación en `.pptx` y `.pdf`. La portada está pendiente (ver [tech-debt](tech-debt.md)). |
| `README.md` (ficha técnica y resultados) | [`README.md`](../README.md) | Incluye problema, solución, funcionalidades, ficha técnica y resultados validados. |
| `LICENSE` | [`LICENSE`](../LICENSE) | MIT. |
| `.gitignore` | [`.gitignore`](../.gitignore) | ✔ |
| `requirements.txt` / `environment.yml` | `JYDE.OpenDataCopilot.slnx`, `Directory.Build.props`, `src/**/*.csproj`, `web/package.json` | En .NET las dependencias se declaran por proyecto (`.csproj`) y se restauran con `dotnet restore`; en el frontend, `web/package.json` + `npm install`. |
| `Changelog.md` | [`Changelog.md`](../Changelog.md) | Formato *Keep a Changelog*; versiones propuestas a partir de los merges a `main`. |
| `docs/architecture/` (diagramas) | [`docs/architecture/`](architecture/) | [`SAD.md`](architecture/SAD.md) (C4 + decisiones) y [`diagramas.md`](architecture/diagramas.md) (DDD, agentes, secuencia, UML en Mermaid). |
| `docs/api_spec.md` | [`docs/api_spec.md`](api_spec.md) | Especificación de los endpoints reales (REST + SSE). |
| `docs/public_impact_assessment.md` | [`docs/public_impact_assessment.md`](public_impact_assessment.md) | Impacto, ética (IA responsable) y mitigación de sesgos. |
| `docs/data_dictionary.md` | [`docs/data_dictionary.md`](data_dictionary.md) | Resumen del modelo de datos; el detalle por almacén vive en [`data/`](../data/README.md). |
| `docs/planteamiento_problema.md` | [`docs/planteamiento_problema.md`](planteamiento_problema.md) | ✔ |
| `docs/marco_metodologico.md` | [`docs/marco_metodologico.md`](marco_metodologico.md) | Metodología real del equipo (hexagonal + DDD + TDD + ADRs + CI), en el espíritu de CRISP-ML adaptado a un producto conversacional. |
| `docs/fuentes_datos.md` | [`docs/fuentes_datos.md`](fuentes_datos.md) | datos.gov.co vía API Socrata (catálogo + SoQL), sin scraping. |
| `docs/conclusiones.md` | [`docs/conclusiones.md`](conclusiones.md) | Hallazgos, limitaciones y próximos pasos. |
| `docs/validación_guide.md` | [`docs/validacion_guide.md`](validacion_guide.md) | Mismo documento; el nombre se normalizó sin tilde para evitar problemas de encoding en URLs/SO. |
| `data/` (raw, processed, realtime, external) | [`data/`](../data/README.md) | Los datos **no se copian al repositorio**: se consultan en vivo vía SoQL sobre datos.gov.co ([ADR-0002](adr/0002-socrata-sin-scraping.md), [ADR-0005](adr/0005-estrategia-datos-hibrida.md)) y se persisten metadatos/vectores/conversaciones en MongoDB Atlas. La carpeta documenta **las estructuras de esos almacenes**. |
| `notebooks/` | *(no aplica)* | No hay flujo de notebooks: la exploración y el análisis los ejecutan los agentes de IA en runtime (p. ej. el agente de cifras genera y ejecuta SoQL). |
| `src/` (agents, data_pipeline, features…) | [`src/`](../src/) | Backend .NET por capas hexagonales. Los **agentes** viven en `src/JYDE.OpenDataCopilot.Application/Conversation/`; la **ingesta** (equivalente a `data_pipeline/`) en los casos de uso de `Catalog`/`Search` y los adaptadores `Socrata*`/`Mongo*` de Infrastructure. |
| `models/` (predictive, llm_rag, simulation) | [`models/`](../models/README.md) | No hay modelos entrenados localmente: los modelos LLM/embeddings se consumen y **versionan en Azure AI Foundry** ([ADR-0004](adr/0004-azure-foundry-gpt41mini.md)). La carpeta documenta cada agente (equivale a `llm_rag/`): modelo, versión, rol y prompts. |
| `reports/` (figures, reporte_final.pdf) | README (resultados) + [`docs/conclusiones.md`](conclusiones.md) + [`resources/`](../resources/) | Los "resultados visibles" son la demo, la presentación y las conclusiones. El reporte final en PDF está pendiente (ver [tech-debt](tech-debt.md)). |
| `tests/` (unit, integration, bias_tests) | [`tests/`](../tests/) | Convención .NET: un proyecto de prueba **por capa** (`*.Domain.Tests`, `*.Application.Tests`, `*.Infrastructure.Tests`, `*.Api.Tests`) + Vitest en `web/`. El equivalente a `bias_tests/` son los **guardrails verificados por tests**: sin fuente no hay respuesta, nunca se inventan cifras (ver [ADR-0015](adr/0015-arquitectura-multiagente.md)). |
| `.github/workflows/` (CI + cron de datos) | [`.github/workflows/`](../.github/workflows/) | `sonarcloud.yml`: build + tests + cobertura + análisis SonarCloud en cada push/PR. No hay cron de ingesta: la ingesta es **bajo demanda** por categorías desde la propia app (`POST /catalog/ingest`). |
| `config/` | `src/JYDE.OpenDataCopilot.Api/appsettings*.json` | Configuración .NET por proyecto. La selección de proveedores (`Providers`) permite intercambiar adaptadores sin tocar código ([ADR-0003](adr/0003-ports-adapters-intercambiables.md)); los agentes y sus versiones se configuran en `Foundry:Chat:Agents`. |
| `deployments/` (docker, kubernetes, serverless) | [`docker-compose.yml`](../docker-compose.yml) + [SAD §12](architecture/SAD.md#12-despliegue-objetivo) | Desarrollo: docker compose local. Producción objetivo: Azure Container Apps/App Service + Static Web Apps + Atlas + Foundry (servicios gestionados; no se administran manifiestos k8s propios). |

## Documentación adicional (no exigida por el ejemplo)

| Documento | Propósito |
|---|---|
| [`docs/requirements/requerimientos_iniciales.md`](requirements/requerimientos_iniciales.md) | Requerimientos funcionales y no funcionales derivados del SAD y los ADRs. |
| [`docs/requirements/user_stories.md`](requirements/user_stories.md) | Historias de usuario por épica/bounded context, con criterios de aceptación. |
| [`docs/adr/`](adr/README.md) | Architecture Decision Records: una decisión = un archivo numerado. |
| [`docs/architecture/coding-standards.md`](architecture/coding-standards.md) | Estándar de codificación (Clean Code + SOLID). |
| [`docs/tech-debt.md`](tech-debt.md) | Deuda técnica y diferimientos conscientes, con condición de disparo. |
