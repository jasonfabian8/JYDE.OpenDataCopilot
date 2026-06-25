import { type FormEvent, type ReactElement } from "react";
import { SectionLabel } from "../../../shared/ui/SectionLabel.tsx";
import { useChatStore, type ChatMessage } from "../state/useChatStore.ts";
import type { ChatSource } from "../../../shared/api/client.ts";

const suggestions: readonly string[] = [
  "¿Qué datasets hay sobre accidentalidad vial?",
  "Datos de cobertura de salud por municipio",
  "Información de calidad del aire",
];

function Sources({ sources }: { readonly sources: ReadonlyArray<ChatSource> }): ReactElement {
  return (
    <ul className="mt-3 space-y-2">
      {sources.map((source) => (
        <li key={source.datasetId} className="rounded-lg border border-line bg-paper-2 px-3 py-2 text-sm">
          {source.sourceUrl === null ? (
            <span className="font-medium">{source.name}</span>
          ) : (
            <a href={source.sourceUrl} target="_blank" rel="noreferrer" className="font-medium text-cobalt hover:underline">
              {source.name}
            </a>
          )}
          <span className="ml-2 font-mono text-xs text-muted">{(source.score * 100).toFixed(0)}%</span>
        </li>
      ))}
    </ul>
  );
}

function Bubble({ message }: { readonly message: ChatMessage }): ReactElement {
  if (message.role === "user") {
    return (
      <div className="flex justify-end">
        <div className="max-w-[85%] rounded-2xl rounded-br-sm bg-cobalt px-4 py-2.5 text-paper">{message.content}</div>
      </div>
    );
  }

  return (
    <div className="flex justify-start">
      <div className="max-w-[90%] rounded-2xl rounded-bl-sm border border-line bg-card px-4 py-3">
        {message.agent !== undefined && (
          <p className="mb-1 font-mono text-xs uppercase tracking-[0.15em] text-muted">vía {message.agent}</p>
        )}
        {message.sources !== undefined && message.sources.length > 0 && <Sources sources={message.sources} />}
        {message.content.length > 0 && <p className="mt-3 whitespace-pre-wrap leading-relaxed">{message.content}</p>}
      </div>
    </div>
  );
}

/** Sección de chat: conversa con el Copilot (streaming SSE) y muestra las fuentes citadas. */
export function ChatPanel(): ReactElement {
  const messages: ReadonlyArray<ChatMessage> = useChatStore((state) => state.messages);
  const input: string = useChatStore((state) => state.input);
  const status: string = useChatStore((state) => state.status);
  const agent: string | null = useChatStore((state) => state.agent);
  const streamingAnswer: string = useChatStore((state) => state.streamingAnswer);
  const sources: ReadonlyArray<ChatSource> | null = useChatStore((state) => state.sources);
  const error: string | null = useChatStore((state) => state.error);
  const setInput = useChatStore((state) => state.setInput);
  const send = useChatStore((state) => state.send);
  const newConversation = useChatStore((state) => state.newConversation);

  const isStreaming: boolean = status === "streaming";

  function handleSubmit(event: FormEvent): void {
    event.preventDefault();
    void send();
  }

  function handleSuggestion(text: string): void {
    setInput(text);
    void send();
  }

  return (
    <section id="chat" className="border-b border-line bg-paper">
      <div className="mx-auto max-w-3xl px-6 py-20 lg:py-28">
        <div className="flex items-start justify-between gap-4">
          <div>
            <SectionLabel>Chat</SectionLabel>
            <h2 className="mt-6 font-display text-4xl font-medium tracking-tight sm:text-5xl">Pregúntale al Copilot</h2>
          </div>
          {messages.length > 0 && (
            <button
              type="button"
              onClick={(): void => newConversation()}
              disabled={isStreaming}
              className="mt-2 shrink-0 rounded-full border border-line bg-card px-4 py-2 font-mono text-xs uppercase tracking-[0.15em] text-ink-soft transition hover:border-cobalt hover:text-cobalt disabled:opacity-50"
            >
              Nueva conversación
            </button>
          )}
        </div>
        <p className="mt-4 leading-relaxed text-ink-soft">
          Haz una pregunta en lenguaje natural; el Copilot busca en datos.gov.co y te recomienda los
          conjuntos de datos relevantes, citando la fuente. Recuerda el contexto de la conversación.
        </p>

        <div className="mt-10 space-y-5">
          {messages.map((message, index) => (
            <Bubble key={index} message={message} />
          ))}

          {isStreaming && (
            <Bubble
              message={{
                role: "assistant",
                content: streamingAnswer,
                agent: agent ?? undefined,
                sources: sources ?? undefined,
              }}
            />
          )}

          {messages.length === 0 && !isStreaming && (
            <div className="flex flex-wrap gap-2">
              {suggestions.map((text) => (
                <button
                  key={text}
                  type="button"
                  onClick={(): void => handleSuggestion(text)}
                  className="rounded-full border border-line bg-card px-4 py-2 text-sm text-ink-soft transition hover:border-cobalt hover:text-cobalt"
                >
                  {text}
                </button>
              ))}
            </div>
          )}
        </div>

        {error !== null && (
          <p role="alert" className="mt-6 rounded-lg border border-amber/40 bg-amber/10 px-4 py-3 text-sm">
            {error}
          </p>
        )}

        <form onSubmit={handleSubmit} className="mt-8 flex gap-3">
          <input
            type="text"
            value={input}
            disabled={isStreaming}
            onChange={(event): void => setInput(event.target.value)}
            placeholder="¿Cuáles son los datasets de accidentalidad vial?"
            className="flex-1 rounded-xl border border-line bg-card px-4 py-3 text-ink outline-none focus:border-cobalt disabled:opacity-60"
          />
          <button
            type="submit"
            disabled={isStreaming || input.trim().length === 0}
            className="rounded-xl bg-cobalt px-6 py-3 font-medium text-white transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isStreaming ? "Pensando…" : "Enviar"}
          </button>
        </form>
      </div>
    </section>
  );
}
