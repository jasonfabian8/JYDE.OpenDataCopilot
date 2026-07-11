import { useEffect, useRef, type ReactElement } from "react";
import { useCopilotStore, type Conversation, type CopilotMessage } from "../state/useCopilotStore.ts";
import { Message } from "./Message.tsx";

/** Hilo desplazable de mensajes de la conversación activa, con burbuja de streaming al final. */
export function MessageList(): ReactElement {
  const conversations: ReadonlyArray<Conversation> = useCopilotStore((state) => state.conversations);
  const activeId: string = useCopilotStore((state) => state.activeId);
  const status: string = useCopilotStore((state) => state.status);
  const agent: string | null = useCopilotStore((state) => state.agent);
  const streamingAnswer: string = useCopilotStore((state) => state.streamingAnswer);
  const streamingSources = useCopilotStore((state) => state.streamingSources);

  const active: Conversation | undefined = conversations.find((c) => c.id === activeId);
  const messages: ReadonlyArray<CopilotMessage> = active?.messages ?? [];
  const isStreaming: boolean = status === "streaming";

  const bottomRef = useRef<HTMLDivElement>(null);
  useEffect((): void => {
    bottomRef.current?.scrollIntoView?.({ behavior: "smooth", block: "end" });
  }, [messages.length, streamingAnswer, isStreaming]);

  return (
    <div className="flex-1 overflow-y-auto px-4">
      <div className="mx-auto flex max-w-3xl flex-col gap-6 py-8">
        {messages.map((message) => (
          <Message key={message.id} message={message} />
        ))}

        {isStreaming && (
          <Message
            message={{
              id: "streaming",
              role: "assistant",
              content: streamingAnswer,
              agent: agent ?? undefined,
              sources: streamingSources ?? undefined,
            }}
            streaming
          />
        )}

        <div ref={bottomRef} />
      </div>
    </div>
  );
}
