import { fireEvent, render, screen } from "@testing-library/react";
import { Composer } from "./Composer.tsx";
import { useCopilotStore, initialCopilotState, type Conversation } from "../state/useCopilotStore.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
  catalogApi: { count: vi.fn(), ingest: vi.fn(), categories: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

/** Siembra un hilo activo con los mensajes de usuario dados (intercalados con respuestas). */
function seedConversation(userContents: readonly string[]): void {
  const messages = userContents.flatMap((content, index) => [
    { id: `u${index}`, role: "user" as const, content },
    { id: `a${index}`, role: "assistant" as const, content: `resp ${index}` },
  ]);
  const conversation: Conversation = { id: "c1", title: "t", messages, threadId: null };
  useCopilotStore.setState({ conversations: [conversation], activeId: "c1", input: "" });
}

/** Coloca el cursor (colapsado) en la posición indicada del textarea. */
function setCaret(textbox: HTMLTextAreaElement, position: number): void {
  textbox.setSelectionRange(position, position);
}

function renderComposer(): HTMLTextAreaElement {
  render(<Composer variant="docked" />);
  return screen.getByRole<HTMLTextAreaElement>("textbox");
}

beforeEach(() => {
  vi.clearAllMocks();
  useCopilotStore.setState(initialCopilotState());
});

describe("Composer — navegación del historial con flechas", () => {
  it("↑ con el cursor al inicio recupera los mensajes del más reciente al más antiguo y se detiene en el más antiguo", () => {
    seedConversation(["primero", "segundo", "tercero"]);
    const textbox = renderComposer();
    setCaret(textbox, 0);

    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("tercero");
    expect(textbox.selectionStart).toBe(0); // el cursor queda estacionado al inicio para seguir subiendo

    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("segundo");
    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("primero");
    // Ya en el más antiguo, seguir subiendo no cambia nada.
    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("primero");
  });

  it("↓ con el cursor al final avanza a los más recientes y restaura el borrador al pasar el último", () => {
    seedConversation(["uno", "dos"]);
    const textbox = renderComposer();
    fireEvent.change(textbox, { target: { value: "mi borrador" } });

    setCaret(textbox, 0); // el cursor debe estar al inicio para entrar al historial con ↑
    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("dos");
    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("uno");

    setCaret(textbox, textbox.value.length); // al final para bajar
    fireEvent.keyDown(textbox, { key: "ArrowDown" });
    expect(textbox).toHaveValue("dos");
    expect(textbox.selectionStart).toBe(textbox.value.length); // estacionado al final para seguir bajando
    fireEvent.keyDown(textbox, { key: "ArrowDown" }); // pasa el último → restaura el borrador
    expect(textbox).toHaveValue("mi borrador");
  });

  it("no navega si el cursor está dentro del texto: ↑/↓ en medio de un párrafo no saltan de mensaje", () => {
    seedConversation(["corto", "linea1\nlinea2\nlinea3"]);
    const textbox = renderComposer();
    setCaret(textbox, 0);

    fireEvent.keyDown(textbox, { key: "ArrowUp" }); // recupera el párrafo más reciente
    expect(textbox).toHaveValue("linea1\nlinea2\nlinea3");

    // Cursor en medio del párrafo: ↑ no salta al mensaje anterior.
    setCaret(textbox, 8);
    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("linea1\nlinea2\nlinea3");
    // Cursor en medio del párrafo: ↓ tampoco salta.
    fireEvent.keyDown(textbox, { key: "ArrowDown" });
    expect(textbox).toHaveValue("linea1\nlinea2\nlinea3");

    // Solo al inicio absoluto ↑ salta al mensaje anterior.
    setCaret(textbox, 0);
    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("corto");
  });

  it("escribir sale del historial: la siguiente ↑ vuelve a empezar desde el más reciente", () => {
    seedConversation(["uno", "dos"]);
    const textbox = renderComposer();
    setCaret(textbox, 0);

    fireEvent.keyDown(textbox, { key: "ArrowUp" }); // "dos"
    fireEvent.keyDown(textbox, { key: "ArrowUp" }); // "uno"
    expect(textbox).toHaveValue("uno");

    fireEvent.change(textbox, { target: { value: "nuevo" } }); // escribir reinicia el modo historial
    expect(textbox).toHaveValue("nuevo");

    setCaret(textbox, 0);
    fireEvent.keyDown(textbox, { key: "ArrowUp" }); // empieza de nuevo desde el más reciente
    expect(textbox).toHaveValue("dos");
  });

  it("↓ sin estar navegando el historial no altera el texto", () => {
    seedConversation(["uno"]);
    const textbox = renderComposer();
    fireEvent.change(textbox, { target: { value: "algo" } });
    setCaret(textbox, textbox.value.length);

    fireEvent.keyDown(textbox, { key: "ArrowDown" });
    expect(textbox).toHaveValue("algo");
  });

  it("sin mensajes previos del usuario, ↑ no hace nada", () => {
    // Estado inicial: conversación vacía (sin mensajes).
    const textbox = renderComposer();
    setCaret(textbox, 0);

    fireEvent.keyDown(textbox, { key: "ArrowUp" });
    expect(textbox).toHaveValue("");
  });
});
