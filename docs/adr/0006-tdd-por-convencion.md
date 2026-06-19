# ADR 0006 — TDD por convención (sin enforcement automático)

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

Buscamos seguridad ante regresiones y un diseño guiado por pruebas. La arquitectura hexagonal hace que **dominio y aplicación sean unitariamente testeables** mediante dobles de los puertos, sin infraestructura. El punto de decisión es el **mecanismo de cumplimiento**: forzar TDD con hooks
(`PostToolUse`/pre-commit) que ejecutan la suite en cada cambio añade latencia al ciclo interno de desarrollo y puede bloquear de forma espuria, mientras que una convención verificada en CI mantiene el ciclo ágil y comprueba igualmente el resultado (incluida la cobertura).

## Decisión

Adoptar **TDD como convención documentada**, no obligada por herramientas:

- Práctica preferida: escribir el test antes del código (Red-Green-Refactor), sobre todo en `Domain` y `Application`.
- **Sin hooks** de `PostToolUse`/pre-commit que ejecuten tests automáticamente.
- Framework: **xUnit**. Aserciones fluidas con **Shouldly** (licencia libre) en lugar de FluentAssertions v8+ (licencia comercial) para no incurrir en costo.
- Los tests se ejecutan manualmente y en CI: `dotnet test`.
- **Meta de cobertura: ≥ 95% por proyecto** (líneas, ramas y métodos). Umbral configurado con **coverlet** en `Directory.Build.props` (sólo se evalúa al recolectar cobertura, `dotnet test /p:CollectCoverage=true`, para no frenar el `dotnet test` cotidiano) y exigido en CI.

## Consecuencias

- **Positivas:** agilidad; el equipo decide el ritmo; sin fricción de tooling. 
- **Negativas / trade-offs:** depende de disciplina → mitigado con revisión de PRs y CI que corre la suite.
- **Seguimiento:** si la cobertura baja en zonas críticas, reconsiderar enforcement selectivo.

## Alternativas consideradas

- **TDD estricto con hooks** — descartado por ahora: añade latencia al ciclo de desarrollo y bloqueos espurios sin beneficio proporcional frente a la verificación en CI.
- **Tests después (pragmático)** — descartado como norma: perdemos el beneficio de diseño del TDD.
