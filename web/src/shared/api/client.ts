/**
 * Cliente HTTP tipado de la API de OpenData Copilot. Es la frontera con el backend: los
 * componentes nunca llaman a `fetch` directamente (análogo a los puertos del backend).
 * La URL base sale de `VITE_API_BASE_URL`; en desarrollo queda vacía y se usa el proxy de Vite.
 */
const baseUrl: string = import.meta.env.VITE_API_BASE_URL ?? "";

/** Resultado de una ingesta del catálogo. */
export interface IngestResult {
  readonly datasetsIngested: number;
}

/** Conteo de datasets almacenados. */
export interface CountResult {
  readonly count: number;
}

/** Resultado de (re)construir el índice de búsqueda. */
export interface IndexResult {
  readonly indexed: number;
}

async function request<TResponse>(path: string, init?: RequestInit): Promise<TResponse> {
  const response: Response = await fetch(`${baseUrl}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...init,
  });

  if (!response.ok) {
    throw new Error(`La API respondió ${response.status} (${response.statusText}) en ${path}.`);
  }

  return (await response.json()) as TResponse;
}

/** Operaciones del catálogo. */
export const catalogApi = {
  /** Ingiere el catálogo desde la fuente (opcionalmente acotado por un límite). */
  ingest: (limit?: number): Promise<IngestResult> =>
    request<IngestResult>("/catalog/ingest", {
      method: "POST",
      body: JSON.stringify(typeof limit === "number" ? { limit } : {}),
    }),

  /** Devuelve la cantidad de datasets almacenados. */
  count: (): Promise<CountResult> => request<CountResult>("/catalog/count"),
};

/** Operaciones de búsqueda. */
export const searchApi = {
  /** (Re)construye el índice de búsqueda a partir del catálogo. */
  buildIndex: (): Promise<IndexResult> => request<IndexResult>("/search/index", { method: "POST" }),
};
