import type { ReactElement } from "react";
import { SiteHeader } from "../features/landing/components/SiteHeader.tsx";
import { Hero } from "../features/landing/components/Hero.tsx";
import { Mission } from "../features/landing/components/Mission.tsx";
import { HowItWorks } from "../features/landing/components/HowItWorks.tsx";
import { Principles } from "../features/landing/components/Principles.tsx";
import { SiteFooter } from "../features/landing/components/SiteFooter.tsx";

/** Raíz de la landing pública (informativa): compone las secciones del sitio.
 *  El chat conversacional y las operaciones viven en la app del Copilot (`/copilot/`). */
export function App(): ReactElement {
  return (
    <>
      <SiteHeader />
      <main>
        <Hero />
        <Mission />
        <HowItWorks />
        <Principles />
      </main>
      <SiteFooter />
    </>
  );
}
