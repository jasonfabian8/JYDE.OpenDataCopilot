---
name: adr
description: Crea un nuevo Architecture Decision Record (ADR) numerado en docs/adr/ usando la plantilla estándar del proyecto. Úsalo cuando se tome una decisión de arquitectura que deba quedar registrada.
---

# Skill: nuevo ADR

Crea un ADR numerado y consistente en `docs/adr/`.

## Pasos

1. Determina el siguiente número: lista `docs/adr/` y toma el mayor `NNNN` + 1 (4 dígitos, p. ej. `0007`).
2. Pide/define un **título corto** en kebab-case → archivo `docs/adr/NNNN-titulo-kebab.md`.
3. Copia la estructura de [`docs/adr/template.md`](../../../docs/adr/template.md) y complétala:
   - Encabezado `# ADR NNNN — Título`.
   - **Estado** (normalmente `Propuesto` o `Aceptado`), **Fecha** (hoy), **Decisores**.
   - **Contexto**, **Decisión**, **Consecuencias** (positivas / trade-offs / seguimiento),
     **Alternativas consideradas**.
4. Agrega la fila correspondiente a la tabla de [`docs/adr/README.md`](../../../docs/adr/README.md).
5. Si la decisión cambia reglas de arquitectura, actualiza también
   [`docs/architecture/SAD.md`](../../../docs/architecture/SAD.md) (fuente única de verdad) y, si
   aplica, enlázala desde `/CLAUDE.md` y `.github/copilot-instructions.md`.

## Convenciones

- Una decisión = un archivo. No reescribas ADRs aceptados; si cambian, crea uno nuevo que "Reemplaza a [ADR-XXXX]" y marca el viejo como `Reemplazado por`.
- Lenguaje claro y accionable; enlaza ADRs relacionados.
