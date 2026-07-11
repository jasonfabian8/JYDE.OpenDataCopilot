import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ChatPanel } from "./ChatPanel.tsx";
import { useChatStore } from "../state/useChatStore.ts";
import { chatApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
}));

const chat = vi.mocked(chatApi);

beforeEach(() => {
  vi.clearAllMocks();
  useChatStore.setState({
    messages: [],
    input: "",
    status: "idle",
    agent: null,
    streamingAnswer: "",
    sources: null,
    conversationId: null,
    error: null,
  });
});

describe("ChatPanel", () => {
  it("renderiza el encabezado y las sugerencias con la conversación vacía", () => {
    render(<ChatPanel />);
    expect(screen.getByText("Pregúntale al Copilot")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /accidentalidad vial/i })).toBeInTheDocument();
  });

  it("escribe una pregunta, la envía y muestra la respuesta con sus fuentes", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "reco" };
      yield {
        kind: "sources",
        sources: [
          { datasetId: "a", name: "Fuente con enlace", sourceUrl: "https://datos.gov.co/a", score: 0.9 },
          { datasetId: "b", name: "Fuente sin enlace", sourceUrl: null, score: 0.7 },
        ],
      };
      yield { kind: "token", text: "Respuesta citada" };
      yield { kind: "done" };
    });
    const user = userEvent.setup();
    render(<ChatPanel />);

    await user.type(screen.getByRole("textbox"), "¿qué datasets hay?");
    await user.click(screen.getByRole("button", { name: /enviar/i }));

    expect(chat.stream).toHaveBeenCalled();
    expect(await screen.findByText("¿qué datasets hay?")).toBeInTheDocument();
    expect(await screen.findByText("Respuesta citada")).toBeInTheDocument();
    expect(await screen.findByText("Fuente con enlace")).toBeInTheDocument();
    expect(screen.getByText("Fuente sin enlace")).toBeInTheDocument();
    expect(screen.getByText("vía reco")).toBeInTheDocument();
  });

  it("tras enviar aparece 'Nueva conversación' y reinicia el hilo", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "token", text: "Hola" };
      yield { kind: "done" };
    });
    const user = userEvent.setup();
    render(<ChatPanel />);

    await user.type(screen.getByRole("textbox"), "hola");
    await user.click(screen.getByRole("button", { name: /enviar/i }));
    await screen.findByText("Hola");

    await user.click(await screen.findByRole("button", { name: /nueva conversación/i }));
    expect(useChatStore.getState().messages).toHaveLength(0);
  });

  it("una sugerencia rellena el input y dispara el envío", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "done" };
    });
    const user = userEvent.setup();
    render(<ChatPanel />);

    await user.click(screen.getByRole("button", { name: /accidentalidad vial/i }));

    expect(chat.stream).toHaveBeenCalled();
  });
});
