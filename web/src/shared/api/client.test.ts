import { catalogApi, searchApi, chatApi, type ChatEvent } from "./client.ts";

const fetchMock = vi.fn();

beforeEach(() => {
  vi.stubGlobal("fetch", fetchMock);
  fetchMock.mockReset();
});

afterEach(() => {
  vi.unstubAllGlobals();
});

function jsonResponse(body: unknown): Response {
  return { ok: true, status: 200, statusText: "OK", json: async () => body } as unknown as Response;
}

function sseResponse(frames: string): Response {
  const encoder = new TextEncoder();
  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      controller.enqueue(encoder.encode(frames));
      controller.close();
    },
  });
  return { ok: true, status: 200, statusText: "OK", body: stream } as unknown as Response;
}

describe("catalogApi", () => {
  it("count devuelve el conteo del backend", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ count: 5 }));

    const result = await catalogApi.count();

    expect(result.count).toBe(5);
    expect(fetchMock).toHaveBeenCalledWith("/catalog/count", expect.anything());
  });

  it("ingest sin límite envía un body vacío", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ datasetsIngested: 0 }));

    await catalogApi.ingest();

    const body: unknown = JSON.parse(fetchMock.mock.calls[0][1].body);
    expect(body).toEqual({});
  });

  it("ingest con límite lo incluye en el body", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ datasetsIngested: 3 }));

    await catalogApi.ingest(3);

    const body: unknown = JSON.parse(fetchMock.mock.calls[0][1].body);
    expect(body).toEqual({ limit: 3 });
  });

  it("lanza un error descriptivo ante un status no-ok", async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 404, statusText: "Not Found" } as Response);

    await expect(catalogApi.count()).rejects.toThrow(/404/);
  });
});

describe("searchApi", () => {
  it("buildIndex devuelve la cantidad indexada", async () => {
    fetchMock.mockResolvedValue(jsonResponse({ indexed: 9 }));

    const result = await searchApi.buildIndex();

    expect(result.indexed).toBe(9);
  });
});

describe("chatApi.stream", () => {
  it("parsea los frames SSE a eventos tipados y descarta los inválidos", async () => {
    const frames =
      [
        'event: agent\ndata: {"agent":"reco"}',
        'event: sources\ndata: {"sources":[{"datasetId":"a","name":"Uno","sourceUrl":null,"score":0.5}]}',
        'event: token\ndata: {"text":"hola"}',
        'event: conversation\ndata: {"conversationId":"c1"}',
        "event: done\ndata: {}",
        "event: desconocido\ndata: {}",
        "data: sin-evento",
      ].join("\n\n") + "\n\n";
    fetchMock.mockResolvedValue(sseResponse(frames));

    const events: ChatEvent[] = [];
    for await (const event of chatApi.stream("hola", null, new AbortController().signal)) {
      events.push(event);
    }

    expect(events.map((event) => event.kind)).toEqual(["agent", "sources", "token", "conversation", "done"]);
    expect(events[0]).toEqual({ kind: "agent", agent: "reco" });
    expect(events[2]).toEqual({ kind: "token", text: "hola" });
  });

  it("incluye conversationId en el body cuando hay hilo abierto", async () => {
    fetchMock.mockResolvedValue(sseResponse("event: done\ndata: {}\n\n"));

    const iterator = chatApi.stream("hola", "conv-9", new AbortController().signal);
    for await (const _event of iterator) {
      // consumir el stream para disparar el fetch
    }

    const body: unknown = JSON.parse(fetchMock.mock.calls[0][1].body);
    expect(body).toEqual({ question: "hola", conversationId: "conv-9" });
  });

  it("ignora líneas ajenas y normaliza campos ausentes o no-string a cadena vacía", async () => {
    const frames =
      [
        'event: agent\ndata: {"agent":42}',
        "event: token\nid: keep-alive\ndata: ",
        'event: conversation\ndata: {"conversationId":true}',
      ].join("\n\n") + "\n\n";
    fetchMock.mockResolvedValue(sseResponse(frames));

    const events: ChatEvent[] = [];
    for await (const event of chatApi.stream("x", null, new AbortController().signal)) {
      events.push(event);
    }

    expect(events).toEqual([
      { kind: "agent", agent: "" },
      { kind: "token", text: "" },
      { kind: "conversation", conversationId: "" },
    ]);
  });

  it("lanza si la respuesta no es ok", async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 500, statusText: "Server Error", body: null } as Response);

    await expect(async () => {
      for await (const _event of chatApi.stream("hola", null, new AbortController().signal)) {
        // no debería emitir nada
      }
    }).rejects.toThrow(/500/);
  });
});
