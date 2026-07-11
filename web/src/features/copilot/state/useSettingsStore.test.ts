import { useSettingsStore, FOCUS_CATEGORIES } from "./useSettingsStore.ts";
import { catalogApi, searchApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  catalogApi: { count: vi.fn(), ingest: vi.fn(), categories: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

const catalog = vi.mocked(catalogApi);
const search = vi.mocked(searchApi);

beforeEach(() => {
  vi.clearAllMocks();
  useSettingsStore.setState({
    open: false,
    count: null,
    categories: [],
    selected: new Set(),
    loadingCategories: false,
    phase: "idle",
    message: null,
  });
});

describe("useSettingsStore", () => {
  it("loadCategories preselecciona las categorías de foco disponibles", async () => {
    catalog.categories.mockResolvedValue([
      { name: "Transporte", count: 261 },
      { name: "Función pública", count: 2892 },
      { name: "Educación", count: 1372 },
    ]);

    await useSettingsStore.getState().loadCategories();

    const state = useSettingsStore.getState();
    expect(state.categories).toHaveLength(3);
    expect([...state.selected].sort()).toEqual(["Educación", "Transporte"]);
    expect(FOCUS_CATEGORIES).toContain("Transporte");
  });

  it("toggleCategory agrega y quita de la selección", () => {
    useSettingsStore.getState().toggleCategory("Salud");
    expect(useSettingsStore.getState().selected.has("Salud")).toBe(true);
    useSettingsStore.getState().toggleCategory("Salud");
    expect(useSettingsStore.getState().selected.has("Salud")).toBe(false);
  });

  it("selectAll y clearSelection operan sobre todas las categorías", () => {
    useSettingsStore.setState({ categories: [{ name: "A", count: 1 }, { name: "B", count: 2 }] });

    useSettingsStore.getState().selectAll();
    expect(useSettingsStore.getState().selected.size).toBe(2);

    useSettingsStore.getState().clearSelection();
    expect(useSettingsStore.getState().selected.size).toBe(0);
  });

  it("ingestSelected envía las categorías seleccionadas y refresca el conteo", async () => {
    catalog.ingest.mockResolvedValue({ datasetsIngested: 261 });
    catalog.count.mockResolvedValue({ count: 261 });
    useSettingsStore.setState({ selected: new Set(["Transporte"]) });

    await useSettingsStore.getState().ingestSelected();

    expect(catalog.ingest).toHaveBeenCalledWith({ categories: ["Transporte"] });
    const state = useSettingsStore.getState();
    expect(state.phase).toBe("ok");
    expect(state.count).toBe(261);
  });

  it("ingestSelected sin selección marca error y no llama al backend", async () => {
    await useSettingsStore.getState().ingestSelected();

    expect(catalog.ingest).not.toHaveBeenCalled();
    expect(useSettingsStore.getState().phase).toBe("error");
  });

  it("ingestAll carga todo el catálogo (body vacío)", async () => {
    catalog.ingest.mockResolvedValue({ datasetsIngested: 10000 });
    catalog.count.mockResolvedValue({ count: 10000 });

    await useSettingsStore.getState().ingestAll();

    expect(catalog.ingest).toHaveBeenCalledWith({});
    expect(useSettingsStore.getState().phase).toBe("ok");
  });

  it("rebuildIndex informa el conteo indexado", async () => {
    search.buildIndex.mockResolvedValue({ indexed: 3664 });

    await useSettingsStore.getState().rebuildIndex();

    const state = useSettingsStore.getState();
    expect(state.phase).toBe("ok");
    expect(state.message).toContain("3");
  });

  it("marca error si una operación falla", async () => {
    catalog.ingest.mockRejectedValue(new Error("backend caído"));

    await useSettingsStore.getState().ingestAll();

    expect(useSettingsStore.getState().phase).toBe("error");
    expect(useSettingsStore.getState().message).toBe("backend caído");
  });
});
