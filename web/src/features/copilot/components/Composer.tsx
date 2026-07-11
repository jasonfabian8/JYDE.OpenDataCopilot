import { useEffect, useLayoutEffect, useMemo, useRef, type KeyboardEvent, type ReactElement } from "react";
import { useCopilotStore, type CopilotMessage } from "../state/useCopilotStore.ts";
import { SendIcon } from "./icons.tsx";

/** Referencia estable para hilos sin mensajes (evita recomputar selectores). */
const EMPTY_MESSAGES: ReadonlyArray<CopilotMessage> = [];

/** Cuadro de entrada del Copilot (textarea autoajustable + botón de envío). */
export function Composer({ variant }: { readonly variant: "centered" | "docked" }): ReactElement {
  const input: string = useCopilotStore((state) => state.input);
  const status: string = useCopilotStore((state) => state.status);
  const activeId: string = useCopilotStore((state) => state.activeId);
  const setInput = useCopilotStore((state) => state.setInput);
  const send = useCopilotStore((state) => state.send);
  // Mensajes del hilo activo (referencia estable mientras no cambien).
  const messages: ReadonlyArray<CopilotMessage> = useCopilotStore(
    (state) => state.conversations.find((item) => item.id === state.activeId)?.messages ?? EMPTY_MESSAGES,
  );
  // Historial navegable: solo los mensajes del usuario, del más antiguo al más reciente.
  const userMessages: ReadonlyArray<string> = useMemo(
    () => messages.filter((message) => message.role === "user").map((message) => message.content),
    [messages],
  );

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

  // Al terminar el streaming, el textarea se reactiva y RECUPERA el foco para seguir escribiendo.
  const wasStreaming = useRef<boolean>(false);
  useEffect((): void => {
    if (wasStreaming.current && !isStreaming) {
      textareaRef.current?.focus();
    }
    wasStreaming.current = isStreaming;
  }, [isStreaming]);

  // Posición dentro del historial: null = escribiendo (borrador); 0..n-1 = mensaje recuperado.
  const historyPos = useRef<number | null>(null);
  // Borrador que el usuario estaba escribiendo antes de entrar al historial (se restaura al bajar).
  const draft = useRef<string>("");
  // Dónde recolocar el cursor tras recuperar un mensaje: "start" al subir (para seguir subiendo) y
  // "end" al bajar (para seguir bajando). Se aplica en un layout effect tras re-renderizar el valor.
  const pendingCaret = useRef<"start" | "end" | null>(null);
  useLayoutEffect((): void => {
    const node: HTMLTextAreaElement | null = textareaRef.current;
    if (node === null || pendingCaret.current === null) {
      return;
    }
    const position: number = pendingCaret.current === "start" ? 0 : node.value.length;
    node.focus();
    node.setSelectionRange(position, position);
    pendingCaret.current = null;
  }, [input]);

  // Al enviar (cambia el nº de mensajes) o cambiar de hilo, salimos del modo historial.
  useEffect((): void => {
    historyPos.current = null;
    draft.current = "";
  }, [activeId, userMessages.length]);

  function submit(): void {
    if (canSend) {
      send();
    }
  }

  /** True si el cursor (sin selección) está al inicio absoluto del texto. */
  function caretAtStart(element: HTMLTextAreaElement): boolean {
    return element.selectionStart === 0 && element.selectionEnd === 0;
  }

  /** True si el cursor (sin selección) está al final absoluto del texto. */
  function caretAtEnd(element: HTMLTextAreaElement): boolean {
    const end: number = element.value.length;
    return element.selectionStart === end && element.selectionEnd === end;
  }

  /** Recupera un mensaje del historial y estaciona el cursor en el borde indicado. */
  function showFromHistory(position: number, caret: "start" | "end"): void {
    historyPos.current = position;
    pendingCaret.current = caret;
    setInput(userMessages[position]);
  }

  /** ↑ (cursor al inicio): retrocede a mensajes más antiguos. */
  function historyUp(): void {
    if (userMessages.length === 0) {
      return;
    }
    if (historyPos.current === null) {
      // Entramos al historial: guardamos el borrador actual y saltamos al más reciente.
      draft.current = input;
      showFromHistory(userMessages.length - 1, "start");
      return;
    }
    showFromHistory(Math.max(0, historyPos.current - 1), "start");
  }

  /** ↓ (cursor al final): avanza a mensajes más recientes; al pasar el último, restaura el borrador. */
  function historyDown(): void {
    if (historyPos.current === null) {
      return;
    }
    const next: number = historyPos.current + 1;
    if (next >= userMessages.length) {
      historyPos.current = null;
      pendingCaret.current = "end";
      setInput(draft.current);
      return;
    }
    showFromHistory(next, "end");
  }

  function onKeyDown(event: KeyboardEvent<HTMLTextAreaElement>): void {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      submit();
      return;
    }

    const element: HTMLTextAreaElement = event.currentTarget;

    // Navegación entre mensajes SOLO en los bordes: ↑ al inicio, ↓ al final. En cualquier otra
    // posición la flecha mueve el cursor dentro del texto (no pisa la edición de un párrafo).
    if (event.key === "ArrowUp" && caretAtStart(element) && userMessages.length > 0) {
      event.preventDefault();
      historyUp();
      return;
    }

    if (event.key === "ArrowDown" && caretAtEnd(element) && historyPos.current !== null) {
      event.preventDefault();
      historyDown();
    }
  }

  function onChange(value: string): void {
    // Escribir sale del modo historial: lo que hay pasa a ser el borrador; no recolocamos el cursor.
    historyPos.current = null;
    pendingCaret.current = null;
    setInput(value);
  }

  return (
    <div className="flex items-end gap-2 rounded-2xl border border-night-line bg-night-3 px-3 py-2.5 transition focus-within:border-sky/50">
      <textarea
        ref={textareaRef}
        value={input}
        rows={1}
        disabled={isStreaming}
        onChange={(event): void => onChange(event.target.value)}
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
