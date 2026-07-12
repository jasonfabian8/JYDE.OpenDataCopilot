# -*- coding: utf-8 -*-
"""
Generador del reporte final de OpenData Copilot (ID 241 — Datos al Ecosistema 2026).

Sigue el mismo patrón que ``demo/``: es una herramienta que consume el SISTEMA REAL
en ejecución (API en http://localhost:5244) para no duplicar lógica: el catálogo, la
búsqueda semántica (RAG), los agentes de conversación y la auditoría se consultan a
través de sus endpoints ya implementados (CatalogController, SearchController,
ChatController). Además extrae métricas de ingeniería directamente del repositorio
(tests, ADRs, agentes) para que el reporte nunca quede desactualizado a mano.

Salidas:
    reports/figures/*.png      — todas las visualizaciones del reporte
    reports/reporte_final.pdf  — reporte ejecutivo + técnico listo para la entrega

Uso:
    dotnet run --project src/JYDE.OpenDataCopilot.Api   # API corriendo
    pip install matplotlib reportlab
    python reports/generar_reporte.py
"""
from __future__ import annotations

import glob
import json
import os
import re
import time
import urllib.request
from datetime import date

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import cm
from reportlab.platypus import (
    HRFlowable,
    Image,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

# ---------------------------------------------------------------- configuración
API = "http://localhost:5244"
AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(AQUI)
FIGURES = os.path.join(AQUI, "figures")
PDF = os.path.join(AQUI, "reporte_final.pdf")
CAPTURAS_DEMO = os.path.join(RAIZ, "demo", "capturas")

# Paleta (idéntica al pitch deck para identidad visual consistente)
INK, GRAY = "#0F172A", "#64748B"
BLUE, GREEN, AMBER = "#2563EB", "#059669", "#D97706"
LIGHT = "#F1F5F9"

PREGUNTAS_EVIDENCIA = [
    "¿Qué datasets hay sobre accidentalidad vial?",
    "¿Qué datasets me recomiendas sobre salud en los municipios de Colombia?",
    "¿Cuántos registros tiene el dataset de mantenimiento vial de Palmira?",
]

plt.rcParams.update({
    "font.family": "Segoe UI",
    "axes.edgecolor": "#E2E8F0",
    "axes.labelcolor": INK,
    "text.color": INK,
    "xtick.color": GRAY,
    "ytick.color": GRAY,
    "axes.spines.top": False,
    "axes.spines.right": False,
    "figure.facecolor": "white",
})


# ---------------------------------------------------------------- API helpers
def api_get(ruta: str, timeout: int = 120):
    with urllib.request.urlopen(API + ruta, timeout=timeout) as respuesta:
        return json.loads(respuesta.read().decode("utf-8"))


def chat(pregunta: str, timeout: int = 240) -> dict:
    """Envía una pregunta al Copilot y recopila los eventos SSE con sus tiempos."""
    cuerpo = json.dumps({"question": pregunta}).encode("utf-8")
    peticion = urllib.request.Request(
        API + "/chat", data=cuerpo,
        headers={"Content-Type": "application/json; charset=utf-8"})
    inicio = time.monotonic()
    resultado = {
        "pregunta": pregunta, "agente": None, "fuentes": [], "auditoria": [],
        "tabla": None, "respuesta": "", "t_primer_token": None, "t_total": None,
    }
    with urllib.request.urlopen(peticion, timeout=timeout) as respuesta:
        evento = None
        for linea in respuesta:
            linea = linea.decode("utf-8").strip()
            if linea.startswith("event: "):
                evento = linea[7:]
            elif linea.startswith("data: ") and evento:
                dato = json.loads(linea[6:])
                if evento == "agent":
                    resultado["agente"] = dato["agent"]
                elif evento == "sources":
                    resultado["fuentes"] = dato["sources"]
                elif evento == "audit":
                    resultado["auditoria"] = dato["interactions"]
                elif evento == "table":
                    resultado["tabla"] = dato["table"]
                elif evento == "token":
                    if resultado["t_primer_token"] is None:
                        resultado["t_primer_token"] = time.monotonic() - inicio
                    resultado["respuesta"] += dato["text"]
                elif evento == "done":
                    resultado["t_total"] = time.monotonic() - inicio
    return resultado


# ---------------------------------------------------------------- métricas repo
def metricas_repositorio() -> dict:
    """Extrae métricas de ingeniería del propio repositorio (siempre al día)."""
    tests_net = 0
    for archivo in glob.glob(os.path.join(RAIZ, "tests", "**", "*.cs"), recursive=True):
        if os.sep + "bin" + os.sep in archivo or os.sep + "obj" + os.sep in archivo:
            continue
        contenido = open(archivo, encoding="utf-8", errors="ignore").read()
        tests_net += len(re.findall(r"\[(?:Fact|Theory)\]", contenido))

    tests_web = 0
    for archivo in glob.glob(os.path.join(RAIZ, "web", "src", "**", "*.test.*"), recursive=True):
        contenido = open(archivo, encoding="utf-8", errors="ignore").read()
        tests_web += len(re.findall(r"\b(?:it|test)\(", contenido))

    adrs = len([a for a in glob.glob(os.path.join(RAIZ, "docs", "adr", "0*.md"))])
    conversacion = os.path.join(RAIZ, "src", "JYDE.OpenDataCopilot.Application", "Conversation")
    # agentes concretos (excluye la interfaz I*) + rastreador de objetivo + enrutador
    concretos = [a for a in glob.glob(os.path.join(conversacion, "*Agent.cs"))
                 if not os.path.basename(a).startswith("I")]
    agentes = (len(concretos)
               + (1 if os.path.exists(os.path.join(conversacion, "ObjectiveTracker.cs")) else 0)
               + (1 if glob.glob(os.path.join(conversacion, "*AgentRouter.cs")) else 0))
    puertos = len([
        archivo for archivo in glob.glob(os.path.join(
            RAIZ, "src", "JYDE.OpenDataCopilot.Application", "**", "I*.cs"), recursive=True)
        if "interface " in open(archivo, encoding="utf-8", errors="ignore").read()
    ])
    return {
        "tests_net": tests_net, "tests_web": tests_web,
        "tests_total": tests_net + tests_web,
        "adrs": adrs, "agentes": agentes, "puertos": puertos,
    }


# ---------------------------------------------------------------- figuras
def guardar(fig, nombre: str) -> str:
    ruta = os.path.join(FIGURES, nombre)
    fig.savefig(ruta, dpi=160, bbox_inches="tight")
    plt.close(fig)
    print("figura:", nombre)
    return ruta


def fig_categorias(categorias: list) -> str:
    top = sorted(categorias, key=lambda c: -c["count"])[:12]
    nombres = [c["name"] for c in top][::-1]
    conteos = [c["count"] for c in top][::-1]
    fig, ax = plt.subplots(figsize=(8, 4.6))
    barras = ax.barh(nombres, conteos, color=BLUE)
    barras[-1].set_color(GREEN)
    ax.bar_label(barras, fmt="{:,.0f}", padding=4, fontsize=8, color=GRAY)
    ax.set_title("Catálogo de datos.gov.co: datasets por categoría (top 12)",
                 fontsize=11, fontweight="bold", loc="left")
    ax.set_xlabel("Datasets publicados")
    ax.tick_params(labelsize=8.5)
    return guardar(fig, "01-catalogo-categorias.png")


def fig_relevancia(turnos: list) -> str:
    etiquetas, valores, colores_barras = [], [], []
    paleta = [BLUE, GREEN, AMBER]
    for i, turno in enumerate(turnos):
        for fuente in turno["fuentes"][:3]:
            nombre = fuente["name"]
            etiquetas.append(nombre[:44] + "…" if len(nombre) > 45 else nombre)
            valores.append(fuente["score"] * 100)
            colores_barras.append(paleta[i % 3])
    fig, ax = plt.subplots(figsize=(8, 0.55 * max(len(etiquetas), 4) + 1.2))
    barras = ax.barh(etiquetas[::-1], valores[::-1], color=colores_barras[::-1])
    ax.bar_label(barras, fmt="%.0f%%", padding=4, fontsize=8, color=GRAY)
    ax.set_xlim(0, 115)
    ax.set_title("Relevancia recalculada por el LLM de cada fuente citada",
                 fontsize=11, fontweight="bold", loc="left")
    ax.set_xlabel("Relevancia (%) — sólo se cita lo que supera el umbral (50%)")
    ax.tick_params(labelsize=8)
    return guardar(fig, "02-relevancia-fuentes.png")


def fig_agentes(turnos: list, n_agentes_repo: int) -> str:
    conteo: dict[str, int] = {}
    for turno in turnos:
        for interaccion in turno["auditoria"]:
            conteo[interaccion["agent"]] = conteo.get(interaccion["agent"], 0) + 1
    fig, ax = plt.subplots(figsize=(6.4, 4.2))
    etiquetas = list(conteo.keys())
    ax.pie(conteo.values(), labels=etiquetas, autopct="%1.0f%%",
           colors=[BLUE, GREEN, AMBER, "#7C3AED", "#DB2777", "#0891B2"][:len(etiquetas)],
           textprops={"fontsize": 9}, wedgeprops={"width": 0.42})
    ax.set_title(
        f"Interacciones por agente durante la evidencia\n"
        f"(arquitectura multiagente: {n_agentes_repo} agentes + enrutador)",
        fontsize=11, fontweight="bold")
    return guardar(fig, "03-distribucion-agentes.png")


def fig_latencia(turnos: list) -> str:
    etiquetas = [f"P{i + 1}" for i in range(len(turnos))]
    primer = [t["t_primer_token"] or 0 for t in turnos]
    total = [t["t_total"] or 0 for t in turnos]
    x = range(len(turnos))
    fig, ax = plt.subplots(figsize=(7.2, 3.6))
    ax.bar([i - 0.18 for i in x], primer, width=0.36, color=BLUE,
           label="Primer token (streaming SSE)")
    ax.bar([i + 0.18 for i in x], total, width=0.36, color=LIGHT,
           edgecolor=GRAY, label="Respuesta completa")
    for i in x:
        ax.text(i - 0.18, primer[i] + 0.12, f"{primer[i]:.1f}s", ha="center",
                fontsize=8, color=BLUE)
        ax.text(i + 0.18, total[i] + 0.12, f"{total[i]:.1f}s", ha="center",
                fontsize=8, color=GRAY)
    ax.set_xticks(list(x), etiquetas)
    ax.set_ylabel("Segundos")
    ax.set_title("Desempeño del Copilot por pregunta de evidencia",
                 fontsize=11, fontweight="bold", loc="left")
    ax.legend(fontsize=8, frameon=False)
    return guardar(fig, "04-latencia-respuestas.png")


def fig_ingenieria(metricas: dict) -> str:
    etiquetas = ["Tests .NET", "Tests web", "ADRs", "Puertos\n(interfaces)", "Agentes IA"]
    valores = [metricas["tests_net"], metricas["tests_web"], metricas["adrs"],
               metricas["puertos"], metricas["agentes"]]
    fig, ax = plt.subplots(figsize=(7.2, 3.4))
    barras = ax.bar(etiquetas, valores, color=[BLUE, BLUE, AMBER, GREEN, "#7C3AED"])
    ax.bar_label(barras, fontsize=10, fontweight="bold", color=INK)
    ax.set_title("Métricas de ingeniería (extraídas del repositorio al generar el reporte)",
                 fontsize=11, fontweight="bold", loc="left")
    ax.tick_params(labelsize=9)
    return guardar(fig, "05-metricas-ingenieria.png")


# ---------------------------------------------------------------- PDF
def estilos() -> dict:
    return {
        "titulo": ParagraphStyle("titulo", fontName="Helvetica-Bold", fontSize=26,
                                 textColor=colors.HexColor(INK), leading=32),
        "subtitulo": ParagraphStyle("subtitulo", fontName="Helvetica", fontSize=13,
                                    textColor=colors.HexColor(BLUE), leading=18),
        "h1": ParagraphStyle("h1", fontName="Helvetica-Bold", fontSize=15,
                             textColor=colors.HexColor(INK), spaceBefore=14,
                             spaceAfter=6, leading=19),
        "h2": ParagraphStyle("h2", fontName="Helvetica-Bold", fontSize=11.5,
                             textColor=colors.HexColor(BLUE), spaceBefore=8,
                             spaceAfter=4),
        "cuerpo": ParagraphStyle("cuerpo", fontName="Helvetica", fontSize=9.8,
                                 textColor=colors.HexColor("#334155"), leading=14.5,
                                 spaceAfter=5),
        "pie": ParagraphStyle("pie", fontName="Helvetica-Oblique", fontSize=8,
                              textColor=colors.HexColor(GRAY)),
    }


def tabla_kv(pares: list, ancho_clave=4.6) -> Table:
    celda = ParagraphStyle("celda", fontName="Helvetica", fontSize=9.3,
                           textColor=colors.HexColor("#334155"), leading=12.5)
    filas = [[clave, Paragraph(str(valor), celda)] for clave, valor in pares]
    tabla = Table(filas, colWidths=[ancho_clave * cm, (16.5 - ancho_clave) * cm])
    tabla.setStyle(TableStyle([
        ("FONTNAME", (0, 0), (0, -1), "Helvetica-Bold"),
        ("FONTNAME", (1, 0), (1, -1), "Helvetica"),
        ("FONTSIZE", (0, 0), (-1, -1), 9.3),
        ("TEXTCOLOR", (0, 0), (0, -1), colors.HexColor(INK)),
        ("TEXTCOLOR", (1, 0), (1, -1), colors.HexColor("#334155")),
        ("ROWBACKGROUNDS", (0, 0), (-1, -1), [colors.white, colors.HexColor(LIGHT)]),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
    ]))
    return tabla


def construir_pdf(datos: dict) -> None:
    e = estilos()
    m = datos["metricas"]
    doc = SimpleDocTemplate(PDF, pagesize=letter, topMargin=2 * cm,
                            bottomMargin=2 * cm, leftMargin=2.2 * cm,
                            rightMargin=2.2 * cm, title="OpenData Copilot — Reporte final",
                            author="Equipo JYDE")
    F = []  # flowables

    # ---- portada
    F.append(Spacer(1, 3.2 * cm))
    F.append(Paragraph("OpenData Copilot", e["titulo"]))
    F.append(Paragraph("Reporte final — Pregúntale a los datos abiertos de Colombia",
                       e["subtitulo"]))
    F.append(Spacer(1, 0.5 * cm))
    F.append(HRFlowable(width="100%", thickness=2, color=colors.HexColor(BLUE)))
    F.append(Spacer(1, 0.6 * cm))
    F.append(tabla_kv([
        ["Concurso", "Datos al Ecosistema 2026 — MinTIC"],
        ["Proyecto", "ID 241 · Nivel Avanzado"],
        ["Equipo", "Yehison Fabian Becerra · Edwin Giovanni Villamizar Aldana · "
                   "Yenny Alarcon · Helen Daniela Benitez Hipolito"],
        ["Repositorio", "github.com/jasonfabian8/JYDE.OpenDataCopilot (MIT)"],
        ["Fecha de generación", date.today().strftime("%d/%m/%Y")],
        ["Generado por", "reports/generar_reporte.py sobre el sistema en ejecución"],
    ]))
    F.append(PageBreak())

    # ---- resumen ejecutivo
    F.append(Paragraph("1. Resumen ejecutivo", e["h1"]))
    F.append(Paragraph(
        "Colombia publica más de 8.000 conjuntos de datos abiertos en datos.gov.co, pero "
        "siguen siendo inaccesibles para la mayoría de los ciudadanos: encontrarlos y "
        "usarlos exige conocer portales, formatos técnicos y lenguajes de consulta. "
        "<b>OpenData Copilot</b> elimina esa barrera: el ciudadano pregunta en lenguaje "
        "natural y un sistema multiagente de IA descubre los datasets relevantes, consulta "
        "los datos en vivo y responde <b>citando siempre la fuente oficial</b>.", e["cuerpo"]))
    F.append(Paragraph(
        f"El prototipo está operativo de extremo a extremo: catálogo real ingerido vía la "
        f"API de Socrata, búsqueda semántica (RAG) sobre MongoDB Atlas Vector Search, "
        f"{m['agentes']} agentes especializados orquestados con GPT-4.1-mini (Azure AI "
        f"Foundry), streaming SSE, auditoría integral y {m['tests_total']} pruebas "
        f"automatizadas con una cobertura exigida ≥95% por proyecto.", e["cuerpo"]))
    F.append(Paragraph("Hallazgos principales", e["h2"]))
    for hallazgo in [
        f"El catálogo público contiene <b>{datos['total_portal']:,} datasets</b> en "
        f"{len(datos['categorias'])} categorías; la ingesta de una categoría completa toma segundos.",
        "La búsqueda híbrida (embeddings + keyword) recupera candidatos pertinentes y el LLM "
        "recalcula su relevancia: sólo se citan las fuentes que superan el umbral (50%).",
        "El guardrail anti-alucinación funciona en la práctica: cuando los datos sólo se "
        "relacionan parcialmente con la pregunta, el sistema lo declara en la respuesta.",
        "El agente de cifras genera SoQL y lo ejecuta en vivo sobre la API de datos.gov.co, "
        "devolviendo tablas verificables en lugar de números inventados.",
        "Toda interacción de todo agente queda registrada en la auditoría (gobierno de IA).",
    ]:
        F.append(Paragraph("• " + hallazgo, e["cuerpo"]))
    F.append(PageBreak())

    # ---- uso de datos abiertos
    F.append(Paragraph("2. Uso de datos abiertos", e["h1"]))
    F.append(Paragraph(
        "La única fuente de datos es la API oficial de Socrata de datos.gov.co (catálogo + "
        "SoQL), sin web scraping (ADR-0002). El estado del catálogo al generar este reporte:",
        e["cuerpo"]))
    F.append(Image(datos["fig_categorias"], width=15.5 * cm, height=8.9 * cm))
    F.append(Spacer(1, 0.3 * cm))
    F.append(tabla_kv([
        ["Datasets en el portal", f"{datos['total_portal']:,}"],
        ["Categorías temáticas", str(len(datos["categorias"]))],
        ["Datasets en el índice local", f"{datos['ingeridos']:,}"],
        ["Estrategia", "Híbrida (ADR-0005): metadatos amplios + consulta de datos en vivo"],
    ]))
    F.append(PageBreak())

    # ---- IA
    F.append(Paragraph("3. Inteligencia artificial: sistema multiagente con RAG", e["h1"]))
    F.append(Paragraph(
        f"La conversación la resuelve un equipo de {m['agentes']} agentes especializados "
        "(enrutador, objetivo, categorías, recomendador, analista y cifras) coordinados por "
        "el Copilot Orquestador (ADR-0015). El RAG recupera del índice vectorial los "
        "datasets candidatos y el LLM razona únicamente sobre ese contexto: sin fuente no "
        "hay respuesta.", e["cuerpo"]))
    F.append(Image(datos["fig_agentes"], width=12.5 * cm, height=8.2 * cm))
    F.append(Paragraph(
        "Distribución real de interacciones registradas por la auditoría durante las "
        "preguntas de evidencia de este reporte.", e["pie"]))
    F.append(Spacer(1, 0.35 * cm))
    F.append(Image(datos["fig_relevancia"], width=15.5 * cm,
                   height=15.5 * cm * datos["rel_alto"] / datos["rel_ancho"]))
    F.append(PageBreak())

    # ---- resultados y evidencias
    F.append(Paragraph("4. Resultados, métricas y evidencias", e["h1"]))
    F.append(Paragraph("Preguntas de evidencia ejecutadas contra el sistema real", e["h2"]))
    for i, turno in enumerate(datos["turnos"], 1):
        fuentes = ", ".join(f["name"] for f in turno["fuentes"][:3]) or "—"
        F.append(Paragraph(
            f"<b>P{i}. {turno['pregunta']}</b><br/>"
            f"Agente: <font color='{BLUE}'>{turno['agente']}</font> · "
            f"Fuentes citadas: {fuentes}", e["cuerpo"]))
        respuesta = turno["respuesta"].strip().replace("\n", " ")
        if len(respuesta) > 420:
            respuesta = respuesta[:420] + "…"
        F.append(Paragraph(f"<i>«{respuesta}»</i>", e["cuerpo"]))
        if turno["tabla"]:
            filas = " · ".join(",".join(fila) for fila in turno["tabla"]["rows"][:3])
            F.append(Paragraph(
                f"Dato en vivo (SoQL): <b>{turno['tabla']['title']}</b> — "
                f"{', '.join(turno['tabla']['columns'])}: {filas}", e["cuerpo"]))
    F.append(Image(datos["fig_latencia"], width=15 * cm, height=7.5 * cm))
    F.append(Spacer(1, 0.3 * cm))
    F.append(Image(datos["fig_ingenieria"], width=15 * cm, height=7.1 * cm))
    F.append(PageBreak())

    # ---- evidencia visual (demo)
    F.append(Paragraph("5. Evidencia visual del prototipo", e["h1"]))
    F.append(Paragraph(
        "Capturas de la demo automatizada (demo/record-demo.js, Playwright sobre el "
        "sistema real; video completo en demo/OpenDataCopilot_Demo_ID241.mp4).", e["cuerpo"]))
    for captura, leyenda in datos["capturas_demo"]:
        F.append(Image(captura, width=14.5 * cm, height=8.16 * cm))
        F.append(Paragraph(leyenda, e["pie"]))
        F.append(Spacer(1, 0.3 * cm))
    F.append(PageBreak())

    # ---- conclusiones
    F.append(Paragraph("6. Conclusiones", e["h1"]))
    for conclusion in [
        "Los datos abiertos generan valor cuando cualquier persona puede usarlos: el "
        "prototipo demuestra que una pregunta en lenguaje natural basta para llegar al "
        "dato oficial citado.",
        "La arquitectura multiagente + RAG produce respuestas confiables y auditables, no "
        "un chatbot genérico: cada afirmación es trazable hasta su dataset de origen.",
        "La arquitectura hexagonal con puertos y adaptadores (ADR-0003) permitió construir "
        "con costo cercano a cero y desplegar con proveedores gestionados sin tocar el núcleo.",
        f"La disciplina de ingeniería ({m['tests_total']} tests, cobertura ≥95%, "
        f"{m['adrs']} ADRs, SonarCloud y SAST en CI) hace del prototipo una base lista "
        "para producción, no un experimento.",
    ]:
        F.append(Paragraph("• " + conclusion, e["cuerpo"]))
    F.append(Spacer(1, 0.8 * cm))
    F.append(HRFlowable(width="100%", thickness=1, color=colors.HexColor("#E2E8F0")))
    F.append(Paragraph(
        "OpenData Copilot · ID 241 · Datos al Ecosistema 2026 · Reporte generado "
        "automáticamente por reports/generar_reporte.py", e["pie"]))

    doc.build(F)
    print("PDF:", PDF)


# ---------------------------------------------------------------- main
def main() -> None:
    os.makedirs(FIGURES, exist_ok=True)

    print("Recolectando datos del sistema en", API)
    categorias = api_get("/catalog/categories")
    ingeridos = api_get("/catalog/count")["count"]
    total_portal = sum(c["count"] for c in categorias)

    turnos = []
    for pregunta in PREGUNTAS_EVIDENCIA:
        print("evidencia:", pregunta)
        turnos.append(chat(pregunta))

    metricas = metricas_repositorio()
    print("métricas repo:", metricas)

    fig1 = fig_categorias(categorias)
    fig2 = fig_relevancia(turnos)
    fig3 = fig_agentes(turnos, metricas["agentes"])
    fig4 = fig_latencia(turnos)
    fig5 = fig_ingenieria(metricas)

    # proporción real de la figura de relevancia para no deformarla en el PDF
    import PIL.Image
    with PIL.Image.open(fig2) as imagen:
        rel_ancho, rel_alto = imagen.size

    capturas = []
    for nombre, leyenda in [
        ("05-respuesta-con-fuentes-citadas.png",
         "Respuesta del Copilot con fuentes citadas, relevancia y enlace oficial."),
        ("06-cifras-tabla-datos-reales.png",
         "Cifra calculada con SoQL en vivo; la tabla llega al panel de artefactos."),
        ("08-panel-auditoria.png",
         "Panel de auditoría: cada interacción de cada agente queda registrada."),
    ]:
        ruta = os.path.join(CAPTURAS_DEMO, nombre)
        if os.path.exists(ruta):
            capturas.append((ruta, leyenda))

    construir_pdf({
        "categorias": categorias, "total_portal": total_portal, "ingeridos": ingeridos,
        "turnos": turnos, "metricas": metricas,
        "fig_categorias": fig1, "fig_relevancia": fig2, "fig_agentes": fig3,
        "fig_latencia": fig4, "fig_ingenieria": fig5,
        "rel_ancho": rel_ancho, "rel_alto": rel_alto,
        "capturas_demo": capturas,
    })
    print("Listo: reporte y figuras generados.")


if __name__ == "__main__":
    main()
