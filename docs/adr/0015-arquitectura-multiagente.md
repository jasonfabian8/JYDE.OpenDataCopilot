# ADR 0015 — Conversación: arquitectura multiagente (Copilot orquestador + agentes especializados)

- **Estado:** Aceptado
- **Fecha:** 2026-06-24
- **Decisores:** Equipo OpenData Copilot

## Contexto

El asistente debe resolver necesidades distintas (descubrir datasets, calcular cifras, comparar,
explicar) sobre un catálogo heterogéneo. Un único prompt monolítico que lo haga todo es frágil,
caro en tokens y difícil de evolucionar. Buscamos especialización, menor tasa de error, menor
consumo de tokens y capacidad de crecer agregando capacidades sin reescribir el núcleo.

## Decisión

Adoptar una **arquitectura multiagente** en el bounded context `Conversation`:

- Un **Copilot orquestador** (`CopilotOrchestrator`, caso de uso) es el punto de entrada con el que
  conversa el usuario. Según la intención, **enruta** hacia un agente especializado mediante un
  `IAgentRouter` (estrategia intercambiable: determinista ahora, basada en LLM/function-calling
  cuando haya más agentes).
- Cada capacidad es un **agente** que implementa `IConversationAgent` (nombre, descripción y
  `HandleAsync` que produce un flujo de eventos). Primer agente: **`DatasetRecommenderAgent`**
  (recomendar datasets usando Search + LLM, citando la fuente). Siguientes: agente de **cifras**
  (SoQL), comparaciones, etc.
- El LLM se consume tras el puerto **`IChatCompletion`** (streaming). Adaptadores: **`Fake`**
  (desarrollo/$0, sin credenciales) y **`Foundry`** (GPT-4.1-mini), seleccionables por
  `Providers:Chat` ([ADR-0003](0003-ports-adapters-intercambiables.md), [ADR-0004](0004-azure-foundry-gpt41mini.md)).
- **Streaming SSE** de extremo a extremo: el orquestador emite eventos `agent` → `sources` (citas)
  → `token`… → `done`.
- **Guardrails:** si ningún dataset es relevante o los datos no sustentan la respuesta, el agente lo
  declara; nunca inventa cifras (regla no negociable de respuestas citadas).
- **Sin estado** por ahora (single-turn); el historial (`IConversationStore`) se añadirá después.

## Consecuencias

- **Positivas:** especialización (mejor calidad, menos error), prompts pequeños (menos tokens),
  extensible (nuevo agente = nueva clase + registro, sin tocar el orquestador), testeable con
  dobles, intercambiable de proveedor de LLM por configuración.
- **Negativas / trade-offs:** más piezas y una etapa de enrutamiento; el enrutamiento por LLM
  consumirá algunos tokens (se mitiga con enrutamiento determinista mientras haya pocos agentes).
- **Seguimiento:** implementar `FoundryChatCompletion` (requiere credenciales), el agente de cifras
  (`IDataQuery`/SoQL) y el enrutador basado en LLM cuando haya ≥ 2 agentes.

## Actualización 2026-07-11 — Enrutador LLM, agente de categorías y re-ranking por JSON

Con ≥ 2 agentes se materializan piezas previstas en el seguimiento:

- **Enrutador basado en LLM** (`LlmAgentRouter`): recibe la consulta + la lista de agentes (nombre y
  descripción) y elige cuál atiende (JSON `{"agente": "..."}`). Es un agente de Foundry (`router-agent`,
  `Conversation:RouterAgent`). **Degrada a reglas** (`DefaultAgentRouter`, por `CanHandle`) si el
  enrutador falla o no está disponible. `IAgentRouter.Route` pasó a **`RouteAsync`**. En local/Fake se
  usa el enrutador por reglas.
- **`CategoryRecommenderAgent`**: recomienda qué **categorías** de datos.gov.co cargar. Recibe la lista
  completa (con conteo) + cuáles están cargadas (`ICatalogRepository.GetLoadedCategoriesAsync`) y
  devuelve JSON con relevancia por categoría + una **consulta sugerida** a reintentar. Emite un evento
  SSE nuevo **`categories`** que el frontend muestra como **botones**: al clic se ingiere esa categoría,
  se reconstruye el índice y se **re-pregunta** la consulta.
- **Re-ranking por JSON en los agentes de recomendación**: el LLM devuelve `{respuesta, ...}` con la
  **relevancia recalculada** por cada candidato; solo se cita/recomienda lo que supera el umbral
  (`Search:RelevanceThreshold`, `Conversation:CategoryRelevanceThreshold`). Evita citar candidatos
  cercanos por embedding pero fuera de tema. Parseo defensivo (degrada si no hay JSON válido).
- **Reequilibrio a Foundry**: las reglas/rúbrica/esquema viven en las instrucciones de cada agente en
  Foundry (versionadas); el sistema envía solo los datos + un recordatorio compacto del JSON.
- Eventos SSE ahora: `agent` → `sources`/`categories` → `token`… → `conversation` → `done`.

## Alternativas consideradas

- **Agente único monolítico** — más simple al inicio, pero frágil, caro en tokens y difícil de
  evolucionar. Descartado.
- **Orquestación por reglas fijas (sin agentes)** — rígida; no escala a nuevas capacidades.
- **Enrutador solo por reglas** — insuficiente para intención en lenguaje natural con varios agentes;
  se conserva únicamente como reserva del enrutador LLM.
