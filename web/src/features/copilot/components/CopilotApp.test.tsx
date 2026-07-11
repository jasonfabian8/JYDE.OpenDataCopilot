import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CopilotApp } from "./CopilotApp.tsx";
import { useCopilotStore, initialCopilotState } from "../state/useCopilotStore.ts";
import { chatApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
}));

const chat = vi.mocked(chatApi);

beforeEach(() => {
  vi.clearAllMocks();
  useCopilotStore.setState(initialCopilotState());
});

describe("CopilotApp", () => {
  it("muestra el estado vacío con saludo, sugerencias y acciones de la barra", () => {
    render(<CopilotApp />);

    expect(screen.getByRole("heading", { level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /accidentalidad vial/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /nuevo chat/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /volver al inicio/i })).toHaveAttribute("href", "/");
  });

  it("envía una pregunta y muestra la respuesta con su agente y fuentes", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "reco" };
      yield {
        kind: "sources",
        sources: [{ datasetId: "a", name: "Fuente A", sourceUrl: "https://datos.gov.co/a", score: 0.9 }],
      };
      yield { kind: "token", text: "Respuesta citada" };
      yield { kind: "done" };
    });
    const user = userEvent.setup();
    render(<CopilotApp />);

    await user.type(screen.getByRole("textbox"), "¿qué datasets hay?");
    await user.click(screen.getByRole("button", { name: /^enviar$/i }));

    // El hilo vive en <main>; el título también aparece en la barra lateral (<aside>).
    const thread = within(screen.getByRole("main"));
    expect(chat.stream).toHaveBeenCalled();
    expect(await thread.findByText("¿qué datasets hay?")).toBeInTheDocument();
    expect(await thread.findByText("Respuesta citada")).toBeInTheDocument();
    expect(await thread.findByText("Fuente A")).toBeInTheDocument();
    expect(thread.getByText("vía reco")).toBeInTheDocument();
  });

  it("una sugerencia dispara el envío", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "done" };
    });
    const user = userEvent.setup();
    render(<CopilotApp />);

    await user.click(screen.getByRole("button", { name: /calidad del aire/i }));

    expect(chat.stream).toHaveBeenCalled();
  });
});
