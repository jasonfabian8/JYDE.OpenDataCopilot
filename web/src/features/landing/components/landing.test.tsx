import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";
import { SourceCard } from "../../../shared/ui/SourceCard.tsx";
import { Hero } from "./Hero.tsx";
import { DemoConsole } from "./DemoConsole.tsx";
import { Mission } from "./Mission.tsx";
import { HowItWorks } from "./HowItWorks.tsx";
import { Principles } from "./Principles.tsx";
import { SiteHeader } from "./SiteHeader.tsx";
import { SiteFooter } from "./SiteFooter.tsx";
import { useDemoStore } from "../state/useDemoStore.ts";
import { demoExchanges } from "../model/demoExchanges.ts";

beforeEach(() => {
  useDemoStore.setState({ activeIndex: 0, active: demoExchanges[0] });
});

describe("UI compartida", () => {
  it("SectionLabel muestra su texto", () => {
    render(<SectionLabel>Etiqueta</SectionLabel>);
    expect(screen.getByText("Etiqueta")).toBeInTheDocument();
  });

  it("SourceCard cita el dataset y enlaza a la fuente oficial", () => {
    render(
      <SourceCard source={{ dataset: "Accidentes viales", entidad: "ANSV", url: "https://datos.gov.co/x" }} />,
    );
    expect(screen.getByText("Accidentes viales")).toBeInTheDocument();
    expect(screen.getByText("ANSV")).toBeInTheDocument();
    expect(screen.getByRole("link")).toHaveAttribute("href", "https://datos.gov.co/x");
  });
});

describe("secciones de la landing", () => {
  it("Hero renderiza el titular principal (h1)", () => {
    render(<Hero />);
    expect(screen.getByRole("heading", { level: 1 })).toBeInTheDocument();
  });

  it("Mission muestra su rótulo", () => {
    render(<Mission />);
    expect(screen.getByText("La misión")).toBeInTheDocument();
  });

  it("HowItWorks muestra su rótulo", () => {
    render(<HowItWorks />);
    expect(screen.getByText("Cómo funciona")).toBeInTheDocument();
  });

  it("Principles muestra su rótulo", () => {
    render(<Principles />);
    expect(screen.getByText("Principios")).toBeInTheDocument();
  });

  it("SiteHeader tiene navegación", () => {
    render(<SiteHeader />);
    expect(screen.getByRole("navigation")).toBeInTheDocument();
  });

  it("SiteFooter se renderiza como contentinfo", () => {
    render(<SiteFooter />);
    expect(screen.getByRole("contentinfo")).toBeInTheDocument();
  });

  it("DemoConsole muestra el intercambio activo y cambia al seleccionar otro", async () => {
    render(<DemoConsole />);
    expect(screen.getByText(demoExchanges[0].question)).toBeInTheDocument();

    if (demoExchanges.length > 1) {
      const user = userEvent.setup();
      const buttons = screen.getAllByRole("button");
      await user.click(buttons[1]);
      expect(screen.getByText(demoExchanges[1].question)).toBeInTheDocument();
    }
  });
});
