import { create } from "zustand";
import { demoExchanges } from "../model/demoExchanges.ts";
import type { DemoExchange } from "../model/DemoExchange.ts";

/** Estado de la consola de demostración de la landing (manejado con Zustand, ver ADR-0008). */
interface DemoState {
  /** Índice del intercambio actualmente visible. */
  readonly activeIndex: number;
  /** Intercambio actualmente visible, derivado del índice. */
  readonly active: DemoExchange;
  /** Selecciona un intercambio por índice. */
  readonly select: (index: number) => void;
  /** Avanza al siguiente intercambio (con envoltura circular). */
  readonly next: () => void;
}

const total: number = demoExchanges.length;

export const useDemoStore = create<DemoState>((set) => ({
  activeIndex: 0,
  active: demoExchanges[0],
  select: (index: number): void =>
    set({ activeIndex: index, active: demoExchanges[index] }),
  next: (): void =>
    set((state: DemoState) => {
      const index: number = (state.activeIndex + 1) % total;
      return { activeIndex: index, active: demoExchanges[index] };
    }),
}));
