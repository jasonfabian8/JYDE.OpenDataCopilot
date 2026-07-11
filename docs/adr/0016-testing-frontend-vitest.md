# ADR 0016 — Testing del frontend con Vitest + React Testing Library

- **Estado:** Aceptado
- **Fecha:** 2026-07-10
- **Decisores:** Equipo OpenData Copilot

## Contexto

El frontend (React + Vite, [ADR-0008](0008-stack-frontend-vite-zustand.md)) no tenía pruebas
automatizadas. SonarCloud analiza todo el repositorio y contabiliza las líneas de TS/TSX sin
cobertura como no cubiertas, lo que hace fallar el Quality Gate (cobertura de *new code* por debajo
del umbral) aunque el backend .NET esté por encima del 95%. La cobertura ≥95% es política del
proyecto (ver `/CLAUDE.md`); debe aplicar también al frontend, no solo a .NET.

Necesitamos un framework de pruebas para el frontend que:

- Integre con la configuración de **Vite** existente (mismo pipeline de transform/resolución).
- Permita probar la **lógica** (stores de Zustand, cliente HTTP/SSE) y los **componentes** (render).
- Emita cobertura en un formato que **SonarCloud** consuma (LCOV vía `sonar.javascript.lcov.reportPaths`).

## Decisión

Adoptar **Vitest** como test runner del frontend, con **React Testing Library** para pruebas de
componentes:

- `vitest` — runner (reusa la config de Vite; API compatible con Jest).
- `@vitest/coverage-v8` — cobertura (salida LCOV para SonarCloud, además de texto/HTML local).
- `@testing-library/react` + `@testing-library/jest-dom` — render y aserciones de componentes.
- `@testing-library/user-event` — interacción de usuario en pruebas.
- `jsdom` — entorno DOM para las pruebas de componentes.

Convenciones:

- Tests junto al código como `*.test.ts`/`*.test.tsx` (colocación por *feature*, igual que el resto).
- La cobertura se recolecta con `npm run test:coverage` (LCOV en `coverage/lcov.info`).
- CI ejecuta las pruebas del frontend y pasa el LCOV al escáner de Sonar.

## Consecuencias

- **Positivas:** cobertura real del frontend en el Quality Gate; regresión detectable en stores y
  cliente SSE (donde vive la lógica); mismo pipeline que Vite (sin config de transform aparte).
- **Negativas / trade-offs:** dependencias de desarrollo nuevas y un entorno `jsdom` que no es un
  navegador real (los detalles de renderizado fino se validan manualmente / e2e si hiciera falta).

## Alternativas consideradas

- **Jest** — descartado: requiere configurar transform/ESM aparte del pipeline de Vite; Vitest es la
  opción nativa para proyectos Vite.
- **Excluir el frontend de la cobertura** (`sonar.coverage.exclusions`) — descartado: apaga la señal
  en vez de cubrir el código; contradice la política de cobertura del proyecto.
