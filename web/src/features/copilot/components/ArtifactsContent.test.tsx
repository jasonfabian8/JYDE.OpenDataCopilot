import { render, screen } from "@testing-library/react";
import { ArtifactsContent } from "./ArtifactsContent.tsx";
import { useCopilotStore, initialCopilotState, type Artifact } from "../state/useCopilotStore.ts";

beforeEach(() => {
  useCopilotStore.setState(initialCopilotState());
});

const table: Artifact = {
  id: "t1",
  kind: "table",
  title: "Mortalidad",
  columns: ["genero", "total"],
  rows: [
    ["Masculino", "120"],
    ["Femenino", "98"],
  ],
};

const chart: Artifact = {
  id: "c1",
  kind: "chart",
  title: "Mortalidad",
  type: "bar",
  xColumn: "genero",
  yColumn: "total",
  columns: ["genero", "total"],
  rows: [
    ["Masculino", "120"],
    ["Femenino", "98"],
  ],
};

describe("ArtifactsContent", () => {
  it("muestra el estado vacío", () => {
    render(<ArtifactsContent />);
    expect(screen.getByText(/Aún no hay artefactos/)).toBeInTheDocument();
  });

  it("renderiza una tabla con sus columnas y celdas", () => {
    useCopilotStore.setState({ artifacts: [table] });
    render(<ArtifactsContent />);

    expect(screen.getByRole("table")).toBeInTheDocument();
    expect(screen.getByText("genero")).toBeInTheDocument();
    expect(screen.getByText("Masculino")).toBeInTheDocument();
    expect(screen.getByText("120")).toBeInTheDocument();
  });

  it("renderiza un gráfico de barras como SVG (una barra por fila)", () => {
    useCopilotStore.setState({ artifacts: [chart] });
    render(<ArtifactsContent />);

    const svg = screen.getByRole("img", { name: "Mortalidad" });
    expect(svg.querySelectorAll("rect")).toHaveLength(2);
  });

  it("renderiza un gráfico de línea como polyline", () => {
    useCopilotStore.setState({ artifacts: [{ ...chart, id: "c2", type: "line" }] });
    render(<ArtifactsContent />);

    const svg = screen.getByRole("img", { name: "Mortalidad" });
    expect(svg.querySelector("polyline")).not.toBeNull();
  });

  it("degrada si el gráfico referencia columnas inexistentes", () => {
    useCopilotStore.setState({ artifacts: [{ ...chart, id: "c3", xColumn: "edad" }] });
    render(<ArtifactsContent />);

    expect(screen.getByText(/no se pudo dibujar el gráfico/)).toBeInTheDocument();
  });
});
