import { useCopilotStore, initialCopilotState } from "./useCopilotStore.ts";
import { chatApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
}));

const chat = vi.mocked(chatApi);

beforeEach(() => {
  vi.clearAllMocks();
  useCopilotStore.setState(initialCopilotState());
});

describe("useCopilotStore", () => {
  it("arranca con una conversación vacía y activa", () => {
    const state = useCopilotStore.getState();
    expect(state.conversations).toHaveLength(1);
    expect(state.conversations[0].messages).toHaveLength(0);
    expect(state.activeId).toBe(state.conversations[0].id);
    expect(state.status).toBe("idle");
  });

  it("envía y agrega el turno del usuario y del asistente, con fuentes, título e hilo", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "reco" };
      yield { kind: "sources", sources: [{ datasetId: "a", name: "N", sourceUrl: "https://x", score: 0.9 }] };
      yield { kind: "token", text: "Hola " };
      yield { kind: "token", text: "mundo" };
      yield { kind: "conversation", conversationId: "resp_1" };
      yield { kind: "done" };
    });

    useCopilotStore.setState({ input: "¿hay datos?" });
    await useCopilotStore.getState().send();

    const conversation = useCopilotStore.getState().conversations[0];
    expect(conversation.messages.map((m) => m.role)).toEqual(["user", "assistant"]);
    expect(conversation.messages[1].content).toBe("Hola mundo");
    expect(conversation.messages[1].agent).toBe("reco");
    expect(conversation.messages[1].sources).toHaveLength(1);
    expect(conversation.threadId).toBe("resp_1");
    expect(conversation.title).toBe("¿hay datos?");
    expect(useCopilotStore.getState().status).toBe("idle");
  });

  it("mantiene el hilo: el segundo envío reutiliza el threadId anterior", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "token", text: "ok" };
      yield { kind: "conversation", conversationId: "resp_1" };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "uno" });
    await useCopilotStore.getState().send();

    chat.stream.mockClear();
    chat.stream.mockImplementation(async function* () {
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "dos" });
    await useCopilotStore.getState().send();

    expect(chat.stream).toHaveBeenCalledWith("dos", "resp_1", expect.any(AbortSignal));
  });

  it("nueva conversación crea un hilo vacío y lo activa", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "token", text: "x" };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "hola" });
    await useCopilotStore.getState().send();
    expect(useCopilotStore.getState().conversations[0].messages.length).toBeGreaterThan(0);

    useCopilotStore.getState().newConversation();
    const state = useCopilotStore.getState();
    expect(state.conversations).toHaveLength(2);
    expect(state.conversations[0].messages).toHaveLength(0);
    expect(state.activeId).toBe(state.conversations[0].id);
  });

  it("nueva conversación es no-op si la activa ya está vacía", () => {
    const before = useCopilotStore.getState().conversations.length;
    useCopilotStore.getState().newConversation();
    expect(useCopilotStore.getState().conversations).toHaveLength(before);
  });

  it("seleccionar una conversación cambia la activa", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "hola" });
    await useCopilotStore.getState().send();
    const firstId = useCopilotStore.getState().activeId;

    useCopilotStore.getState().newConversation();
    const secondId = useCopilotStore.getState().activeId;
    expect(secondId).not.toBe(firstId);

    useCopilotStore.getState().selectConversation(firstId);
    expect(useCopilotStore.getState().activeId).toBe(firstId);
  });

  it("marca estado de error si el flujo falla", async () => {
    chat.stream.mockImplementation(() => {
      throw new Error("boom");
    });
    useCopilotStore.setState({ input: "hola" });
    await useCopilotStore.getState().send();

    expect(useCopilotStore.getState().status).toBe("error");
    expect(useCopilotStore.getState().error).toContain("boom");
  });
});
