import type { ReactElement } from "react";
import { useCopilotStore, type RightPanel } from "../state/useCopilotStore.ts";
import { CloseIcon } from "./icons.tsx";
import { MemoryContent } from "./MemoryContent.tsx";
import { ArtifactsContent } from "./ArtifactsContent.tsx";
import { AuditContent } from "./AuditContent.tsx";

const TITLES: Record<Exclude<RightPanel, "none">, string> = {
  memory: "Memoria",
  artifacts: "Artefactos",
  audit: "Auditoría",
};

/**
 * Panel derecho ACOPLADO (no se sobrepone al chat): es un hijo flex que empuja al chat en lugar de
 * taparlo, para poder seguir escribiendo con el panel abierto. El usuario decide cuándo cerrarlo.
 * Memoria, artefactos y auditoría son excluyentes (un solo panel a la vez).
 */
export function RightDock(): ReactElement | null {
  const rightPanel: RightPanel = useCopilotStore((state) => state.rightPanel);
  const closePanel = useCopilotStore((state) => state.closePanel);

  if (rightPanel === "none") {
    return null;
  }

  return (
    <aside className="flex h-full w-[42%] min-w-[340px] max-w-2xl shrink-0 flex-col border-l border-night-line bg-night-2">
      <div className="flex shrink-0 items-center justify-between border-b border-night-line px-5 py-4">
        <h2 className="font-display text-xl font-medium text-night-ink">{TITLES[rightPanel]}</h2>
        <button
          type="button"
          onClick={(): void => closePanel()}
          aria-label="Cerrar panel"
          className="rounded-md p-1.5 text-night-soft transition hover:bg-night-3 hover:text-night-ink"
        >
          <CloseIcon className="h-5 w-5" />
        </button>
      </div>
      <div className="flex-1 overflow-y-auto px-5 py-5">
        {rightPanel === "memory" && <MemoryContent />}
        {rightPanel === "artifacts" && <ArtifactsContent />}
        {rightPanel === "audit" && <AuditContent />}
      </div>
    </aside>
  );
}
