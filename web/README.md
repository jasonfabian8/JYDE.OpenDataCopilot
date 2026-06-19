# OpenData Copilot — Web

Landing pública de **OpenData Copilot**: un copiloto conversacional sobre los datos
abiertos de Colombia (`datos.gov.co`). Comunica el objetivo del producto —preguntar en
lenguaje natural y recibir respuestas **citando la fuente**— con un diseño editorial
orientado al uso técnico.

## Stack

- **Vite + React + TypeScript** (`strict`) — ver [ADR-0008](../docs/adr/0008-stack-frontend-vite-zustand.md).
- **Zustand** — estado de la consola de demostración.
- **Tailwind CSS** (v4, con design tokens) — ver [ADR-0009](../docs/adr/0009-estilos-tailwind.md).

> Gobierno de librerías: cualquier dependencia nueva se concierta con el equipo y se
> registra en SAD + ADR antes de adoptarse.

## Comandos

```bash
npm install      # instalar dependencias
npm run dev      # servidor de desarrollo (http://localhost:5191)
npm run build    # build de producción (dist/)
npm run preview  # previsualizar el build
```

## Estructura

```
src/
├── app/                      # composición de la página (App)
├── features/landing/
│   ├── components/           # secciones de la landing
│   ├── model/                # tipos y datos de ejemplo
│   └── state/                # store de Zustand (consola demo)
├── shared/ui/                # componentes reutilizables (design system)
└── styles/                   # Tailwind + design tokens
```

> Las cifras de los intercambios de ejemplo son **ilustrativas**. En producción las
> respuestas provienen de la API de Socrata en vivo, siempre con su fuente citada.
