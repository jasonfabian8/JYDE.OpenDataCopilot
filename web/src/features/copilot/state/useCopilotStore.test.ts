import { useCopilotStore, initialCopilotState } from "./useCopilotStore.ts";
import { chatApi, catalogApi, searchApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
  catalogApi: { ingest: vi.fn(), count: vi.fn(), categories: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

const chat = vi.mocked(chatApi);
const catalog = vi.mocked(catalogApi);
const search = vi.mocked(searchApi);

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

    expect(chat.stream).toHaveBeenCalledWith("dos", "resp_1", expect.any(AbortSignal), "", [], "ok");
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

  it("guarda las categorías recomendadas y la consulta en el mensaje del asistente", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "category-recommender-agent" };
      yield {
        kind: "categories",
        query: "suicidio juvenil",
        categories: [{ name: "Salud y Protección Social", count: 1312, loaded: false, relevance: 0.9 }],
      };
      yield { kind: "token", text: "Carga Salud." };
      yield { kind: "done" };
    });

    useCopilotStore.setState({ input: "qué categorías cargo" });
    await useCopilotStore.getState().send();

    const messages = useCopilotStore.getState().conversations[0].messages;
    const assistant = messages[messages.length - 1];
    expect(assistant.categories).toHaveLength(1);
    expect(assistant.categories?.[0].name).toBe("Salud y Protección Social");
    expect(assistant.query).toBe("suicidio juvenil");
  });

  it("loadCategoryAndRetry ingiere la categoría, reconstruye el índice y reintenta la consulta", async () => {
    catalog.ingest.mockResolvedValue({ datasetsIngested: 1312 });
    search.buildIndex.mockResolvedValue({ indexed: 1312 });
    chat.stream.mockImplementation(async function* () {
      yield { kind: "token", text: "listo" };
      yield { kind: "done" };
    });

    await useCopilotStore.getState().loadCategoryAndRetry("Salud y Protección Social", "suicidio juvenil");

    expect(catalog.ingest).toHaveBeenCalledWith({ categories: ["Salud y Protección Social"] });
    expect(search.buildIndex).toHaveBeenCalled();
    expect(chat.stream).toHaveBeenCalledWith("suicidio juvenil", null, expect.any(AbortSignal), "", [], "");
    expect(useCopilotStore.getState().loadingCategory).toBeNull();
    expect(useCopilotStore.getState().loadedCategories).toContain("Salud y Protección Social");
  });

  it("loadCategoryAndRetry marca error si la carga falla", async () => {
    catalog.ingest.mockRejectedValue(new Error("falló la ingesta"));

    await useCopilotStore.getState().loadCategoryAndRetry("Transporte", "accidentalidad vial");

    expect(useCopilotStore.getState().status).toBe("error");
    expect(useCopilotStore.getState().loadingCategory).toBeNull();
  });

  it("aplica el evento objective y actualiza la memoria", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "objective", objective: "analizar la mortalidad y su relación con la deserción" };
      yield { kind: "token", text: "ok" };
      yield { kind: "done" };
    });

    useCopilotStore.setState({ input: "mortalidad" });
    await useCopilotStore.getState().send();

    expect(useCopilotStore.getState().objective).toBe("analizar la mortalidad y su relación con la deserción");
  });

  it("send envía el objetivo y los nombres de datasets seleccionados", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "done" };
    });
    useCopilotStore.setState({
      input: "hola",
      objective: "mi objetivo",
      selectedDatasets: [{ id: "a", name: "Dataset A" }],
    });

    await useCopilotStore.getState().send();

    expect(chat.stream).toHaveBeenCalledWith(
      "hola", null, expect.any(AbortSignal), "mi objetivo", [{ id: "a", name: "Dataset A" }], "");
  });

  it("pinDataset agrega sin duplicar y unpinDataset quita", () => {
    useCopilotStore.getState().pinDataset({ id: "a", name: "A" });
    useCopilotStore.getState().pinDataset({ id: "a", name: "A" });
    useCopilotStore.getState().pinDataset({ id: "b", name: "B" });
    expect(useCopilotStore.getState().selectedDatasets.map((d) => d.id)).toEqual(["a", "b"]);

    useCopilotStore.getState().unpinDataset("a");
    expect(useCopilotStore.getState().selectedDatasets.map((d) => d.id)).toEqual(["b"]);
  });

  it("setObjective y clearMemory editan la memoria", () => {
    useCopilotStore.getState().setObjective("nuevo objetivo");
    useCopilotStore.getState().pinDataset({ id: "a", name: "A" });

    useCopilotStore.getState().clearMemory();

    expect(useCopilotStore.getState().objective).toBe("");
    expect(useCopilotStore.getState().selectedDatasets).toHaveLength(0);
  });

  it("aplica eventos table y chart y llena el panel de artefactos", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "table", table: { title: "Mortalidad", columns: ["genero", "total"], rows: [["M", "1"], ["F", "2"]] } };
      yield { kind: "chart", chart: { title: "Mortalidad", type: "bar", xColumn: "genero", yColumn: "total" } };
      yield { kind: "token", text: "listo" };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "cuántas muertes por género" });

    await useCopilotStore.getState().send();

    const artifacts = useCopilotStore.getState().artifacts;
    expect(artifacts).toHaveLength(2);
    expect(artifacts[0].kind).toBe("table");
    const chart = artifacts[1];
    expect(chart.kind).toBe("chart");
    if (chart.kind === "chart") {
      expect(chart.rows).toHaveLength(2); // el gráfico lleva los datos de la tabla del turno
      expect(chart.xColumn).toBe("genero");
    }
    expect(useCopilotStore.getState().rightPanel).toBe("artifacts"); // el dock se abre en artefactos
  });

  it("aplica el evento audit y acumula la bitácora con el mensaje del usuario", async () => {
    chat.stream.mockImplementation(async function* () {
      yield {
        kind: "audit",
        interactions: [
          { agent: "router-agent", request: "enrutar", response: "dataset-recommender-agent" },
          { agent: "dataset-recommender-agent", request: "recomendar", response: "{...}" },
        ],
      };
      yield { kind: "token", text: "listo" };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "accidentalidad vial" });

    await useCopilotStore.getState().send();

    const auditLog = useCopilotStore.getState().auditLog;
    expect(auditLog).toHaveLength(1);
    expect(auditLog[0].userMessage).toBe("accidentalidad vial");
    expect(auditLog[0].interactions).toHaveLength(2);
    expect(auditLog[0].interactions[0].agent).toBe("router-agent");
    expect(auditLog[0].interactions[1].response).toBe("{...}");
  });

  it("auto-fija el dataset analizado cuando responde un agente de análisis", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "dataset-analyst-agent" };
      yield {
        kind: "sources",
        sources: [{ datasetId: "xpi4-vt35", name: "Indicadores de Morbilidad 2019", sourceUrl: "https://x", score: 0.95 }],
      };
      yield { kind: "token", text: "listo" };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "listame las columnas del dataset" });

    await useCopilotStore.getState().send();

    expect(useCopilotStore.getState().selectedDatasets).toEqual([
      { id: "xpi4-vt35", name: "Indicadores de Morbilidad 2019" },
    ]);
  });

  it("NO auto-fija cuando responde el recomendador (agente de descubrimiento)", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "agent", agent: "dataset-recommender-agent" };
      yield { kind: "sources", sources: [{ datasetId: "aaaa-0001", name: "Otro", sourceUrl: "https://x", score: 0.9 }] };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "recomiéndame datasets" });

    await useCopilotStore.getState().send();

    expect(useCopilotStore.getState().selectedDatasets).toHaveLength(0);
  });

  it("la memoria/artefactos/auditoría son POR conversación (no globales)", async () => {
    chat.stream.mockImplementation(async function* () {
      yield { kind: "objective", objective: "objetivo del hilo A" };
      yield { kind: "table", table: { title: "T", columns: ["c"], rows: [["1"]] } };
      yield {
        kind: "audit",
        interactions: [{ agent: "router-agent", request: "r", response: "s" }],
      };
      yield { kind: "token", text: "ok" };
      yield { kind: "done" };
    });
    useCopilotStore.setState({ input: "hola A" });
    await useCopilotStore.getState().send();
    const idA = useCopilotStore.getState().activeId;

    // Hilo A quedó con memoria, artefactos y auditoría.
    expect(useCopilotStore.getState().objective).toBe("objetivo del hilo A");
    expect(useCopilotStore.getState().artifacts).toHaveLength(1);
    expect(useCopilotStore.getState().auditLog).toHaveLength(1);

    // Nuevo hilo B: el espejo se vacía.
    useCopilotStore.getState().newConversation();
    expect(useCopilotStore.getState().objective).toBe("");
    expect(useCopilotStore.getState().artifacts).toHaveLength(0);
    expect(useCopilotStore.getState().auditLog).toHaveLength(0);
    useCopilotStore.getState().setObjective("objetivo del hilo B");

    // Volver a A restaura SU memoria/artefactos/auditoría; B conserva la suya.
    useCopilotStore.getState().selectConversation(idA);
    expect(useCopilotStore.getState().objective).toBe("objetivo del hilo A");
    expect(useCopilotStore.getState().artifacts).toHaveLength(1);
    expect(useCopilotStore.getState().auditLog).toHaveLength(1);
  });

  it("togglePanel alterna y closePanel cierra el dock derecho", () => {
    useCopilotStore.getState().togglePanel("memory");
    expect(useCopilotStore.getState().rightPanel).toBe("memory");
    useCopilotStore.getState().togglePanel("audit");
    expect(useCopilotStore.getState().rightPanel).toBe("audit");
    useCopilotStore.getState().togglePanel("audit"); // el mismo panel lo cierra
    expect(useCopilotStore.getState().rightPanel).toBe("none");
    useCopilotStore.getState().togglePanel("memory");
    useCopilotStore.getState().closePanel();
    expect(useCopilotStore.getState().rightPanel).toBe("none");
  });
});
