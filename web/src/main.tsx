import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./app/App.tsx";
import "./styles/index.css";

const container: HTMLElement | null = document.getElementById("root");
if (container === null) {
  throw new Error("No se encontró el elemento raíz #root.");
}

createRoot(container).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
