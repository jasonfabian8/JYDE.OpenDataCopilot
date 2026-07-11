import type { DatasetSource } from "./DatasetSource.ts";

/**
 * Intercambio de ejemplo mostrado en la consola de demostración: una pregunta en
 * lenguaje natural, la respuesta citada, la consulta SoQL generada y sus fuentes.
 * Es contenido ilustrativo de la landing, no una llamada real al backend.
 */
export interface DemoExchange {
  /** Identificador estable para listas de React. */
  readonly id: string;
  /** Pregunta del usuario en lenguaje natural. */
  readonly question: string;
  /** Respuesta sintetizada por el copiloto. */
  readonly answer: string;
  /** Consulta SoQL que el copiloto ejecutaría contra la API de Socrata. */
  readonly soql: string;
  /** Fuentes oficiales que respaldan la respuesta. */
  readonly sources: readonly DatasetSource[];
}
