import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SettingsModal } from "./SettingsModal.tsx";
import { useSettingsStore } from "../state/useSettingsStore.ts";
import { catalogApi, searchApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
  catalogApi: { count: vi.fn(), ingest: vi.fn(), categories: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

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

describe("SettingsModal", () => {
  it("no renderiza nada si está cerrado", () => {
    const { container } = render(<SettingsModal />);
    expect(container).toBeEmptyDOMElement();
  });

  it("renderiza el diálogo con el conteo y las categorías cuando está abierto", () => {
    useSettingsStore.setState({ open: true, count: 1312, categories: [{ name: "Salud", count: 10 }] });
    render(<SettingsModal />);

    expect(screen.getByRole("dialog", { name: "Configuración" })).toBeInTheDocument();
    expect(screen.getByText("Salud")).toBeInTheDocument();
  });

  it("cierra con la tecla Escape", async () => {
    useSettingsStore.setState({ open: true });
    const user = userEvent.setup();
    render(<SettingsModal />);

    await user.keyboard("{Escape}");
    expect(useSettingsStore.getState().open).toBe(false);
  });

  it("reconstruye el índice al pulsar el botón", async () => {
    search.buildIndex.mockResolvedValue({ indexed: 5 });
    useSettingsStore.setState({ open: true });
    const user = userEvent.setup();
    render(<SettingsModal />);

    await user.click(screen.getByRole("button", { name: /Reconstruir índice/ }));
    expect(search.buildIndex).toHaveBeenCalled();
  });
});
