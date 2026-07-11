import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryContent } from "./MemoryContent.tsx";
import { useCopilotStore, initialCopilotState } from "../state/useCopilotStore.ts";

beforeEach(() => {
  useCopilotStore.setState(initialCopilotState());
});

describe("MemoryContent", () => {
  it("muestra el estado vacío de datasets", () => {
    render(<MemoryContent />);
    expect(screen.getByText(/Fija datasets desde las/)).toBeInTheDocument();
  });

  it("edita el objetivo de la conversación", async () => {
    const user = userEvent.setup();
    render(<MemoryContent />);

    await user.type(screen.getByLabelText(/Objetivo de la conversación/), "analizar mortalidad");
    expect(useCopilotStore.getState().objective).toBe("analizar mortalidad");
  });

  it("lista los datasets seleccionados y permite quitarlos", async () => {
    useCopilotStore.setState({
      selectedDatasets: [
        { id: "a", name: "Dataset A" },
        { id: "b", name: "Dataset B" },
      ],
    });
    const user = userEvent.setup();
    render(<MemoryContent />);

    expect(screen.getByText("Dataset A")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Quitar Dataset A" }));
    expect(useCopilotStore.getState().selectedDatasets.map((dataset) => dataset.id)).toEqual(["b"]);
  });

  it("limpia la memoria (objetivo y datasets)", async () => {
    useCopilotStore.setState({ objective: "algo", selectedDatasets: [{ id: "a", name: "A" }] });
    const user = userEvent.setup();
    render(<MemoryContent />);

    await user.click(screen.getByRole("button", { name: /Limpiar memoria/ }));
    expect(useCopilotStore.getState().objective).toBe("");
    expect(useCopilotStore.getState().selectedDatasets).toHaveLength(0);
  });
});
