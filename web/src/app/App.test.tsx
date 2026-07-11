import { render, screen } from "@testing-library/react";
import { App } from "./App.tsx";

describe("App", () => {
  it("compone la landing informativa con su titular y el acceso al Copilot", () => {
    render(<App />);

    expect(screen.getByRole("heading", { level: 1 })).toBeInTheDocument();
    const copilotLinks = screen.getAllByRole("link", { name: /abrir copilot/i });
    expect(copilotLinks.length).toBeGreaterThan(0);
    expect(copilotLinks[0]).toHaveAttribute("href", "/copilot/");
  });
});
