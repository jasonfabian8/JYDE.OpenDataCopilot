import { useEffect, useRef, type KeyboardEvent, type ReactElement } from "react";
import { useCopilotStore } from "../state/useCopilotStore.ts";
import { SendIcon } from "./icons.tsx";

/** Cuadro de entrada del Copilot (textarea autoajustable + botón de envío). */
export function Composer({ variant }: { readonly variant: "centered" | "docked" }): ReactElement {
  const input: string = useCopilotStore((state) => state.input);
  const status: string = useCopilotStore((state) => state.status);
  const setInput = useCopilotStore((state) => state.setInput);
  const send = useCopilotStore((state) => state.send);

  const isStreaming: boolean = status === "streaming";
  const canSend: boolean = input.trim().length > 0 && !isStreaming;

  const textareaRef = useRef<HTMLTextAreaElement>(null);
  useEffect((): void => {
    const element: HTMLTextAreaElement | null = textareaRef.current;
    if (element === null) {
      return;
    }
    element.style.height = "0px";
    element.style.height = `${Math.min(element.scrollHeight, 200)}px`;
  }, [input]);

  function submit(): void {
    if (canSend) {
      void send();
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLTextAreaElement>): void {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      submit();
    }
  }

  return (
    <div className="flex items-end gap-2 rounded-2xl border border-night-line bg-night-3 px-3 py-2.5 transition focus-within:border-sky/50">
      <textarea
        ref={textareaRef}
        value={input}
        rows={1}
        disabled={isStreaming}
        onChange={(event): void => setInput(event.target.value)}
        onKeyDown={onKeyDown}
        placeholder={variant === "centered" ? "¿Cómo puedo ayudarte con los datos abiertos?" : "Escribe tu pregunta…"}
        className="max-h-[200px] flex-1 resize-none bg-transparent py-1.5 text-[15px] leading-relaxed text-night-ink placeholder:text-night-muted focus:outline-none disabled:opacity-60"
      />
      <button
        type="button"
        onClick={submit}
        disabled={!canSend}
        aria-label="Enviar"
        className="mb-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-sky text-night transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-40"
      >
        <SendIcon className="h-4 w-4" />
      </button>
    </div>
  );
}
