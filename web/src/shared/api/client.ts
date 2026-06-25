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

/** Fuente citada por el Copilot (dataset). */
export interface ChatSource {
  readonly datasetId: string;
  readonly name: string;
  readonly sourceUrl: string | null;
  readonly score: number;
}

/** Evento del flujo de chat (SSE). */
export type ChatEvent =
  | { readonly kind: "agent"; readonly agent: string }
  | { readonly kind: "sources"; readonly sources: ReadonlyArray<ChatSource> }
  | { readonly kind: "token"; readonly text: string }
  | { readonly kind: "conversation"; readonly conversationId: string }
  | { readonly kind: "done" };

function parseSseFrame(frame: string): ChatEvent | null {
  let eventName = "";
  let data = "";
  for (const line of frame.split("\n")) {
    if (line.startsWith("event:")) {
      eventName = line.slice("event:".length).trim();
    } else if (line.startsWith("data:")) {
      data += line.slice("data:".length).trim();
    }
  }

  if (eventName.length === 0) {
    return null;
  }

  const payload: Record<string, unknown> = data.length > 0 ? JSON.parse(data) : {};
  switch (eventName) {
    case "agent":
      return { kind: "agent", agent: String(payload.agent ?? "") };
    case "sources":
      return { kind: "sources", sources: (payload.sources as ReadonlyArray<ChatSource>) ?? [] };
    case "token":
      return { kind: "token", text: String(payload.text ?? "") };
    case "conversation":
      return { kind: "conversation", conversationId: String(payload.conversationId ?? "") };
    case "done":
      return { kind: "done" };
    default:
      return null;
  }
}

/** Operaciones del Copilot conversacional. */
export const chatApi = {
  /**
   * Envía una pregunta al Copilot y devuelve el flujo de eventos SSE (agent, sources, token, done).
   */
  async *stream(
    question: string,
    conversationId: string | null,
    signal: AbortSignal,
  ): AsyncGenerator<ChatEvent> {
    const body: Record<string, unknown> = { question };
    if (conversationId !== null) {
      body.conversationId = conversationId;
    }

    const response: Response = await fetch(`${baseUrl}/chat`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      signal,
    });

    if (!response.ok || response.body === null) {
      throw new Error(`El Copilot respondió ${response.status} (${response.statusText}).`);
    }

    const reader: ReadableStreamDefaultReader<Uint8Array> = response.body.getReader();
    const decoder: TextDecoder = new TextDecoder();
    let buffer = "";

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      let separator: number = buffer.indexOf("\n\n");
      while (separator >= 0) {
        const frame: string = buffer.slice(0, separator);
        buffer = buffer.slice(separator + 2);
        const event: ChatEvent | null = parseSseFrame(frame);
        if (event !== null) {
          yield event;
        }

        separator = buffer.indexOf("\n\n");
      }
    }
  },
};
