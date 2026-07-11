# ADR 0007 — Estándar de codificación: Clean Code, SOLID y un tipo por archivo

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

La base de código será mantenida por **varios contribuyentes** —humanos y asistentes de IA (Claude Code, GitHub Copilot)— y debe permanecer legible, consistente y con bajo índice de defectos a medida que crece. Sin un estándar explícito surgen inconsistencias de estilo y de diseño que elevan el costo cognitivo, dificultan la revisión y el refactor, y degradan la mantenibilidad y la testabilidad. Necesitamos un conjunto de prácticas **accionable**, que sirva de referencia común y sea, en lo posible, **verificable por herramientas** (analizadores, estilo en build).

## Decisión

Adoptar como obligatorio un **estándar de codificación** documentado en
[`docs/architecture/coding-standards.md`](../architecture/coding-standards.md), que exige:

- **Principios SOLID** (con DIP/OCP materializados por la arquitectura hexagonal de puertos y adaptadores, ver [ADR-0003](0003-ports-adapters-intercambiables.md)).
- **Clean Code**: nombres reveladores, funciones pequeñas con un solo nivel de abstracción, DRY/KISS/YAGNI, guard clauses/fail-fast, manejo de errores correcto.
- **Un solo tipo por archivo** (clase/record/interfaz/enum/struct), con nombre de archivo = tipo.
- Documentación XML en miembros públicos y `TreatWarningsAsErrors` activo.

El estándar es la **fuente única de verdad**; `CLAUDE.md` y `.github/copilot-instructions.md` lo referencian. Enforcement parcial vía analizadores en `.editorconfig`; las reglas de organización de archivos (SA1402/SA1649) se activan al incorporar StyleCop.Analyzers.

## Consecuencias

- **Positivas:** código consistente y fácil de revisar; menor deuda técnica; menor costo de onboarding y de revisión; guía clara para IA y humanos.
- **Negativas / trade-offs:** disciplina adicional en cada PR; algunas reglas solo se automatizan al añadir StyleCop.
- **Seguimiento:** evaluar incorporar StyleCop.Analyzers para enforcement completo de organización de archivos.

## Alternativas consideradas

- **Sin estándar explícito** — descartado: inconsistencia y revisiones más lentas.
- **Habilitar StyleCop completo desde ya** — diferido: con `warnings-as-errors` introduce mucho ruido inicial; se adopta primero como convención documentada + analizadores selectivos.
