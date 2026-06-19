# Instrucciones de GitHub Copilot — OpenData Copilot

> Espejo del gobierno definido para todo el equipo y para Claude Code (`/CLAUDE.md`).
> **Fuente única de verdad:** [`docs/architecture/SAD.md`](../docs/architecture/SAD.md) y los
> [ADRs](../docs/adr/). No dupliques reglas de detalle: consúltalas allí.

## Proyecto

Asistente conversacional sobre los datos abiertos de Colombia (`datos.gov.co`). El usuario pregunta en lenguaje natural; el sistema descubre datasets, consulta datos vía API de Socrata y responde **citando la fuente**. Restricción dura: **bajo costo**.

## Arquitectura: Hexagonal + DDD

Regla de dependencias **sólo hacia adentro**: `Api → Infrastructure → Application → Domain`.

- **Domain** (`src/JYDE.OpenDataCopilot.Domain`): núcleo puro, **cero dependencias externas**.
- **Application** (`src/JYDE.OpenDataCopilot.Application`): casos de uso; **define los puertos**.
- **Infrastructure** (`src/JYDE.OpenDataCopilot.Infrastructure`): **adaptadores** de los puertos.
- **Api** (`src/JYDE.OpenDataCopilot.Api`): endpoints + composición DI por configuración.

## Reglas no negociables

1. No violar la dirección de dependencias entre capas.
2. Toda dependencia externa (HTTP, Azure/Foundry, Socrata, Mongo, índices) va **detrás de un puerto** y se implementa sólo en Infrastructure.
3. Selección de proveedor por configuración (`appsettings → Providers`), nunca hardcodeada.
4. Datos sólo vía **API de Socrata** (catálogo + SoQL); **sin web scraping**.
5. Respuestas **citadas**; si los datos no soportan la respuesta, declararlo; **no inventar cifras**.
6. **TDD por convención** (xUnit + Shouldly); preferir test primero en Domain/Application.
7. **Gobierno de librerías:** no introducir una dependencia nueva (frontend o backend) sin acuerdo
   del equipo; registrar toda adopción **actualizando el SAD y un ADR**. Base de frontend decidida:
   React + Vite + Zustand ([ADR-0008](../docs/adr/0008-stack-frontend-vite-zustand.md)).

## Convenciones de código C#

> Estándar completo (**Clean Code + SOLID**, checklist de PR):
> [`docs/architecture/coding-standards.md`](../docs/architecture/coding-standards.md).

- Aplicar **SOLID** y **Clean Code**: una sola responsabilidad por clase/método; programar contra puertos (abstracciones); métodos cortos con un nivel de abstracción; nombres reveladores;
  DRY/KISS/YAGNI; guard clauses y fail-fast; no tragar excepciones.
- **Un solo tipo por archivo** (clase/record/interfaz/enum/struct), nombre de archivo = tipo.
- `namespace` con file-scope, nullable habilitado. **Tipos explícitos por defecto**; `var` sólo en excepciones reales (p. ej. tipos anónimos).
- **Cobertura de pruebas ≥ 95% por proyecto** (coverlet); medir con `dotnet test /p:CollectCoverage=true`.
- Puertos = interfaces con prefijo `I`, pequeñas y orientadas al caso de uso.
- Adaptadores nombrados `{Proveedor}{Puerto}` sin la `I` (p. ej. `SocrataCatalogClient`).
- Lenguaje ubicuo del dominio (ver glosario en el SAD).
- **Documentación XML obligatoria** en miembros públicos. `TreatWarningsAsErrors` está activo.

> Reglas específicas por capa: ver `.github/instructions/*.instructions.md` (aplican por ruta).
