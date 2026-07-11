import { useChatStore } from "./useChatStore.ts";
import { chatApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
}));

const chat = vi.mocked(chatApi);

function resetChat(): void {
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
}

beforeEach(() => {
  vi.clearAllMocks();
  resetChat();
});

describe("useChatStore", () => {
  it("setInput actualiza el texto de entrada", () => {
    useChatStore.getState().setInput("hola");
    expect(useChatStore.getState().input).toBe("hola");
  });

  it("send no hace nada si el input está vacío", async () => {
    useChatStore.setState({ input: "   " });

    await useChatStore.getState().send();

    expect(chat.stream).not.toHaveBeenCalled();
    expect(useChatStore.getState().messages).toHaveLength(0);
  });

  it("send consolida la respuesta con agente, fuentes y conversationId", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "reco" };
      yield {
        kind: "sources",
        sources: [{ datasetId: "a", name: "Uno", sourceUrl: null, score: 0.9 }],
      };
      yield { kind: "token", text: "Hola " };
      yield { kind: "token", text: "mundo" };
      yield { kind: "conversation", conversationId: "conv-1" };
      yield { kind: "done" };
    });
    useChatStore.setState({ input: "  ¿qué datasets hay?  " });

    await useChatStore.getState().send();

    const state = useChatStore.getState();
    expect(state.status).toBe("idle");
    expect(state.conversationId).toBe("conv-1");
    expect(state.input).toBe("");
    expect(state.messages).toHaveLength(2);
    expect(state.messages[0]).toMatchObject({ role: "user", content: "¿qué datasets hay?" });
    expect(state.messages[1]).toMatchObject({ role: "assistant", content: "Hola mundo", agent: "reco" });
    expect(state.messages[1].sources).toHaveLength(1);
  });

  it("send marca error si el stream falla", async () => {
    chat.stream.mockImplementation(async function* () {
      throw new Error("SSE caído");
    });
    useChatStore.setState({ input: "hola" });

    await useChatStore.getState().send();

    const state = useChatStore.getState();
    expect(state.status).toBe("error");
    expect(state.error).toBe("SSE caído");
    expect(state.messages).toHaveLength(1); // el mensaje del usuario permanece
  });

  it("send usa el mensaje por defecto ante un error no-Error", async () => {
    chat.stream.mockImplementation(async function* () {
      throw "cadena suelta";
    });
    useChatStore.setState({ input: "hola" });

    await useChatStore.getState().send();

    expect(useChatStore.getState().error).toBe("Ocurrió un error inesperado.");
  });

  it("newConversation reinicia el estado", () => {
    useChatStore.setState({
      messages: [{ id: "x", role: "user", content: "hola" }],
      conversationId: "conv-9",
      error: "algo",
    });

    useChatStore.getState().newConversation();

    const state = useChatStore.getState();
    expect(state.messages).toHaveLength(0);
    expect(state.conversationId).toBeNull();
    expect(state.error).toBeNull();
  });
});
