import { render, screen, fireEvent } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { OperationsPanel } from "./OperationsPanel.tsx";
import { useOperationsStore } from "../state/useOperationsStore.ts";
import { catalogApi, searchApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  catalogApi: { count: vi.fn(), ingest: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

const catalog = vi.mocked(catalogApi);
const search = vi.mocked(searchApi);

beforeEach(() => {
  vi.clearAllMocks();
  catalog.count.mockResolvedValue({ count: 10 });
  catalog.ingest.mockResolvedValue({ datasetsIngested: 2 });
  search.buildIndex.mockResolvedValue({ indexed: 4 });
  useOperationsStore.setState({
    count: null,
    ingestLimit: 1000,
    catalogPhase: "idle",
    indexPhase: "idle",
    message: null,
  });
});

describe("OperationsPanel", () => {
  it("muestra el conteo al montar (useEffect → refreshCount)", async () => {
    render(<OperationsPanel />);
    expect(await screen.findByText("10")).toBeInTheDocument();
  });

  it("actualiza el catálogo e informa el resultado", async () => {
    const user = userEvent.setup();
    render(<OperationsPanel />);

    await user.click(screen.getByRole("button", { name: /actualizar catálogo/i }));

    expect(catalog.ingest).toHaveBeenCalledWith(1000);
    expect(await screen.findByText(/2 datasets ingeridos/i)).toBeInTheDocument();
  });

  it("reconstruye el índice", async () => {
    const user = userEvent.setup();
    render(<OperationsPanel />);

    await user.click(screen.getByRole("button", { name: /reconstruir índice/i }));

    expect(search.buildIndex).toHaveBeenCalled();
    expect(await screen.findByText(/4 datasets/i)).toBeInTheDocument();
  });

  it("el input de límite actualiza el store (onChange)", () => {
    render(<OperationsPanel />);
    fireEvent.change(screen.getByRole("spinbutton"), { target: { value: "5" } });
    expect(useOperationsStore.getState().ingestLimit).toBe(5);
  });

  it("el botón Refrescar vuelve a consultar el conteo", async () => {
    const user = userEvent.setup();
    render(<OperationsPanel />);
    expect(await screen.findByText("10")).toBeInTheDocument();

    catalog.count.mockResolvedValue({ count: 20 });
    await user.click(screen.getByRole("button", { name: /refrescar/i }));

    expect(await screen.findByText("20")).toBeInTheDocument();
  });
});
