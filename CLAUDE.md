# CLAUDE.md — OpenData Copilot

Gobierno para asistentes de IA (Claude Code) y el equipo. La **fuente única de verdad** de la arquitectura es [`docs/architecture/SAD.md`](docs/architecture/SAD.md) y los [ADRs](docs/adr/). Este archivo resume las reglas operativas; si hay duda, gana el SAD/ADR.

> El mismo gobierno está espejado para GitHub Copilot en `.github/copilot-instructions.md` y 
> `.github/instructions/`. No dupliques reglas de detalle: enlázalas al SAD/ADR.

## Qué es

Asistente conversacional sobre los datos abiertos de Colombia (`datos.gov.co`). El usuario pregunta en lenguaje natural; el sistema descubre datasets, consulta datos y responde **citando la fuente**. Contexto: concurso Datos al Ecosistema 2026, recursos propios (**el costo es restricción dura**).

## Arquitectura (resumen — detalle en el SAD)

Hexagonal + DDD. Regla de dependencias **sólo hacia adentro**:

```
Api ──► Infrastructure ──► Application ──► Domain
```

- **Domain** (`src/JYDE.OpenDataCopilot.Domain`): núcleo puro. **Cero** dependencias externas.
- **Application** (`src/JYDE.OpenDataCopilot.Application`): casos de uso, **define los puertos**.
- **Infrastructure** (`src/JYDE.OpenDataCopilot.Infrastructure`): **adaptadores** de los puertos.
- **Api** (`src/JYDE.OpenDataCopilot.Api`): endpoints + composición DI por configuración.

Cada capa tiene su propio `CLAUDE.md` con reglas específicas. Léelo antes de editar esa capa.

## Reglas no negociables

1. **No violar la dirección de dependencias.** Domain no referencia nada; Application no referencia Infrastructure; los adaptadores viven sólo en Infrastructure.
2. **Dependencias externas siempre detrás de un puerto** (interfaz). Nada de `HttpClient`, SDKs de Azure/Socrata/Mongo, etc. en Domain o Application.
3. **Selección de proveedor por configuración** (`appsettings → Providers`), nunca hardcodeada. Ver [ADR-0003](docs/adr/0003-ports-adapters-intercambiables.md).
4. **Datos sólo vía API de Socrata** (catálogo + SoQL). Sin web scraping
   ([ADR-0002](docs/adr/0002-socrata-sin-scraping.md)).
5. **Respuestas citadas.** Toda respuesta basada en datos incluye su fuente. Si los datos no soportan la respuesta, decláralo; **no inventes cifras**.
6. **TDD por convención** ([ADR-0006](docs/adr/0006-tdd-por-convencion.md)): preferir test primero en Domain/Application. xUnit + Shouldly. No hay hooks que lo fuercen.
7. **Cobertura ≥ 95% por proyecto.** Umbral configurado con coverlet (líneas/ramas/métodos); se verifica al recolectar cobertura y en CI. Mídela con `dotnet test /p:CollectCoverage=true`.

## Convenciones de código

> Estándar completo (**Clean Code + SOLID**, nombres, funciones, errores, tests, checklist de PR):
> [`docs/architecture/coding-standards.md`](docs/architecture/coding-standards.md). Síguelo siempre.

- **SOLID** y **Clean Code** son obligatorios: responsabilidad única, programar contra abstracciones (puertos), métodos cortos con un solo nivel de abstracción, nombres reveladores, DRY/KISS/YAGNI.
- C# moderno: `namespace` con file-scope, nullable habilitado. **Tipos explícitos por defecto**; `var` sólo en excepciones reales (p. ej. tipos anónimos).
- **Un solo tipo por archivo**: una clase, `record`, interfaz, `enum` o `struct` por archivo, y el **nombre del archivo coincide con el del tipo** (p. ej. `Dataset.cs` → `class Dataset`). Facilita localizar y mantener el código.
- Interfaces (puertos) con prefijo `I`. Un puerto pequeño y orientado al caso de uso.
- Nombres de dominio en el **lenguaje ubicuo** (ver glosario en el SAD): `Dataset`, `Catalog`,
  `Query`, etc.
- **Documentación XML obligatoria** en todo el código (`GenerateDocumentationFile`): cada tipo y
  miembro público lleva su comentario `/// <summary>`. CS1591 está suprimido **sólo temporalmente** durante la Fase 0; al documentar el esqueleto se retira de `Directory.Build.props`.
- `TreatWarningsAsErrors` está activo: mantén el build limpio.

## Estructura de carpetas

```
src/        capas .NET (Domain, Application, Infrastructure, Api)
web/        frontend React (Vite)
tests/      proyectos de prueba por capa
docs/       SAD (architecture/) y decisiones (adr/)
.claude/    skills propios (new-context, new-adapter, adr)
.github/    instrucciones de GitHub Copilot (espejo del gobierno)
```

## Comandos

```bash
dotnet build                       # compilar la solución
dotnet test                        # ejecutar todas las pruebas
dotnet test /p:CollectCoverage=true   # pruebas + cobertura (umbral >= 95% por proyecto)
dotnet run --project src/JYDE.OpenDataCopilot.Api   # levantar la API
docker compose up -d               # dependencias locales (pgvector, qdrant)
```

## Flujo de trabajo

- Rama por feature; commits pequeños y descriptivos. **No commitear sin que el build pase. Y sin autorización previa**
- Skills disponibles para mantener el patrón: `/new-context`, `/new-adapter`, `/adr`.
- Al tomar una decisión de arquitectura, registra un ADR (`/adr`) y, si aplica, actualiza el SAD.
