import type { ReactElement } from "react";
import { useCopilotStore, type Conversation } from "../state/useCopilotStore.ts";
import { MessageList } from "./MessageList.tsx";
import { Composer } from "./Composer.tsx";
import { EmptyState } from "./EmptyState.tsx";
import { PanelIcon } from "./icons.tsx";

/** Área principal: barra superior + hilo de mensajes (o estado vacío) + cuadro de entrada. */
export function ChatView({
  sidebarOpen,
  onToggleSidebar,
}: {
  readonly sidebarOpen: boolean;
  readonly onToggleSidebar: () => void;
}): ReactElement {
  const conversations: ReadonlyArray<Conversation> = useCopilotStore((state) => state.conversations);
  const activeId: string = useCopilotStore((state) => state.activeId);
  const status: string = useCopilotStore((state) => state.status);
  const error: string | null = useCopilotStore((state) => state.error);

  const active: Conversation | undefined = conversations.find((c) => c.id === activeId);
  const isStreaming: boolean = status === "streaming";
  const hasThread: boolean = (active?.messages.length ?? 0) > 0 || isStreaming;

  return (
    <main className="flex min-w-0 flex-1 flex-col bg-night">
      <header className="flex h-14 shrink-0 items-center gap-3 border-b border-night-line px-4">
        <button
          type="button"
          onClick={onToggleSidebar}
          aria-label={sidebarOpen ? "Contraer menú" : "Expandir menú"}
          className={`rounded-md p-1.5 text-night-soft transition hover:bg-night-3 hover:text-night-ink ${
            sidebarOpen ? "md:hidden" : ""
          }`}
        >
          <PanelIcon className="h-5 w-5" />
        </button>
        <span className="font-mono text-xs uppercase tracking-[0.18em] text-night-muted">OpenData Copilot</span>
      </header>

      {hasThread ? (
        <>
          <MessageList />
          <div className="shrink-0 border-t border-night-line px-4 py-4">
            <div className="mx-auto max-w-3xl">
              {error !== null && (
                <p
                  role="alert"
                  className="mb-3 rounded-lg border border-amber/40 bg-amber/10 px-4 py-2.5 text-sm text-night-ink"
                >
                  {error}
                </p>
              )}
              <Composer variant="docked" />
            </div>
          </div>
        </>
      ) : (
        <EmptyState />
      )}
    </main>
  );
}
