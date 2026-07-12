import { useEffect, useState, type ReactElement } from "react";
import { useCopilotStore } from "../state/useCopilotStore.ts";
import { Sidebar } from "./Sidebar.tsx";
import { ChatView } from "./ChatView.tsx";
import { SettingsModal } from "./SettingsModal.tsx";
import { FloatingToolbar } from "./FloatingToolbar.tsx";
import { RightDock } from "./RightDock.tsx";

/** Raíz de la app del Copilot: barra lateral + chat + panel derecho acoplado, a pantalla completa. */
export function CopilotApp(): ReactElement {
  const [sidebarOpen, setSidebarOpen] = useState<boolean>(true);
  const toggleSidebar = (): void => setSidebarOpen((value) => !value);
  const loadConversations = useCopilotStore((state) => state.loadConversations);

  // Al abrir la app, trae de la BD las conversaciones guardadas (guardado manual, ver ADR-0017).
  useEffect((): void => {
    void loadConversations();
  }, [loadConversations]);

  return (
    <div className="flex h-dvh overflow-hidden bg-night font-sans text-night-ink">
      <Sidebar open={sidebarOpen} onToggle={toggleSidebar} />
      {/* Contenedor relativo: el chat ocupa el espacio y el panel derecho lo empuja (no lo tapa). */}
      <div className="relative flex min-w-0 flex-1">
        <ChatView sidebarOpen={sidebarOpen} onToggleSidebar={toggleSidebar} />
        <FloatingToolbar />
      </div>
      <RightDock />
      <SettingsModal />
    </div>
  );
}
