# ADR 0009 — Estilos/UI del frontend: Tailwind CSS

- **Estado:** Aceptado
- **Fecha:** 2026-06-19
- **Decisores:** Equipo OpenData Copilot

## Contexto

[ADR-0008](0008-stack-frontend-vite-zustand.md) fijó la base del frontend (Vite + Zustand) y dejó **estilos/UI como pendiente por concertar**. El primer entregable visible es una **landing pública** que debe transmitir el objetivo de la solución —un copiloto conversacional sobre los datos abiertos de Colombia que responde **citando la fuente**— con un diseño de **alto impacto, editorial y orientado al uso técnico**.

Necesitamos una estrategia de estilos que permita iterar rápido sobre un diseño novedoso, mantener consistencia (design tokens), no acoplar lógica a estilos y no añadir peso en runtime. Restricciones del proyecto: **costo y plazo duros** (≈ 3 semanas), equipo pequeño.

Criterios:

- **Velocidad de iteración** sobre el diseño sin salir del marcado.
- **Consistencia** mediante tokens (color, tipografía, espaciado) centralizados.
- **Sin CSS en runtime** (no CSS-in-JS) para no penalizar rendimiento.
- **Tree-shaking** del CSS no usado; bundle pequeño.
- Integración nativa con **Vite** y TypeScript.

## Decisión

1. **Tailwind CSS** como sistema de estilos del frontend, integrado con Vite mediante su plugin oficial (`@tailwindcss/vite`).
2. Los **design tokens** (paleta, tipografía, escalas) se definen de forma centralizada en la capa de estilos y se consumen vía utilidades; los componentes reutilizables del design system viven en `shared/ui`.
3. No se introduce ninguna librería de componentes UI por ahora (se construye el design system propio sobre Tailwind). Cualquier adición futura (p. ej. Radix, Headless UI) se concierta y registra en un ADR aparte, conforme al gobierno de [ADR-0008](0008-stack-frontend-vite-zustand.md).

## Consecuencias

- **Positivas:** iteración rápida de un diseño novedoso sin cambiar de contexto a archivos CSS; tokens consistentes; CSS final mínimo (purga del no usado) y cero runtime; integración directa con Vite; bien tipado vía el plugin de editor.
- **Negativas / trade-offs:** marcado con muchas clases utilitarias (mitigado extrayendo componentes en `shared/ui` y patrones reutilizables); curva inicial de tokens/convenciones. No resuelve animaciones complejas ni gráficos (se concertarán aparte si hacen falta).
- **Seguimiento:** si la complejidad de UI crece, evaluar una librería de primitivas accesibles (Radix/Headless UI) en un ADR sucesivo. Linter/formatter y pruebas del frontend siguen pendientes (ver SAD §11).

## Alternativas consideradas

- **CSS Modules (sin dependencia nueva)** — cero dependencias y scope por componente, pero más verboso y más lento para iterar un diseño de alto impacto; los tokens y la consistencia quedan a cargo del equipo sin utilidades. Descartado frente a la velocidad de Tailwind.
- **CSS-in-JS (styled-components / Emotion)** — buena ergonomía pero añade **runtime** y coste de rendimiento, contrario a la restricción de costo/eficiencia. Descartado.
- **Librería de componentes completa (MUI, Chakra)** — acelera CRUDs pero impone un lenguaje visual difícil de doblar hacia un diseño "muy novedoso", y añade peso. Descartado para una landing de identidad propia.
