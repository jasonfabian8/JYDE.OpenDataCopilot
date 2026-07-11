import type { ReactElement, ReactNode } from "react";
import { useCopilotStore, type RightPanel } from "../state/useCopilotStore.ts";
import { AuditIcon, ChartBarIcon, MemoryIcon } from "./icons.tsx";

interface ToolbarButtonProps {
  readonly active: boolean;
  readonly label: string;
  readonly icon: ReactNode;
  readonly badge?: number;
  readonly onClick: () => void;
}

function ToolbarButton({ active, label, icon, badge, onClick }: ToolbarButtonProps): ReactElement {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={`pointer-events-auto flex items-center gap-2 rounded-full border px-4 py-2.5 text-sm shadow-lg backdrop-blur transition ${
        active
          ? "border-sky bg-sky/15 text-sky"
          : "border-night-line bg-night-2/90 text-night-ink hover:border-sky/50"
      }`}
    >
      {icon}
      {label}
      {badge !== undefined && badge > 0 && (
        <span className="rounded-full bg-sky/20 px-1.5 text-xs font-mono text-sky">{badge}</span>
      )}
    </button>
  );
}

/**
 * Botones flotantes (dentro del área del chat) que alternan el panel derecho acoplado:
 * memoria, artefactos y auditoría. No se sobreponen al chat porque el panel es un dock, no un overlay.
 */
export function FloatingToolbar(): ReactElement {
  const rightPanel: RightPanel = useCopilotStore((state) => state.rightPanel);
  const togglePanel = useCopilotStore((state) => state.togglePanel);
  const selectedCount: number = useCopilotStore((state) => state.selectedDatasets.length);
  const artifactsCount: number = useCopilotStore((state) => state.artifacts.length);
  const auditCount: number = useCopilotStore((state) => state.auditLog.length);

  return (
    <div className="pointer-events-none absolute right-5 top-20 z-20 flex flex-col items-end gap-2">
      <ToolbarButton
        active={rightPanel === "memory"}
        label="Memoria"
        icon={<MemoryIcon className="h-4 w-4 text-sky" />}
        badge={selectedCount}
        onClick={(): void => togglePanel("memory")}
      />
      {artifactsCount > 0 && (
        <ToolbarButton
          active={rightPanel === "artifacts"}
          label="Artefactos"
          icon={<ChartBarIcon className="h-4 w-4 text-sky" />}
          badge={artifactsCount}
          onClick={(): void => togglePanel("artifacts")}
        />
      )}
      {auditCount > 0 && (
        <ToolbarButton
          active={rightPanel === "audit"}
          label="Auditoría"
          icon={<AuditIcon className="h-4 w-4 text-sky" />}
          badge={auditCount}
          onClick={(): void => togglePanel("audit")}
        />
      )}
    </div>
  );
}
