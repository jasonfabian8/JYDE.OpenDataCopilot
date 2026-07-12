# Reports — reporte final de OpenData Copilot (ID 241)

Módulo de reportes del proyecto para la entrega del concurso **Datos al Ecosistema 2026**.

```
reports/
├── figures/              # visualizaciones generadas automáticamente
├── generar_reporte.py    # generador: consume el sistema real y produce todo
├── reporte_final.pdf     # reporte ejecutivo + técnico (9 páginas)
└── README.md
```

## Cómo funciona

Sigue el mismo patrón que `demo/`: **no duplica lógica del backend**. El generador consume
el sistema real en ejecución a través de sus endpoints ya implementados:

| Fuente | Endpoint / origen | Se usa para |
|---|---|---|
| Catálogo (Socrata) | `GET /catalog/categories`, `GET /catalog/count` | Estadísticas de datos abiertos, figura de categorías |
| RAG + agentes | `POST /chat` (SSE: `agent`, `sources`, `audit`, `table`, `token`) | Evidencias, fuentes citadas con relevancia, distribución de agentes, latencias |
| Auditoría | evento `audit` del chat | Interacciones reales por agente (gobierno de IA) |
| Repositorio | conteo de `[Fact]`/`[Theory]`, `it()/test()`, ADRs, puertos, agentes | Métricas de ingeniería siempre al día |
| Demo | `demo/capturas/*.png` | Evidencia visual embebida en el PDF |

## Figuras generadas (`figures/`)

1. `01-catalogo-categorias.png` — datasets por categoría del portal (barras)
2. `02-relevancia-fuentes.png` — relevancia recalculada por el LLM de cada fuente citada (barras)
3. `03-distribucion-agentes.png` — interacciones por agente según la auditoría (dona)
4. `04-latencia-respuestas.png` — primer token vs. respuesta completa por pregunta (barras)
5. `05-metricas-ingenieria.png` — tests, ADRs, puertos y agentes extraídos del repo (barras)

## Contenido del PDF

1. Portada · 2. Resumen ejecutivo y hallazgos · 3. Uso de datos abiertos ·
4. IA multiagente + RAG · 5. Resultados, métricas y evidencias (preguntas reales con
respuestas, fuentes y datos SoQL en vivo) · 6. Evidencia visual del prototipo ·
7. Conclusiones y recomendaciones.

## Regenerar

```bash
# 1. API corriendo con credenciales (appsettings.Development.json)
dotnet run --project src/JYDE.OpenDataCopilot.Api

# 2. Dependencias de la herramienta (solo tooling local; no tocan el backend)
pip install matplotlib reportlab

# 3. Generar figuras + PDF
python reports/generar_reporte.py
```

> Las respuestas del LLM son no deterministas: cada ejecución produce evidencias
> ligeramente distintas (misma estructura, datos frescos). Las métricas de ingeniería se
> recalculan del código en cada corrida, por lo que el reporte nunca queda desactualizado.
