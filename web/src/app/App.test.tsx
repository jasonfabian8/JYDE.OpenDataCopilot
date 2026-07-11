import { render, screen } from "@testing-library/react";
import { App } from "./App.tsx";
import { catalogApi } from "../shared/api/client.ts";

vi.mock("../shared/api/client.ts", () => ({
  catalogApi: { count: vi.fn(), ingest: vi.fn() },
  searchApi: { buildIndex: vi.fn() },
  chatApi: { stream: vi.fn() },
}));

beforeEach(() => {
  vi.mocked(catalogApi).count.mockResolvedValue({ count: 0 });
});

describe("App", () => {
  it("compone la landing con sus secciones clave", () => {
    render(<App />);

    expect(screen.getByRole("heading", { level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Consola de ejecución")).toBeInTheDocument();
    const copilotLinks = screen.getAllByRole("link", { name: /abrir copilot/i });
    expect(copilotLinks.length).toBeGreaterThan(0);
    expect(copilotLinks[0]).toHaveAttribute("href", "/copilot/");
  });
});
