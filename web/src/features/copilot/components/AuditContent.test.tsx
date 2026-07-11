import { render, screen } from "@testing-library/react";
import { AuditContent } from "./AuditContent.tsx";
import { useCopilotStore, initialCopilotState } from "../state/useCopilotStore.ts";

beforeEach(() => {
  useCopilotStore.setState(initialCopilotState());
});

describe("AuditContent", () => {
  it("muestra el estado vacío", () => {
    render(<AuditContent />);
    expect(screen.getByText(/Aún no hay interacciones/)).toBeInTheDocument();
  });

  it("muestra el turno del usuario y las interacciones de los agentes", () => {
    useCopilotStore.setState({
      auditLog: [
        {
          id: "e1",
          userMessage: "accidentalidad vial",
          interactions: [
            { agent: "router-agent", request: "enrutar", response: "elige figuras" },
            { agent: "figures-agent", request: "SoQL", response: "{...}" },
          ],
        },
      ],
    });
    render(<AuditContent />);

    expect(screen.getByText("accidentalidad vial")).toBeInTheDocument();
    expect(screen.getByText("router-agent")).toBeInTheDocument();
    expect(screen.getByText("figures-agent")).toBeInTheDocument();
    expect(screen.getByText("enrutar")).toBeInTheDocument();
  });

  it("indica cuando un turno no tiene interacciones", () => {
    useCopilotStore.setState({
      auditLog: [{ id: "e2", userMessage: "hola", interactions: [] }],
    });
    render(<AuditContent />);

    expect(screen.getByText(/Sin interacciones registradas/)).toBeInTheDocument();
  });
});
