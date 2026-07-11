import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// Configuración de Vite (ver ADR-0008 y ADR-0009). Dos entradas (multi-página): la landing
// pública (`index.html`, informativa) y la app del Copilot (`copilot/index.html`, servida en
// `/copilot/`). Comparten el design system y el cliente de la API; no hay dependencias nuevas.
// El proxy evita CORS en desarrollo: el front llama rutas relativas (/catalog, /search, /chat) y
// Vite las reenvía a la API. Debe coincidir con el puerto de la API (ver Api/Properties/launchSettings.json).
const apiTarget: string = "http://localhost:5244";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    rollupOptions: {
      input: {
        main: "index.html",
        copilot: "copilot/index.html",
      },
    },
  },
  server: {
    port: 5191,
    proxy: {
      "/catalog": { target: apiTarget, changeOrigin: true },
      "/search": { target: apiTarget, changeOrigin: true },
      "/chat": { target: apiTarget, changeOrigin: true },
    },
  },
  // Testing del frontend con Vitest (ver ADR-0016). La cobertura sale en LCOV para SonarCloud.
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    css: true,
    coverage: {
      provider: "v8",
      reporter: ["text-summary", "lcov"],
      reportsDirectory: "./coverage",
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
        "src/main.tsx",
        "src/copilot.tsx",
        "src/vite-env.d.ts",
        "src/**/*.test.{ts,tsx}",
        "src/test/**",
        "src/**/*.d.ts",
      ],
    },
  },
});
