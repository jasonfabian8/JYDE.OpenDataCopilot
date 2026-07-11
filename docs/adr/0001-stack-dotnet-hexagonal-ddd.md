# ADR 0001 — Stack .NET con arquitectura hexagonal + DDD y frontend React

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

Necesitamos elegir la plataforma base del backend para una solución que combina una API conversacional (baja latencia, respuestas en streaming), integración con servicios externos (API de Socrata e inferencia de modelos por HTTP) y un dominio que debe permanecer aislado de la infraestructura. Los criterios técnicos de la decisión son:

- **Tipado estático fuerte y verificación en compilación** (nullable, analizadores, estilo forzado en build), que reduce defectos y hace el código predecible y refactorizable con seguridad.
- **Soporte de primera clase para arquitectura hexagonal/DDD**: inyección de dependencias nativa, límites de proyecto explícitos y un ecosistema maduro de patrones empresariales.
- **Rendimiento y concurrencia**: ASP.NET Core ofrece alto throughput y baja latencia, con un modelo `async`/`await` maduro y `HttpClient`/resiliencia robustos para integrar APIs externas y SSE.
- **Testabilidad**: el aislamiento por puertos permite probar dominio y aplicación sin infraestructura; herramienta de pruebas y cobertura de primer nivel.
- **Interoperabilidad nativa con el resto de la solución en Azure** (AI Foundry, AI Search, Container Apps), evitando *impedance mismatch* entre plataformas.

El componente de IA (LLM y embeddings) se consume vía API REST, por lo que es **independiente del lenguaje** del backend: la elección se decide por los criterios técnicos anteriores, no por la disponibilidad de SDKs de un proveedor.

## Decisión

- **Backend en .NET** con **arquitectura hexagonal (puertos y adaptadores) + DDD**.
- **Frontend en React** (Vite) como SPA que consume la API REST.
- Capas: `Domain` (núcleo puro) ← `Application` (casos de uso + puertos) ← `Infrastructure` (adaptadores) ← `Api` (composition root).

## Consecuencias

- **Positivas:** límites de capa claros y verificados por el compilador/analizadores; alta testabilidad (dominio y aplicación aislados de la infraestructura); rendimiento y concurrencia adecuados para una API conversacional con streaming; integración nativa con Azure; código   predecible y consistente, también para asistentes de IA (Claude Code + GitHub Copilot).
- **Negativas / trade-offs:** parte del ecosistema de RAG/LLM tiene más recorrido en Python; se mitiga consumiendo Azure AI Foundry vía REST y con librerías .NET equivalentes. 
- **Seguimiento:** mantener la regla de dependencias hacia adentro (ver [SAD](../architecture/SAD.md)).

## Alternativas consideradas

- **Python con FastAPI** — alternativa legítima por la madurez de su ecosistema de IA. No se elige por razones técnicas: el tipado dinámico ofrece menos garantías en compilación para un dominio rico, y la concurrencia/throughput y la integración nativa con Azure son ventajas de .NET; el acceso a los modelos (vía REST) es equivalente desde ambas plataformas.
- **Frontend en Blazor (full .NET)** — daría un único lenguaje en todo el stack; se prefiere React por su madurez de UX y ecosistema de componentes, valioso.
