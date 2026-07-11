# ADR 0008 — Frontend: base con Vite y Zustand (y gobierno de librerías)

- **Estado:** Aceptado
- **Fecha:** 2026-06-19
- **Decisores:** Equipo OpenData Copilot

## Contexto

El frontend es una SPA en **React + TypeScript** (ver [ADR-0001](0001-stack-dotnet-hexagonal-ddd.md)) que consume la API REST y un flujo **SSE** para el chat en streaming. Antes de construir funcionalidad necesitamos fijar dos piezas base —herramienta de build/dev y manejo de estado— con criterios técnicos, y establecer **cómo se incorporan las demás librerías** para evitar dependencias ad-hoc y mantener la coherencia del diseño.

Criterios técnicos:

- **Tooling de build/dev:** arranque y *hot reload* rápidos (servidor dev sobre ESM nativo), build optimizado, soporte TypeScript de primera clase y configuración simple. No requerimos SSR.
- **Manejo de estado:** una solución **ligera, tipada y desacoplada** del árbol de componentes, con poco *boilerplate*, fácil de testear y que no acople la lógica de estado a React.

## Decisión

1. **Vite** como herramienta de build y servidor de desarrollo del frontend.
2. **Zustand** como librería de manejo de estado de la aplicación.
3. **Gobierno de librerías (regla permanente):** cualquier otra dependencia del frontend (o del backend) debe **concertarse con el equipo** y quedar **registrada actualizando el [SAD](../architecture/SAD.md) y un ADR** *antes* de adoptarse. No se introducen librerías de facto sin ese acuerdo.

**Pendientes por concertar** (se decidirán y registrarán cuando se aborden): estilos/UI, obtención y cache de datos del servidor, gráficos/visualización, framework de pruebas e i18n.

## Consecuencias

- **Positivas:** base mínima y moderna lista para construir; estado tipado y testeable con bajo acoplamiento; proceso claro que evita proliferación de dependencias y mantiene SAD/ADR como fuente de verdad.
- **Negativas / trade-offs:** Zustand cubre estado de cliente; la estrategia de estado de servidor (cache/reintentos) queda pendiente de concertar. El gobierno añade un paso antes de adoptar una librería, a cambio de consistencia.
- **Seguimiento:** abordar los pendientes (estilos, data-fetching, gráficos, pruebas, i18n) en ADRs sucesivos a medida que se desarrollen las features.

## Actualización 2026-07-10 — Estructura multi-página (landing + Copilot)

Para **independizar la experiencia de trabajo (el chat) de la comunicación (la landing)** se
adopta una estructura **multi-página de Vite con dos puntos de entrada**, sin router ni
dependencias nuevas (el gobierno de librerías se respeta):

- `index.html` → **landing informativa** (tema claro), enlaza con «Abrir Copilot».
- `copilot/index.html` → servida en **`/copilot/`**: la **app del Copilot conversacional**
  (tema oscuro, layout estilo ChatGPT/Claude: barra lateral + área central).

Ambas páginas comparten el *design system* (`styles/index.css`, con tokens oscuros `night`/`sky`)
y el cliente de la API (`shared/api`). El estado del chat vive en `features/copilot`
(varias conversaciones **en memoria de la sesión**; el refresco reinicia el hilo, coherente con
el backend sin estado — ver [ADR-0015](0015-arquitectura-multiagente.md)). El antiguo `ChatPanel`
embebido en la landing se retiró.

## Alternativas consideradas

- **Build:** Create React App (en desuso, lento) y Next.js (orientado a SSR/full-stack, innecesario para una SPA que ya tiene backend .NET) — descartadas frente a la simplicidad y velocidad de Vite.
- **Estado:** Redux Toolkit (más *boilerplate* y ceremonia del que necesitamos) y sólo Context API (no escala bien para estado compartido y provoca re-renders) — descartadas frente a Zustand.
