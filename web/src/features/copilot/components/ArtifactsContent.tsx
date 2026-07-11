import type { ReactElement } from "react";
import { useCopilotStore, type Artifact, type ChartArtifact, type TableArtifact } from "../state/useCopilotStore.ts";

function TableView({ artifact }: { readonly artifact: TableArtifact }): ReactElement {
  // Keys estables por contenido/posición (no el índice del map): celda por su columna, fila por su nº.
  const rows = artifact.rows.map((cells, rowIndex) => ({
    key: `row-${rowIndex}`,
    cells: cells.map((value, cellIndex) => ({ key: artifact.columns[cellIndex] ?? `col-${cellIndex}`, value })),
  }));

  return (
    <section className="rounded-xl border border-night-line bg-night-3">
      <p className="border-b border-night-line px-4 py-2.5 font-medium text-night-ink">{artifact.title}</p>
      <div className="max-h-[50vh] overflow-auto">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr>
              {artifact.columns.map((column) => (
                <th
                  key={column}
                  className="sticky top-0 border-b border-night-line bg-night-2 px-3 py-2 text-left font-mono text-xs uppercase tracking-wider text-night-muted"
                >
                  {column}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.key} className="border-b border-night-line/60">
                {row.cells.map((cell) => (
                  <td key={cell.key} className="px-3 py-1.5 text-night-soft">{cell.value}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function ChartView({ artifact }: { readonly artifact: ChartArtifact }): ReactElement {
  const xIndex: number = artifact.columns.findIndex((column) => column.toLowerCase() === artifact.xColumn.toLowerCase());
  const yIndex: number = artifact.columns.findIndex((column) => column.toLowerCase() === artifact.yColumn.toLowerCase());
  if (xIndex < 0 || yIndex < 0) {
    return (
      <section className="rounded-xl border border-night-line bg-night-3 px-4 py-3 text-sm text-night-muted">
        {artifact.title}: no se pudo dibujar el gráfico.
      </section>
    );
  }

  const points = artifact.rows.slice(0, 20).map((row, index) => ({
    key: `pt-${index}`,
    label: row[xIndex] ?? "",
    value: Number.parseFloat((row[yIndex] ?? "").replace(",", ".")) || 0,
  }));
  const max: number = Math.max(1, ...points.map((point) => point.value));
  const width = 560;
  const height = 260;
  const pad = 34;
  const chartWidth = width - pad * 2;
  const chartHeight = height - pad * 2;
  const step: number = points.length > 0 ? chartWidth / points.length : chartWidth;
  const yOf = (value: number): number => pad + chartHeight - (value / max) * chartHeight;

  return (
    <section className="rounded-xl border border-night-line bg-night-3">
      <p className="border-b border-night-line px-4 py-2.5 font-medium text-night-ink">
        {artifact.title} · {artifact.yColumn} por {artifact.xColumn}
      </p>
      <div className="overflow-x-auto p-3">
        <svg viewBox={`0 0 ${width} ${height}`} className="h-auto w-full min-w-[480px]" role="img" aria-label={artifact.title}>
          <line x1={pad} y1={pad} x2={pad} y2={pad + chartHeight} stroke="currentColor" className="text-night-line" />
          <line x1={pad} y1={pad + chartHeight} x2={pad + chartWidth} y2={pad + chartHeight} stroke="currentColor" className="text-night-line" />
          <text x={pad - 6} y={pad + 4} textAnchor="end" className="fill-night-muted text-[10px] font-mono">
            {max.toLocaleString("es-CO")}
          </text>

          {artifact.type === "line" ? (
            <polyline
              fill="none"
              stroke="currentColor"
              strokeWidth={2}
              className="text-sky"
              points={points.map((point, index) => `${pad + step * index + step / 2},${yOf(point.value)}`).join(" ")}
            />
          ) : (
            points.map((point, index) => (
              <rect
                key={point.key}
                x={pad + step * index + step * 0.15}
                y={yOf(point.value)}
                width={step * 0.7}
                height={pad + chartHeight - yOf(point.value)}
                className="fill-sky"
              />
            ))
          )}

          {points.map((point, index) => (
            <text
              key={point.key}
              x={pad + step * index + step / 2}
              y={height - 12}
              textAnchor="middle"
              className="fill-night-muted text-[9px]"
            >
              {point.label.length > 10 ? `${point.label.slice(0, 9)}…` : point.label}
            </text>
          ))}
        </svg>
      </div>
    </section>
  );
}

/** Contenido del panel de artefactos (estilo Claude): tablas y gráficos generados en la conversación. */
export function ArtifactsContent(): ReactElement {
  const artifacts: ReadonlyArray<Artifact> = useCopilotStore((state) => state.artifacts);

  if (artifacts.length === 0) {
    return <p className="text-sm text-night-muted">Aún no hay artefactos. Pide una tabla o un gráfico y aparecerán aquí.</p>;
  }

  return (
    <div className="space-y-5">
      {[...artifacts].reverse().map((artifact) =>
        artifact.kind === "table" ? (
          <TableView key={artifact.id} artifact={artifact} />
        ) : (
          <ChartView key={artifact.id} artifact={artifact} />
        ),
      )}
    </div>
  );
}
