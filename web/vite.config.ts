import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// Configuración de Vite para la SPA pública (ver ADR-0008 y ADR-0009).
// El proxy evita CORS en desarrollo: el front llama rutas relativas (/catalog, /search) y Vite las
// reenvía a la API. Debe coincidir con el puerto de la API (ver Api/Properties/launchSettings.json).
const apiTarget: string = "http://localhost:5244";

export default defineConfig({
  plugins: [react(), tailwindcss()],
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
        "src/vite-env.d.ts",
        "src/**/*.test.{ts,tsx}",
        "src/test/**",
        "src/**/*.d.ts",
      ],
    },
  },
});
