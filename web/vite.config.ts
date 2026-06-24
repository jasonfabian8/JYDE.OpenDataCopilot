import { defineConfig } from "vite";
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
    },
  },
});
