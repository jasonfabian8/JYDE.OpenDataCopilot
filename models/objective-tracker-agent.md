# objective-tracker-agent — Rastreador del objetivo (memoria)

| Ítem | Valor |
|---|---|
| **Versión** | 1 (`appsettings → Foundry:Chat:Agents:objective-tracker-agent`) |
| **Modelo** | `gpt-4o-mini` (resumen corto → modelo de mínimo costo) |
| **Código** | `src/JYDE.OpenDataCopilot.Application/Conversation/ObjectiveTracker.cs` |
| **Decisión** | [ADR-0015 §Memoria de conversación](../docs/adr/0015-arquitectura-multiagente.md) |

## Rol

Mantiene la **memoria** de la conversación: al final de cada turno recibe el objetivo acumulado y
el último mensaje del ciudadano, y devuelve una versión **actualizada y concisa** del objetivo
para no perder el hilo en conversaciones largas. El resultado se emite como evento SSE
`objective`, el frontend lo muestra en el panel de memoria y **el usuario puede editarlo**; viaja
de vuelta en el siguiente request (backend sin estado).

## Entrada (user prompt — sólo datos)

```
Objetivo actual: <objetivo o "(aún no hay objetivo)">

Último mensaje del ciudadano: <mensaje>
```

## Salida esperada (JSON)

```json
{ "objetivo": "Comparar el desempleo municipal con la cobertura educativa en 2024" }
```

## Degradación (resiliencia)

Si el modelo falla o no devuelve JSON válido, se **conserva el objetivo actual** — la memoria
nunca se corrompe por un error del LLM.

## Interacciones

- Invocado por `CopilotOrchestrator` después de que el agente especializado respondió.
- El objetivo se antepone al input de los demás agentes vía `ContextHeader` (junto con los
  datasets fijados).

## System prompt

> versionado en el agente publicado en Azure AI Foundry.
Eres el rastreador de objetivo de OpenData Copilot. Recibes "Objetivo actual" y "Último
mensaje del ciudadano". Devuelve una versión BREVE y actualizada del objetivo (1-2 frases),
integrando el mensaje nuevo sin perder lo previo. Si dice "(aún no hay objetivo)", extráelo
del mensaje. Responde ÚNICAMENTE con JSON: {"objetivo": "<objetivo actualizado, en español>"}
