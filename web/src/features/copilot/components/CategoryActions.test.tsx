import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CategoryActions } from "./CategoryActions.tsx";
import { useCopilotStore, initialCopilotState } from "../state/useCopilotStore.ts";
import { catalogApi, searchApi, chatApi } from "../../../shared/api/client.ts";

vi.mock("../../../shared/api/client.ts", () => ({
  chatApi: { stream: vi.fn() },
  catalogApi: { ingest: vi.fn(), count: vi.fn(), categories: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
}));

const catalog = vi.mocked(catalogApi);
const search = vi.mocked(searchApi);
const chat = vi.mocked(chatApi);

const categories = [
  { name: "Salud y Protección Social", count: 1312, loaded: false, relevance: 0.9 },
  { name: "Transporte", count: 500, loaded: true, relevance: 0.5 },
];

beforeEach(() => {
  vi.clearAllMocks();
  useCopilotStore.setState(initialCopilotState());
});

describe("CategoryActions", () => {
  it("muestra un botón para la no cargada y un chip para la ya cargada", () => {
    render(<CategoryActions categories={categories} query="suicidio" />);

    expect(screen.getByRole("button", { name: /Salud y Protección Social/ })).toBeInTheDocument();
    expect(screen.getByText("(cargada)")).toBeInTheDocument();
  });

  it("al hacer clic carga la categoría (ingesta + índice) y reintenta", async () => {
    catalog.ingest.mockResolvedValue({ datasetsIngested: 1312 });
    search.buildIndex.mockResolvedValue({ indexed: 1312 });
    chat.stream.mockImplementation(async function* () {
      yield { kind: "done" };
    });
    const user = userEvent.setup();
    render(<CategoryActions categories={categories} query="suicidio" />);

    await user.click(screen.getByRole("button", { name: /Salud y Protección Social/ }));

    await waitFor(() => expect(catalog.ingest).toHaveBeenCalledWith({ categories: ["Salud y Protección Social"] }));
    await waitFor(() => expect(search.buildIndex).toHaveBeenCalled());
  });
});
