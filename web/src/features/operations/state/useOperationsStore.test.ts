import { useOperationsStore } from "./useOperationsStore.ts";
import { catalogApi, searchApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  catalogApi: { count: vi.fn(), ingest: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

const catalog = vi.mocked(catalogApi);
const search = vi.mocked(searchApi);

beforeEach(() => {
  vi.clearAllMocks();
  useOperationsStore.setState({
    count: null,
    ingestLimit: 1000,
    catalogPhase: "idle",
    indexPhase: "idle",
    message: null,
  });
});

describe("useOperationsStore", () => {
  it("setIngestLimit acota el límite a un mínimo de 1", () => {
    useOperationsStore.getState().setIngestLimit(0);
    expect(useOperationsStore.getState().ingestLimit).toBe(1);

    useOperationsStore.getState().setIngestLimit(50);
    expect(useOperationsStore.getState().ingestLimit).toBe(50);
  });

  it("refreshCount guarda el conteo en éxito", async () => {
    catalog.count.mockResolvedValue({ count: 42 });

    await useOperationsStore.getState().refreshCount();

    expect(useOperationsStore.getState().count).toBe(42);
  });

  it("refreshCount guarda el mensaje de error en fallo", async () => {
    catalog.count.mockRejectedValue(new Error("sin conexión"));

    await useOperationsStore.getState().refreshCount();

    expect(useOperationsStore.getState().message).toBe("sin conexión");
  });

  it("updateCatalog marca ok, informa y refresca el conteo", async () => {
    catalog.ingest.mockResolvedValue({ datasetsIngested: 3 });
    catalog.count.mockResolvedValue({ count: 3 });

    await useOperationsStore.getState().updateCatalog();

    const state = useOperationsStore.getState();
    expect(state.catalogPhase).toBe("ok");
    expect(state.message).toContain("3");
    expect(state.count).toBe(3);
    expect(catalog.ingest).toHaveBeenCalledWith(1000);
  });

  it("updateCatalog marca error en fallo", async () => {
    catalog.ingest.mockRejectedValue(new Error("falló la ingesta"));

    await useOperationsStore.getState().updateCatalog();

    const state = useOperationsStore.getState();
    expect(state.catalogPhase).toBe("error");
    expect(state.message).toBe("falló la ingesta");
  });

  it("rebuildIndex marca ok e informa el conteo indexado", async () => {
    search.buildIndex.mockResolvedValue({ indexed: 7 });

    await useOperationsStore.getState().rebuildIndex();

    const state = useOperationsStore.getState();
    expect(state.indexPhase).toBe("ok");
    expect(state.message).toContain("7");
  });

  it("rebuildIndex usa el mensaje por defecto ante un error no-Error", async () => {
    search.buildIndex.mockRejectedValue("cadena suelta");

    await useOperationsStore.getState().rebuildIndex();

    const state = useOperationsStore.getState();
    expect(state.indexPhase).toBe("error");
    expect(state.message).toBe("Ocurrió un error inesperado.");
  });
});
