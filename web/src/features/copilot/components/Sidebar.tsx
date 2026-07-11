import type { ReactElement } from "react";
import { useCopilotStore, type Conversation } from "../state/useCopilotStore.ts";
import { BrandMark } from "./BrandMark.tsx";
import { ChatBubbleIcon, HomeIcon, PanelIcon, PlusIcon } from "./icons.tsx";

/** Barra lateral: marca, «Nuevo chat», lista de conversaciones de la sesión y pie. */
export function Sidebar({
  open,
  onToggle,
}: {
  readonly open: boolean;
  readonly onToggle: () => void;
}): ReactElement | null {
  const conversations: ReadonlyArray<Conversation> = useCopilotStore((state) => state.conversations);
  const activeId: string = useCopilotStore((state) => state.activeId);
  const status: string = useCopilotStore((state) => state.status);
  const newConversation = useCopilotStore((state) => state.newConversation);
  const selectConversation = useCopilotStore((state) => state.selectConversation);

  if (!open) {
    return null;
  }

  const listed: ReadonlyArray<Conversation> = conversations.filter((c) => c.messages.length > 0);
  const isStreaming: boolean = status === "streaming";

  return (
    <>
      {/* Telón para cerrar en pantallas pequeñas (la barra se superpone). */}
      <button
        type="button"
        aria-label="Cerrar menú"
        onClick={onToggle}
        className="fixed inset-0 z-30 bg-black/50 md:hidden"
      />

      <aside className="fixed inset-y-0 left-0 z-40 flex w-[264px] shrink-0 flex-col border-r border-night-line bg-night-2 md:static md:z-auto">
        <div className="flex items-center justify-between px-4 py-4">
          <BrandMark />
          <button
            type="button"
            onClick={onToggle}
            aria-label="Contraer menú"
            className="rounded-md p-1.5 text-night-soft transition hover:bg-night-3 hover:text-night-ink"
          >
            <PanelIcon className="h-5 w-5" />
          </button>
        </div>

        <div className="px-3">
          <button
            type="button"
            onClick={(): void => newConversation()}
            disabled={isStreaming}
            className="flex w-full items-center gap-2 rounded-lg border border-night-line bg-night-3 px-3 py-2.5 text-sm font-medium text-night-ink transition hover:border-sky/50 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <PlusIcon className="h-4 w-4" />
            Nuevo chat
          </button>
        </div>

        <nav className="mt-4 flex-1 overflow-y-auto px-3">
          <p className="px-2 pb-2 font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">Chats</p>
          {listed.length === 0 ? (
            <p className="px-2 text-sm text-night-muted">Aún no hay conversaciones.</p>
          ) : (
            <ul className="space-y-0.5">
              {listed.map((conversation) => (
                <li key={conversation.id}>
                  <button
                    type="button"
                    onClick={(): void => selectConversation(conversation.id)}
                    disabled={isStreaming}
                    className={`flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-left text-sm transition disabled:cursor-not-allowed ${
                      conversation.id === activeId
                        ? "bg-night-3 text-night-ink"
                        : "text-night-soft hover:bg-night-3/60"
                    }`}
                  >
                    <ChatBubbleIcon className="h-4 w-4 shrink-0 text-night-muted" />
                    <span className="truncate">{conversation.title}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </nav>

        <div className="border-t border-night-line p-3">
          <a
            href="/"
            className="flex items-center gap-2 rounded-lg px-2.5 py-2 text-sm text-night-soft transition hover:bg-night-3/60 hover:text-night-ink"
          >
            <HomeIcon className="h-4 w-4" />
            Volver al inicio
          </a>
          <p className="px-2.5 pt-2 text-xs text-night-muted">Datos de datos.gov.co · siempre citados</p>
        </div>
      </aside>
    </>
  );
}
