import { useState, type ReactElement } from "react";
import { useCopilotStore, type Conversation } from "../state/useCopilotStore.ts";
import { useSettingsStore } from "../state/useSettingsStore.ts";
import { BrandMark } from "./BrandMark.tsx";
import { ChatBubbleIcon, GearIcon, HomeIcon, KebabIcon, PanelIcon, PlusIcon, SaveIcon, TrashIcon } from "./icons.tsx";

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
  const saveConversation = useCopilotStore((state) => state.saveConversation);
  const deleteConversation = useCopilotStore((state) => state.deleteConversation);
  const openSettings = useSettingsStore((state) => state.openSettings);
  // Id de la conversación cuyo menú de acciones (⋮) está abierto; null = ninguno.
  const [menuId, setMenuId] = useState<string | null>(null);

  if (!open) {
    return null;
  }

  // Se listan las conversaciones con mensajes y las guardadas en la BD (aunque aún no estén hidratadas).
  const listed: ReadonlyArray<Conversation> = conversations.filter(
    (c) => c.messages.length > 0 || c.persisted === true,
  );
  const isStreaming: boolean = status === "streaming";

  const confirmDelete = (conversation: Conversation): void => {
    const message: string = conversation.persisted === true
      ? `¿Eliminar «${conversation.title}» de la base de datos? Se borrarán también su memoria, artefactos y auditoría.`
      : `¿Eliminar «${conversation.title}»?`;
    if (window.confirm(message)) {
      void deleteConversation(conversation.id);
    }
  };

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
        {/* Telón que cierra el menú de acciones (⋮) al hacer clic fuera. */}
        {menuId !== null && (
          <button
            type="button"
            aria-hidden="true"
            tabIndex={-1}
            onClick={(): void => setMenuId(null)}
            className="fixed inset-0 z-10 cursor-default"
          />
        )}
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
                <li
                  key={conversation.id}
                  className={`relative flex items-center rounded-lg transition ${
                    conversation.id === activeId ? "bg-night-3 text-night-ink" : "text-night-soft hover:bg-night-3/60"
                  }`}
                >
                  <button
                    type="button"
                    onClick={(): void => void selectConversation(conversation.id)}
                    disabled={isStreaming}
                    className="flex min-w-0 flex-1 items-center gap-2 px-2.5 py-2 text-left text-sm disabled:cursor-not-allowed"
                  >
                    <ChatBubbleIcon className="h-4 w-4 shrink-0 text-night-muted" />
                    <span className="truncate">{conversation.title}</span>
                    {conversation.persisted === true && (
                      <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-sky/70" aria-label="Guardada" title="Guardada en la base de datos" />
                    )}
                  </button>
                  <button
                    type="button"
                    onClick={(): void => setMenuId(menuId === conversation.id ? null : conversation.id)}
                    disabled={isStreaming}
                    aria-label={`Acciones de ${conversation.title}`}
                    aria-haspopup="menu"
                    aria-expanded={menuId === conversation.id}
                    className="mr-1 shrink-0 rounded p-1 text-night-muted transition hover:bg-night-2 hover:text-night-ink disabled:cursor-not-allowed disabled:opacity-30"
                  >
                    <KebabIcon className="h-4 w-4" />
                  </button>
                  {menuId === conversation.id && (
                    <div
                      role="menu"
                      className="absolute right-1 top-9 z-20 w-40 overflow-hidden rounded-lg border border-night-line bg-night-2 py-1 shadow-xl"
                    >
                      {conversation.hydrated !== false && (
                        <button
                          type="button"
                          role="menuitem"
                          onClick={(): void => {
                            setMenuId(null);
                            void saveConversation(conversation.id);
                          }}
                          className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-night-soft transition hover:bg-night-3 hover:text-night-ink"
                        >
                          <SaveIcon className="h-4 w-4" />
                          Guardar
                        </button>
                      )}
                      <button
                        type="button"
                        role="menuitem"
                        onClick={(): void => {
                          setMenuId(null);
                          confirmDelete(conversation);
                        }}
                        className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm text-night-soft transition hover:bg-night-3 hover:text-amber"
                      >
                        <TrashIcon className="h-4 w-4" />
                        Eliminar
                      </button>
                    </div>
                  )}
                </li>
              ))}
            </ul>
          )}
        </nav>

        <div className="border-t border-night-line p-3">
          <button
            type="button"
            onClick={(): void => openSettings()}
            className="flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-left text-sm text-night-soft transition hover:bg-night-3/60 hover:text-night-ink"
          >
            <GearIcon className="h-4 w-4" />
            Configuraciones
          </button>
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
