import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// Configuración de Vite para la SPA pública (ver ADR-0008 y ADR-0009).
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5191,
  },
});
