# models/ — Agentes de IA del Copilot

En la guía del concurso, `models/` contiene modelos entrenados (`predictive/`, `llm_rag/`). Este
proyecto **no entrena modelos propios**: consume LLMs y embeddings gestionados vía **Azure AI
Foundry** ([ADR-0004](../docs/adr/0004-azure-foundry-gpt41mini.md)), donde los agentes —sus
instrucciones (system prompts), modelo y versión— están **publicados y versionados**. Esta
carpeta documenta cada agente del sistema multiagente
([ADR-0015](../docs/adr/0015-arquitectura-multiagente.md)): su rol, modelo, versión, entradas,
salidas y guardrails.

## Filosofía: modelos de bajo costo

El costo es **restricción dura** del proyecto ([SAD §2](../docs/architecture/SAD.md#2-drivers-y-atributos-de-calidad)).
Por eso cada agente usa el modelo **más económico que resuelve su tarea**:

- **`gpt-4o-mini`** para tareas ligeras de clasificación/resumen (enrutar, rastrear objetivo,
  puntuar categorías).
- **`gpt-4.1-mini`** para tareas de razonamiento sobre contexto (recomendar con re-ranking,
  analizar esquemas, escribir SoQL).
- Los **user prompts llevan sólo datos**; las reglas, rúbricas y esquemas JSON viven en las
  instrucciones del agente en Foundry (versionadas) — menos tokens por turno.
- La arquitectura multiagente reduce el error y el consumo: prompts pequeños y especializados en
  lugar de un prompt monolítico.

## Catálogo de agentes

Modelo y versión según `src/JYDE.OpenDataCopilot.Api/appsettings.json → Foundry:Chat:Agents`:

| Agente | Versión | Modelo | Responsabilidad |
|---|---|---|---|
| [`router-agent`](router-agent.md) | 4 | gpt-4o-mini | Decide qué agente especializado atiende cada mensaje |
| [`objective-tracker-agent`](objective-tracker-agent.md) | 1 | gpt-4o-mini | Resume y actualiza el objetivo de la conversación (memoria) |
| [`category-recommender-agent`](category-recommender-agent.md) | 2 | gpt-4o-mini | Recomienda qué categorías del catálogo cargar |
| [`dataset-recommender-agent`](dataset-recommender-agent.md) | 5 | gpt-4.1-mini | Recomienda datasets relevantes, citando la fuente |
| [`dataset-analyst-agent`](dataset-analyst-agent.md) | 3 | gpt-4.1-mini | Explica esquemas y evalúa cruces/correlaciones entre datasets |
| [`figures-agent`](figures-agent.md) | 1 | gpt-4.1-mini | Consulta datos reales vía SoQL: tabula y grafica cifras |
| [`embeddings`](embeddings.md) | — | text-embedding-3-small | Vectorización para la búsqueda semántica (RAG) |

## Cómo se orquestan

`CopilotOrchestrator` (Application) recibe la pregunta, la enruta con el `router-agent` (con
degradación a reglas si falla), reemite el flujo de eventos del agente elegido, actualiza el
objetivo con el `objective-tracker-agent` y anexa la auditoría del turno. Diagramas:
[`docs/architecture/diagramas.md`](../docs/architecture/diagramas.md).

## Guardrails comunes

- **Sin fuente no hay respuesta**: sólo se citan datasets cuya relevancia recalculada supera el
  umbral (0.5 por defecto); si nada es relevante, el agente lo declara.
- **Nunca se inventan cifras**: los números provienen de ejecutar SoQL sobre la fuente oficial;
  si la consulta falla, se explica con honestidad.
- **Parseo defensivo**: toda respuesta del LLM se interpreta con `JsonText.FirstJsonObject`
  (robusto ante prosa, vallas ``` y JSON duplicado); si no hay JSON válido, el agente degrada de
  forma explícita en lugar de fallar.
- **Auditoría**: cada interacción (input/output crudos) queda registrada por turno
  ([`data/conversaciones.md`](../data/conversaciones.md)).

## Configuración e infraestructura

- El backend consume los agentes por el puerto `IChatCompletion` con `ChatPrompt(agent, input,
  previousResponseId)`; el adaptador `FoundryChatCompletion` resuelve nombre/versión/modelo desde
  configuración, y `FakeChatCompletion` permite desarrollar sin credenciales ni costo
  (`Providers:Chat = Fake | Foundry`).
- El threading (hilo de conversación) usa el `ResponseId` del proveedor.
- **System prompts**: viven versionados en Foundry. Su transcripción a esta carpeta está
  pendiente de que el equipo los provea (ver sección "System prompt" en cada archivo).
