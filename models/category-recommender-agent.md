# category-recommender-agent — Recomendador de categorías del catálogo

| Ítem | Valor |
|---|---|
| **Versión** | 2 (`appsettings → Foundry:Chat:Agents:category-recommender-agent`) |
| **Modelo** | `gpt-4o-mini` (puntuación de una lista cerrada → modelo de mínimo costo) |
| **Código** | `src/JYDE.OpenDataCopilot.Application/Conversation/CategoryRecommenderAgent.cs` |
| **Decisión** | [ADR-0015 §Actualización 2026-07-11](../docs/adr/0015-arquitectura-multiagente.md) |

## Rol

Cuando el catálogo cargado **no cubre** la necesidad del usuario, este agente recomienda qué
**categorías** de datos.gov.co conviene ingerir. Recibe la lista completa de categorías (con su
conteo de datasets) y cuáles ya están cargadas, y devuelve la relevancia de cada una más una
**consulta sugerida** para reintentar después de cargar.

## Disparadores (`CanHandle`, reserva del enrutador)

Mensajes que contienen `categor`, `carg` o `descarg`.

## Entrada (user prompt — sólo datos)

Memoria (`ContextHeader`: objetivo + datasets fijados) + pregunta + lista de categorías con
conteo, marcando las ya cargadas (vía `ICatalogSource.GetCategoriesAsync` y
`ICatalogRepository.GetLoadedCategoriesAsync`).

## Salida esperada (JSON)

```json
{
  "respuesta": "Para tu consulta conviene cargar…",
  "consulta": "¿Qué datasets hay sobre desempleo juvenil?",
  "categorias": [ { "nombre": "Trabajo", "relevancia": 0.9 }, … ]
}
```

## Comportamiento en la interfaz

Las categorías con relevancia ≥ umbral (`Conversation:CategoryRelevanceThreshold`, 0.5) se emiten
como evento SSE `categories` y el frontend las muestra como **botones**: al hacer clic se ingiere
esa categoría (`POST /catalog/ingest`), se reconstruye el índice (`POST /search/index`) y se
**re-pregunta** automáticamente la consulta sugerida.

## Degradación (resiliencia)

Parseo defensivo del JSON: si no es interpretable, responde con el texto rescatado (o pide
reformular) y **no emite recomendaciones** sin relevancia validada.

## System prompt

> **Pendiente de insumo del equipo** — versionado en el agente publicado en Azure AI Foundry.
