import { useState, type ReactElement } from "react";
import { Sidebar } from "./Sidebar.tsx";
import { ChatView } from "./ChatView.tsx";

/** Raíz de la app del Copilot: barra lateral + área de chat, a pantalla completa (tema oscuro). */
export function CopilotApp(): ReactElement {
  const [sidebarOpen, setSidebarOpen] = useState<boolean>(true);
  const toggleSidebar = (): void => setSidebarOpen((value) => !value);

  return (
    <div className="flex h-dvh overflow-hidden bg-night font-sans text-night-ink">
      <Sidebar open={sidebarOpen} onToggle={toggleSidebar} />
      <ChatView sidebarOpen={sidebarOpen} onToggleSidebar={toggleSidebar} />
    </div>
  );
}
