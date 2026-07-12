# -*- coding: utf-8 -*-
"""Genera narración TTS (voz es-CO) sincronizada por escena y la mezcla en el video."""
import asyncio
import os
import subprocess

import edge_tts
import imageio_ffmpeg

AQUI = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(AQUI, "out")
VOZ = "es-CO-SalomeNeural"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

# (segundo de inicio, texto) — inicios derivados de out/tiempos.json
SEGMENTOS = [
    (1.5, "Colombia publica más de ocho mil conjuntos de datos abiertos, pero para la mayoría "
          "de las personas siguen siendo difíciles de usar. OpenData Copilot los convierte en respuestas."),
    (13.2, "La plataforma es simple: preguntas en lenguaje natural, y el copiloto encuentra el "
           "dato oficial y responde citando su fuente."),
    (22.3, "Este es el copiloto. Sin portales, sin archivos técnicos: solo una conversación."),
    (29.5, "Preguntamos por accidentalidad vial, como lo haría cualquier ciudadano."),
    (38.0, "Seis agentes de inteligencia artificial colaboran: el sistema recupera los datasets "
           "del índice semántico y G P T cuatro punto uno mini responde citando la fuente oficial. "
           "Y si los datos no alcanzan, lo declara: nunca inventa cifras."),
    (54.0, "También calcula cifras reales: el agente de cifras escribe una consulta SoQL y la "
           "ejecuta en vivo sobre la A P I de datos abiertos. El resultado llega como una tabla verificable."),
    (73.0, "La conversación mantiene el contexto: pedimos recomendaciones para el Valle del Cauca "
           "y el copiloto sigue el hilo."),
    (81.0, "Transparencia total: cada interacción de cada agente queda registrada en el panel de auditoría."),
    (87.4, "Y la memoria conserva el objetivo de la conversación en todo momento."),
    (93.5, "OpenData Copilot: pregunta simple, respuesta con evidencia. Proyecto doscientos "
           "cuarenta y uno, Datos al Ecosistema dos mil veintiséis."),
]


async def generar():
    for i, (_, texto) in enumerate(SEGMENTOS):
        destino = os.path.join(OUT, f"narr{i:02d}.mp3")
        if os.path.exists(destino) and os.path.getsize(destino) > 1000:
            continue
        await edge_tts.Communicate(texto, VOZ, rate="+8%").save(destino)
        print("TTS", i, "ok")


def duracion(ruta):
    r = subprocess.run([FFMPEG, "-i", ruta], capture_output=True, text=True)
    for linea in r.stderr.splitlines():
        if "Duration" in linea:
            h, m, s = linea.split("Duration:")[1].split(",")[0].strip().split(":")
            return int(h) * 3600 + int(m) * 60 + float(s)
    return 0.0


def mezclar():
    entradas = [os.path.join(OUT, f"narr{i:02d}.mp3") for i in range(len(SEGMENTOS))]
    for (inicio, _), ruta in zip(SEGMENTOS, entradas):
        d = duracion(ruta)
        print(f"  {os.path.basename(ruta)}: inicia {inicio}s, dura {d:.1f}s, termina {inicio + d:.1f}s")

    cmd = [FFMPEG, "-y", "-i", os.path.join(OUT, "demo.webm")]
    for ruta in entradas:
        cmd += ["-i", ruta]
    filtros = []
    for i, (inicio, _) in enumerate(SEGMENTOS):
        ms = int(inicio * 1000)
        filtros.append(f"[{i + 1}:a]adelay={ms}|{ms}[a{i}]")
    mezcla = "".join(f"[a{i}]" for i in range(len(SEGMENTOS)))
    filtros.append(f"{mezcla}amix=inputs={len(SEGMENTOS)}:normalize=0[mezcla]")
    filtros.append("[mezcla]apad[aout]")
    # congelar el último frame para que la narración final no se corte
    filtros.append("[0:v]tpad=stop_mode=clone:stop_duration=6[vout]")
    # -t explícito: con apad (audio infinito) + tpad, -shortest puede no terminar
    total = duracion(os.path.join(OUT, "demo.webm")) + 6
    cmd += [
        "-filter_complex", ";".join(filtros),
        "-map", "[vout]", "-map", "[aout]", "-t", f"{total:.0f}",
        "-c:v", "libx264", "-preset", "fast", "-crf", "18", "-pix_fmt", "yuv420p",
        "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart", "-shortest",
        os.path.join(OUT, "OpenDataCopilot_Demo_ID241.mp4"),
    ]
    subprocess.run(cmd, check=True, capture_output=True)
    print("MP4 narrado OK")


asyncio.run(generar())
mezclar()
