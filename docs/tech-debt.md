# Deuda técnica

Registro vivo de deuda técnica: diferimientos conscientes que respetan el gobierno (SAD/ADR) hoy,
pero que deben resolverse cuando se cumpla su condición de disparo. Una deuda = una fila. Al saldarla,
mueve la fila a **Resueltas** con la fecha y el commit/PR.

## Abiertas

| ID | Deuda | Contexto y razón del diferimiento | Condición de disparo (cuándo resolver) | Referencias |
|----|-------|-----------------------------------|----------------------------------------|-------------|
| TD-001 | Selección de adaptador del contexto **Catalog** por configuración (`Providers`). | El composition root (`Program.cs`) registra hoy `SocrataCatalogClient` (fuente) e `InMemoryCatalogRepository` (repositorio) de forma directa. Hay **un único adaptador por puerto** y `Catalog` **no** está en la matriz `Providers` del ADR-0003. Añadir un `switch` de una sola rama sería YAGNI; el ADR contempla implementación **incremental** de los contratos. | Al incorporar un **segundo adaptador** de `ICatalogSource` o `ICatalogRepository` (p. ej. un repositorio persistente): añadir claves `Providers:CatalogSource` / `Providers:CatalogRepository`, la rama de registro DI (lanzando en valor desconocido) y su test; actualizar SAD/ADR-0003. | [ADR-0003](adr/0003-ports-adapters-intercambiables.md); `src/JYDE.OpenDataCopilot.Api/Program.cs` |
| TD-004 | **System prompts de los agentes** no transcritos en `models/`. | Las instrucciones viven versionadas en Azure AI Foundry; el equipo las proveerá para documentarlas junto a cada agente. | Cuando el equipo entregue los prompts: completar la sección "System prompt" de cada archivo en `models/`. | [`models/README.md`](../models/README.md) |
| TD-005 | **OpenAPI/Swagger generado** no habilitado; `api_spec.md` se mantiene a mano. | La API no expone Swagger; añadirlo implica una decisión de librería (gobierno de dependencias). | Al crecer la superficie HTTP o integrarse un consumidor externo: habilitar OpenAPI y generar la spec; registrar ADR. | [`api_spec.md`](api_spec.md) |
| TD-006 | **DevSecOps incompleto**: DAST y escaneo de imágenes Docker pendientes; detección de secretos solo parcial (SonarCloud). | El pipeline actual cubre SAST (SonarCloud) y SCA (Dependabot); el resto se difirió por alcance de concurso. | Antes de exponer un entorno público estable: añadir DAST, escaneo de imágenes y detección de secretos dedicada al pipeline. | [`marco_metodologico.md §Seguridad`](marco_metodologico.md#seguridad-devsecops) |
| TD-007 | **Sin cron de ingesta ni CODEOWNERS** (sugeridos por la guía del concurso). | La ingesta es bajo demanda desde la app (decisión de producto: el usuario elige categorías); CODEOWNERS es innecesario con el tamaño actual del equipo. | Cron: si se requiere frescura automática del catálogo. CODEOWNERS: si el equipo crece o se reparte por módulos. | [`estructura_repositorio.md`](estructura_repositorio.md) |

## Resueltas

| ID | Deuda | Resolución | Fecha |
|----|-------|------------|-------|
| TD-002 | **`resources/portada.png`** faltante (imagen de la diapositiva principal, pedida por la guía del concurso). | Exportada la diapositiva 1 de `resources/Presentación OpenData.pptx` como `resources/portada.png` (1920×1080). | 2026-07-11 (rama `feature/copilot`) |
| TD-003 | **Reporte final en PDF** (`reports/reporte_final.pdf` de la guía del concurso). | Módulo [`reports/`](../reports/README.md): `generar_reporte.py` produce `reporte_final.pdf` + `figures/` consumiendo el sistema real (catálogo vía `/catalog/*`, agentes/RAG/auditoría vía `/chat` SSE) y métricas extraídas del repositorio; regenerable en cualquier momento. | 2026-07-11 (rama `feature/copilot`) |
