# Demo automatizada — OpenData Copilot (ID 241)

Evidencia visual del funcionamiento del sistema para la presentación ante jurados del
concurso **Datos al Ecosistema 2026**. La demo se genera de forma **automatizada y
reproducible** con Playwright sobre el sistema real, con **IA real** (GPT-4.1-mini vía
Azure AI Foundry) y **narración en español** (voz neural colombiana).

## Entregables

| Archivo | Descripción |
|---|---|
| `OpenDataCopilot_Demo_ID241.mp4` | Video 1920×1080 (H.264 + AAC, ~2:20 min) narrado en español |
| `capturas/*.png` | 10 capturas Full HD de los momentos clave, listas para la presentación |
| `record-demo.js` | Script de Playwright que graba la demo automáticamente |
| `narrar.py` | Script que genera la narración TTS (es-CO) y la mezcla en el video |

## Qué demuestra la demo (escenario por escenario)

| # | Captura | Escenario | Objetivo |
|---|---------|-----------|----------|
| 1–2 | `01`, `02` | **Landing pública** | Identidad visual y propuesta de valor |
| 3 | `03` | **Pantalla inicial del Copilot** | UX de entrada: pregunta libre + sugerencias |
| 4 | `04` | **Pregunta del ciudadano** | Lenguaje natural, sin conocimientos técnicos |
| 5 | `05` | **Recomendación citada (RAG + GPT-4.1-mini)** | Fuente real con relevancia y enlace a datos.gov.co; el modelo responde con honestidad: si el dato solo se relaciona parcialmente, **lo declara** (guardrail anti-alucinación en acción) |
| 6 | `06` | **Cifras con datos en vivo** | El agente de cifras genera SoQL real y lo ejecuta sobre la API de datos.gov.co; la tabla llega al panel Artefactos |
| 7 | `07` | **Seguimiento conversacional** | El hilo mantiene el contexto (Valle del Cauca) |
| 8 | `08` | **Auditoría** | Cada interacción de cada agente queda registrada (gobierno de IA) |
| 9 | `09` | **Memoria** | Objetivo de la conversación rastreado por el agente de objetivo |
| 10 | `10` | **Vista final** | Flujo completo: usuario → agentes → RAG → respuesta citada |

## Cómo reproducirla

```bash
# 1. Credenciales: src/JYDE.OpenDataCopilot.Api/appsettings.Development.json
#    (ver .example; requiere Azure AI Foundry y MongoDB Atlas con el catálogo indexado)

# 2. API y frontend
dotnet run --project src/JYDE.OpenDataCopilot.Api        # puerto 5244
cd web && npm install && npm run dev                      # puerto 5191

# 3. Grabación (Node + Playwright)
npm i playwright && npx playwright install chromium
node demo/record-demo.js          # genera out/demo.webm + capturas + tiempos.json

# 4. Narración + MP4 final (Python)
pip install edge-tts imageio-ffmpeg
python demo/narrar.py             # genera out/OpenDataCopilot_Demo_ID241.mp4
```

> Nota: las respuestas del LLM son no deterministas; si se regraba, revisar
> `out/tiempos.json` y ajustar los segundos de inicio en `narrar.py` si alguna
> escena cambió de duración.
