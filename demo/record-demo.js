/**
 * Demo automatizada de OpenData Copilot (Playwright).
 * Graba video 1920x1080 y toma capturas PNG de los momentos clave.
 *
 * Requisitos: API en http://localhost:5244 (catálogo vacío al iniciar) y
 * frontend Vite en http://localhost:5191.
 *
 * Salida: ./out/video.webm + ./out/capturas/*.png
 */
const { chromium } = require("playwright");
const path = require("path");
const fs = require("fs");

const BASE = "http://localhost:5191";
const OUT = path.join(__dirname, "out");
const SHOTS = path.join(OUT, "capturas");

const PAUSA_LECTURA = 6000; // pausa para que el espectador lea
const PAUSA_CORTA = 2000;

/** Espera a que existan N respuestas del asistente (etiquetas "VÍA <agente>") y que no esté transmitiendo. */
async function esperarTurnos(page, n) {
  await page.waitForFunction(
    (esperados) => {
      const vias = Array.from(document.querySelectorAll("p, span, div")).filter(
        (node) => /^V[ÍI]A\s/i.test((node.textContent || "").trim()) && node.children.length === 0,
      );
      const textarea = document.querySelector("textarea");
      return vias.length >= esperados && textarea !== null && !textarea.disabled;
    },
    n,
    { timeout: 300000 },
  );
}

async function main() {
  fs.mkdirSync(SHOTS, { recursive: true });

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 },
    deviceScaleFactor: 1,
    recordVideo: { dir: OUT, size: { width: 1920, height: 1080 } },
    locale: "es-CO",
  });
  const page = await context.newPage();
  let shot = 0;
  const inicio = Date.now();
  const tiempos = []; // [{escena, segundos}] para sincronizar la narración
  const marca = (escena) => tiempos.push({ escena, segundos: (Date.now() - inicio) / 1000 });
  const captura = async (nombre) => {
    shot += 1;
    marca(nombre);
    await page.screenshot({
      path: path.join(SHOTS, `${String(shot).padStart(2, "0")}-${nombre}.png`),
    });
  };

  // ── Escena 1: landing pública (identidad visual) ─────────────────────────
  await page.goto(BASE + "/", { waitUntil: "networkidle" });
  await page.waitForTimeout(PAUSA_LECTURA);
  await captura("landing-inicio");

  await page.mouse.wheel(0, 700);
  await page.waitForTimeout(PAUSA_CORTA);
  await page.mouse.wheel(0, 700);
  await page.waitForTimeout(PAUSA_LECTURA);
  await captura("landing-como-funciona");
  await page.mouse.wheel(0, -1600);
  await page.waitForTimeout(PAUSA_CORTA);

  // ── Escena 2: entrar al Copilot (pantalla inicial) ───────────────────────
  await page.click('a[href="/copilot/"]');
  await page.waitForLoadState("networkidle");
  await page.waitForTimeout(PAUSA_LECTURA);
  await captura("copilot-pantalla-inicial");

  // ── Escena 3: recomendación con fuentes citadas (RAG + GPT-4.1-mini) ─────
  const entrada = page.locator("textarea").first();
  await entrada.click();
  await entrada.pressSequentially(
    "¿Qué datasets hay sobre accidentalidad vial?",
    { delay: 42 },
  );
  await page.waitForTimeout(600);
  await captura("pregunta-al-copilot");
  await page.keyboard.press("Enter");

  await esperarTurnos(page, 1);
  await page.waitForTimeout(PAUSA_LECTURA + 1500);
  await captura("respuesta-con-fuentes-citadas");

  // ── Escena 4: cifras reales (SoQL en vivo) + artefactos ──────────────────
  const entrada2 = page.locator("textarea").first();
  await entrada2.click();
  await entrada2.pressSequentially(
    "¿Cuántos registros tiene el dataset de mantenimiento vial de Palmira?",
    { delay: 42 },
  );
  await page.waitForTimeout(400);
  await page.keyboard.press("Enter");

  // segundo turno; si hay tabla, el panel de artefactos se abre solo
  await esperarTurnos(page, 2);
  await page.waitForTimeout(PAUSA_LECTURA + 1000);
  await captura("cifras-tabla-datos-reales");

  // ── Escena 5: seguimiento conversacional (segunda recomendación) ────────
  const entrada3 = page.locator("textarea").first();
  await entrada3.click();
  await entrada3.pressSequentially(
    "¿Y qué datasets me recomiendas sobre seguridad vial en los municipios del Valle del Cauca?",
    { delay: 42 },
  );
  await page.waitForTimeout(500);
  await page.keyboard.press("Enter");
  await esperarTurnos(page, 3);
  await page.waitForTimeout(PAUSA_LECTURA + 2000);
  await captura("seguimiento-conversacional");

  // ── Escena 6: transparencia — auditoría de agentes ───────────────────────
  await page.click('button:has-text("Auditoría")');
  await page.waitForTimeout(PAUSA_LECTURA);
  await captura("panel-auditoria");

  // ── Escena 7: memoria de la conversación (objetivo) ──────────────────────
  await page.click('button:has-text("Memoria")');
  await page.waitForTimeout(PAUSA_LECTURA);
  await captura("panel-memoria-objetivo");

  // ── Cierre: vista general de la conversación ─────────────────────────────
  await page.click('button:has-text("Memoria")'); // cierra el panel
  await page.waitForTimeout(PAUSA_LECTURA);
  await captura("vista-final-conversacion");

  await context.close(); // finaliza el video
  await browser.close();

  // renombrar el video generado
  const video = fs.readdirSync(OUT).find((f) => f.endsWith(".webm"));
  if (video) {
    fs.renameSync(path.join(OUT, video), path.join(OUT, "demo.webm"));
  }
  fs.writeFileSync(path.join(OUT, "tiempos.json"), JSON.stringify(tiempos, null, 2));
  console.log("OK: video, capturas y tiempos en", OUT);
}

main().catch((error) => {
  console.error("FALLO:", error);
  process.exit(1);
});
