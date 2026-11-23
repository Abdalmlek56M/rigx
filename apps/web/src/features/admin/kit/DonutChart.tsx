import { donutArcs, type DonutSlice } from "./donut-rules";

const SIZE = 160;
const STROKE = 26;
const RADIUS = (SIZE - STROKE) / 2;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

/**
 * A ring split into slices, drawn with SVG only (no chart library). The slice colours are page tokens, a small gap in the page colour
 * separates neighbours, and the middle carries the total. The picture is decorative: the legend next to it holds the same figures as text.
 */
export function DonutChart({ slices, centerTop, centerBottom }: { slices: readonly DonutSlice[]; centerTop: string; centerBottom: string }) {
  const arcs = donutArcs(slices, CIRCUMFERENCE);
  return (
    <svg viewBox={`0 0 ${SIZE} ${SIZE}`} aria-hidden="true" className="size-40 shrink-0">
      <circle cx={SIZE / 2} cy={SIZE / 2} r={RADIUS} fill="none" stroke="var(--line)" strokeWidth={STROKE} />
      <g transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}>
        {slices.map((slice, index) => (
          <circle
            key={slice.key} cx={SIZE / 2} cy={SIZE / 2} r={RADIUS} fill="none" stroke={slice.color} strokeWidth={STROKE}
            strokeDasharray={`${arcs[index]?.length ?? 0} ${CIRCUMFERENCE}`} strokeDashoffset={arcs[index]?.offset ?? 0}
          />
        ))}
      </g>
      <text x={SIZE / 2} y={SIZE / 2 - 4} textAnchor="middle" className="fill-ink-muted text-[10px]">{centerTop}</text>
      <text x={SIZE / 2} y={SIZE / 2 + 14} textAnchor="middle" className="fill-ink text-[14px] font-bold">{centerBottom}</text>
    </svg>
  );
}
