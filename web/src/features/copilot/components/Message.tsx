import type { ReactElement } from "react";
import type { CopilotMessage } from "../state/useCopilotStore.ts";
import { Sources } from "./Sources.tsx";
import { CategoryActions } from "./CategoryActions.tsx";

/** Indicador de «pensando» mientras llega el primer token. */
function ThinkingDots(): ReactElement {
  return (
    <div className="flex items-center gap-1.5 py-1" aria-label="Pensando">
      <span className="h-2 w-2 animate-pulse rounded-full bg-night-muted [animation-delay:-0.3s]" />
      <span className="h-2 w-2 animate-pulse rounded-full bg-night-muted [animation-delay:-0.15s]" />
      <span className="h-2 w-2 animate-pulse rounded-full bg-night-muted" />
    </div>
  );
}

/** Un turno de la conversación: burbuja del usuario o bloque del asistente (con fuentes). */
export function Message({
  message,
  streaming = false,
}: {
  readonly message: CopilotMessage;
  readonly streaming?: boolean;
}): ReactElement {
  if (message.role === "user") {
    return (
      <div className="flex justify-end">
        <div className="max-w-[80%] whitespace-pre-wrap rounded-2xl rounded-br-md bg-cobalt px-4 py-2.5 text-[15px] leading-relaxed text-white">
          {message.content}
        </div>
      </div>
    );
  }

  const showThinking: boolean = streaming && message.content.length === 0;

  return (
    <div className="flex flex-col gap-3">
      {message.agent !== undefined && (
        <p className="font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">vía {message.agent}</p>
      )}
      {message.sources !== undefined && message.sources.length > 0 && <Sources sources={message.sources} />}
      {showThinking ? (
        <ThinkingDots />
      ) : (
        message.content.length > 0 && (
          <div className="whitespace-pre-wrap text-[15px] leading-relaxed text-night-ink">{message.content}</div>
        )
      )}
      {message.categories !== undefined && message.categories.length > 0 && (
        <CategoryActions categories={message.categories} query={message.query ?? ""} />
      )}
    </div>
  );
}
