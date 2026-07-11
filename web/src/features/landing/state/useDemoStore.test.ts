import { useDemoStore } from "./useDemoStore.ts";
import { demoExchanges } from "../model/demoExchanges.ts";

const total: number = demoExchanges.length;

beforeEach(() => {
  useDemoStore.setState({ activeIndex: 0, active: demoExchanges[0] });
});

describe("useDemoStore", () => {
  it("inicia en el primer intercambio", () => {
    const state = useDemoStore.getState();
    expect(state.activeIndex).toBe(0);
    expect(state.active).toBe(demoExchanges[0]);
  });

  it("select fija el intercambio por índice", () => {
    useDemoStore.getState().select(1 % total);
    expect(useDemoStore.getState().activeIndex).toBe(1 % total);
    expect(useDemoStore.getState().active).toBe(demoExchanges[1 % total]);
  });

  it("select envuelve un índice fuera de rango (por arriba y por debajo)", () => {
    useDemoStore.getState().select(total + 1);
    expect(useDemoStore.getState().activeIndex).toBe(1 % total);

    useDemoStore.getState().select(-1);
    expect(useDemoStore.getState().activeIndex).toBe(total - 1);
  });

  it("next avanza y da la vuelta al final", () => {
    useDemoStore.getState().select(total - 1);
    useDemoStore.getState().next();
    expect(useDemoStore.getState().activeIndex).toBe(0);
    expect(useDemoStore.getState().active).toBe(demoExchanges[0]);
  });
});
