import type { ReactElement } from "react";
import { useCopilotStore, type AuditEntry, type AuditInteraction } from "../state/useCopilotStore.ts";

/** Una interacción cruda con un agente: nodo colapsable con el mensaje enviado y la respuesta. */
function InteractionNode({ interaction }: { readonly interaction: AuditInteraction }): ReactElement {
  return (
    <details className="group rounded-lg border border-night-line bg-night-2">
      <summary className="flex cursor-pointer list-none items-center gap-2 px-3 py-2">
        <span className="text-night-muted transition group-open:rotate-90">▸</span>
        <span className="font-mono text-xs uppercase tracking-wider text-sky">{interaction.agent}</span>
      </summary>
      <div className="space-y-2 px-3 pb-3 pt-1">
        <div>
          <p className="font-mono text-[10px] uppercase tracking-[0.18em] text-night-muted">Enviado</p>
          <pre className="mt-1 max-h-56 overflow-auto whitespace-pre-wrap break-words rounded bg-night-3 p-2 text-xs leading-relaxed text-night-soft">
            {interaction.request}
          </pre>
        </div>
        <div>
          <p className="font-mono text-[10px] uppercase tracking-[0.18em] text-night-muted">Respuesta</p>
          <pre className="mt-1 max-h-56 overflow-auto whitespace-pre-wrap break-words rounded bg-night-3 p-2 text-xs leading-relaxed text-night-soft">
            {interaction.response}
          </pre>
        </div>
      </div>
    </details>
  );
}

/** Tarjeta de un turno: inicia con el mensaje del usuario y despliega el árbol de interacciones. */
function AuditCard({ entry }: { readonly entry: AuditEntry }): ReactElement {
  return (
    <section className="rounded-xl border border-night-line bg-night-3">
      <div className="border-b border-night-line px-4 py-3">
        <p className="font-mono text-[11px] uppercase tracking-[0.18em] text-night-muted">Usuario</p>
        <p className="mt-1 text-sm text-night-ink">{entry.userMessage}</p>
      </div>
      {entry.interactions.length === 0 ? (
        <p className="px-4 py-3 text-sm text-night-muted">Sin interacciones registradas para este turno.</p>
      ) : (
        <div className="space-y-1.5 p-2.5">
          {entry.interactions.map((interaction, index) => (
            <InteractionNode key={index} interaction={interaction} />
          ))}
        </div>
      )}
    </section>
  );
}

/**
 * Contenido del panel de auditoría: una tarjeta por turno (mensaje del usuario) y, como árbol,
 * cada interacción cruda con los agentes (mensaje enviado / respuesta). Sirve para afinar los agentes.
 */
export function AuditContent(): ReactElement {
  const auditLog: ReadonlyArray<AuditEntry> = useCopilotStore((state) => state.auditLog);

  if (auditLog.length === 0) {
    return (
      <p className="text-sm text-night-muted">
        Aún no hay interacciones. Envía un mensaje y aquí verás, en crudo, lo que se intercambió con cada agente.
      </p>
    );
  }

  return (
    <div className="space-y-4">
      {[...auditLog].reverse().map((entry) => (
        <AuditCard key={entry.id} entry={entry} />
      ))}
    </div>
  );
}
