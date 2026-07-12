# router-agent — Enrutador conversacional

| Ítem | Valor |
|---|---|
| **Versión** | 4 (`appsettings → Foundry:Chat:Agents:router-agent`) |
| **Modelo** | `gpt-4o-mini` (tarea ligera de clasificación → modelo de mínimo costo) |
| **Código** | `src/JYDE.OpenDataCopilot.Application/Conversation/LlmAgentRouter.cs` |
| **Decisión** | [ADR-0015 §Actualización 2026-07-11](../docs/adr/0015-arquitectura-multiagente.md) |

## Rol

Es la primera decisión de cada turno: recibe el mensaje del usuario y la lista de agentes
disponibles (nombre + descripción) y elige **cuál debe atenderlo**. No responde al ciudadano; su
única salida es la selección.

## Entrada (user prompt — sólo datos)

```
Respuesta anterior del Copilot: <contexto>        ← opcional, para desambiguar ("sí", "hazlo")

Mensaje del usuario: <pregunta>

Agentes disponibles:
- dataset-recommender-agent: Recomienda conjuntos de datos abiertos relevantes…
- dataset-analyst-agent: Explica las columnas/esquema de un dataset…
- category-recommender-agent: Recomienda qué categorías de datos.gov.co cargar…
- figures-agent: Consulta datos reales de un dataset (SoQL)…
```

## Salida esperada (JSON)

```json
{ "agente": "figures-agent" }
```

## Degradación (resiliencia)

Si el agente no está disponible, falla la red o no devuelve un nombre válido, el enrutamiento
**degrada a reglas** (`IConversationAgent.CanHandle` por palabras clave; en último término, el
primer agente registrado — el recomendador). En local con `Providers:Chat = Fake` se usa el
enrutador por reglas (`DefaultAgentRouter`).

## Interacciones

- Invocado por `CopilotOrchestrator` al inicio de cada turno.
- Su interacción cruda queda en la auditoría del turno (evento SSE `audit`).

## System prompt

> **Pendiente de insumo del equipo** — las instrucciones (regla de selección y esquema JSON)
> viven versionadas en el agente publicado en Azure AI Foundry.
