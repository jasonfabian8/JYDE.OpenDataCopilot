import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { CopilotApp } from "./features/copilot/components/CopilotApp.tsx";
import "./styles/index.css";

const container: HTMLElement | null = document.getElementById("copilot-root");
if (container === null) {
  throw new Error("No se encontró el elemento raíz #copilot-root.");
}

createRoot(container).render(
  <StrictMode>
    <CopilotApp />
  </StrictMode>,
);
