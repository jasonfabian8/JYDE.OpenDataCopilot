# figures-agent — Agente de cifras (SoQL sobre datos reales)

| Ítem | Valor |
|---|---|
| **Versión** | 1 (`appsettings → Foundry:Chat:Agents:figures-agent`) |
| **Modelo** | `gpt-4.1-mini` (generación de SoQL a partir de esquemas) |
| **Código** | `src/JYDE.OpenDataCopilot.Application/Conversation/FiguresAgent.cs` |
| **Decisión** | [ADR-0015 §Actualización 2026-07-11](../docs/adr/0015-arquitectura-multiagente.md) |

## Rol

El único agente que toca **datos reales**: responde preguntas de cifras (conteos, sumas,
promedios, tendencias, rankings) generando una consulta **SoQL**, ejecutándola en vivo sobre
datos.gov.co y emitiendo **artefactos** de tabla y gráfico.

## Disparadores (`CanHandle`, reserva del enrutador)

`cuánt*/cuant*`, `cifra`, `total`, `suma`, `promedio`, `gráfic*/grafic*`, `tabla`, `tabular`,
`estadística`, `tendencia`, `ranking`.

## Flujo

1. Resuelve hasta 3 datasets candidatos: **primero los fijados por el usuario**, luego los
   mejores por búsqueda semántica, con su esquema completo de columnas.
2. El LLM elige el dataset y escribe la **consulta SoQL** (usando `FieldName`/`DataType` reales
   de las columnas), con una explicación y una sugerencia de gráfico opcional.
3. El backend **ejecuta** el SoQL por el puerto `IDataQuery` (adaptador `SocrataDataQuery` sobre
   la API SODA) — el LLM nunca "recuerda" cifras, sólo escribe la consulta.
4. Emite el artefacto `table` (columnas + filas reales), el `chart` si aplica (`bar`/`line` con
   ejes X/Y) y la explicación por `token`s, citando el dataset consultado.

## Salida esperada del LLM (JSON)

```json
{
  "datasetId": "ddau-8cy9",
  "soql": "SELECT municipio, count(*) AS casos GROUP BY municipio ORDER BY casos DESC LIMIT 10",
  "explicacion": "Estos son los 10 municipios con más casos registrados…",
  "chart": { "type": "bar", "x": "municipio", "y": "casos" }
}
```

## Guardrails (honestidad con las cifras)

- **Nunca inventa cifras**: todo número mostrado proviene de ejecutar la consulta sobre la
  fuente oficial en ese momento.
- Si no hay dataset con datos para la consulta, lo dice y sugiere cargar la categoría
  correspondiente.
- Si el LLM no produce SoQL válido o la ejecución falla, **lo explica con honestidad** y pide
  reformular — no fabrica un resultado.
- Los artefactos guardan las filas ya tabuladas para redibujarse al recuperar la conversación
  ([`data/conversaciones.md`](../data/conversaciones.md)).

## System prompt

> **Pendiente de insumo del equipo** — versionado en el agente publicado en Azure AI Foundry.
