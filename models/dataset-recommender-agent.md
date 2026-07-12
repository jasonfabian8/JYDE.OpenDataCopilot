# dataset-recommender-agent — Recomendador de datasets (RAG citado)

| Ítem | Valor |
|---|---|
| **Versión** | 5 (`appsettings → Foundry:Chat:Agents:dataset-recommender-agent`) — el agente más iterado del sistema |
| **Modelo** | `gpt-4.1-mini` (razonamiento sobre candidatos + redacción citada) |
| **Código** | `src/JYDE.OpenDataCopilot.Application/Conversation/DatasetRecommenderAgent.cs` |
| **Decisión** | [ADR-0015](../docs/adr/0015-arquitectura-multiagente.md) (primer agente del sistema) |

## Rol

El corazón del descubrimiento: dada una consulta en lenguaje natural, recupera candidatos del
índice vectorial (RAG) y produce una respuesta clara **citando sólo los datasets realmente
relevantes**. Es el agente por defecto cuando ningún otro aplica (`CanHandle` → siempre `true`).

## Flujo (RAG + re-ranking)

1. **Retrieval**: embedding de la pregunta (`IEmbeddingGenerator`) → top-k candidatos del índice
   (`IDatasetSearchIndex`, ver [`data/rag_embeddings.md`](../data/rag_embeddings.md)).
2. **Generación**: envía al LLM la memoria (`ContextHeader`) + consulta + candidatos
   (id | nombre | categoría | fuente).
3. **Re-ranking**: el LLM devuelve la respuesta **y una relevancia recalculada por candidato**;
   sólo se citan los que superan el umbral (0.5) — evita citar datasets cercanos por embedding
   pero fuera de tema.
4. **Streaming**: emite `sources` (citas ordenadas por relevancia) y luego la respuesta por
   `token`s; mantiene el hilo con el `ResponseId` del proveedor.

## Entrada (user prompt — sólo datos)

```
<ContextHeader: objetivo + datasets fijados>
Consulta del ciudadano: <pregunta>

Candidatos recuperados del índice (id | nombre | categoría | fuente):
1. [id=ddau-8cy9] Tasa de desempleo municipal (categoría: Trabajo; fuente: https://…)
…
```

## Salida esperada (JSON)

```json
{
  "respuesta": "Encontré 2 datasets útiles para tu consulta…",
  "datasets": [ { "id": "ddau-8cy9", "relevancia": 0.85 }, { "id": "abcd-1234", "relevancia": 0.2 } ]
}
```

## Guardrails

- **Sin fuente no hay respuesta**: si ningún candidato supera el umbral, responde sin citar y lo
  declara; si el índice no devuelve candidatos, se lo comunica al LLM explícitamente.
- Parseo defensivo: si el JSON no es interpretable, entrega el texto rescatado **sin citas** (no
  se cita nada cuya relevancia no esté validada).

## System prompt

> **Pendiente de insumo del equipo** — reglas, rúbrica de relevancia y esquema JSON viven
> versionados en el agente publicado en Azure AI Foundry.
