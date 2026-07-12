# dataset-analyst-agent — Analista de esquemas y cruces

| Ítem | Valor |
|---|---|
| **Versión** | 3 (`appsettings → Foundry:Chat:Agents:dataset-analyst-agent`) |
| **Modelo** | `gpt-4.1-mini` (razonamiento sobre esquemas de columnas) |
| **Código** | `src/JYDE.OpenDataCopilot.Application/Conversation/DatasetAnalystAgent.cs` |
| **Decisión** | [ADR-0015 §Actualización 2026-07-11](../docs/adr/0015-arquitectura-multiagente.md) |

## Rol

Entiende los datasets **desde sus metadatos** (columnas) ya almacenados en el catálogo:

- **(a)** describe las columnas/esquema de un dataset en lenguaje claro, o
- **(b)** evalúa si dos datasets pueden **cruzarse o correlacionarse** por columnas comunes
  (municipio, año, código DANE…).

**No consulta datos reales** — eso corresponde al [`figures-agent`](figures-agent.md).

## Disparadores (`CanHandle`, reserva del enrutador)

`columna`, `campo`, `esquema`, `estructura`, `atributo`, `variable`, `cruz*`, `correlacion`,
`combinar`, `relacionar`.

## Flujo

1. Resuelve los datasets de la consulta: **primero los fijados por el usuario** (memoria), luego
   los mejores por búsqueda semántica, hasta 8 candidatos (`DatasetCandidates.ResolveAsync`).
2. Trae el **esquema completo** (columnas con `FieldName` y `DataType`) del repositorio del
   catálogo ([`data/catalogo_datasets.md`](../data/catalogo_datasets.md)).
3. Pide al LLM la explicación o la evaluación de cruce, con relevancia recalculada por dataset.
4. Cita **sólo los datasets usados** (re-ranking con umbral 0.5) y responde en streaming
   manteniendo el hilo.

## Salida esperada (JSON)

```json
{
  "respuesta": "El dataset X tiene columnas de municipio y año, así que puede cruzarse con Y por…",
  "datasets": [ { "id": "ddau-8cy9", "relevancia": 0.9 } ]
}
```

## Guardrails

- Sólo razona sobre metadatos reales del catálogo; si el esquema no soporta el cruce, lo declara.
- Parseo defensivo del JSON; degrada al texto rescatado sin citas si no es interpretable.

## System prompt

> **Pendiente de insumo del equipo** — versionado en el agente publicado en Azure AI Foundry.
