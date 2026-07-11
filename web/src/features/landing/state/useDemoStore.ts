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

if (total === 0) {
  throw new Error("demoExchanges debe contener al menos un intercambio.");
}

/** Normaliza un índice arbitrario al rango [0, total) con envoltura circular. */
const wrapIndex = (index: number): number => ((index % total) + total) % total;

export const useDemoStore = create<DemoState>((set) => ({
  activeIndex: 0,
  active: demoExchanges[0],
  select: (index: number): void =>
    set(() => {
      const safeIndex: number = wrapIndex(index);
      return { activeIndex: safeIndex, active: demoExchanges[safeIndex] };
    }),
  next: (): void =>
    set((state: DemoState) => {
      const nextIndex: number = wrapIndex(state.activeIndex + 1);
      return { activeIndex: nextIndex, active: demoExchanges[nextIndex] };
    }),
}));
