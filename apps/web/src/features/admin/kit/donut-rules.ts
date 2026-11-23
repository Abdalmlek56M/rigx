/** The pure part of the admin's donut chart: which colour a slice takes, how rows beyond the palette are grouped, and where each arc starts. Tested. */

/** Colours of the slices, all page tokens (they hold in the light and the dark theme); the last one is kept for "other". */
const QUIET_COLOR = "var(--muted)";
export const DONUT_COLORS = ["var(--orange)", "var(--glow-blue)", "var(--success)", "var(--warning)", "var(--error)", "var(--ink)", QUIET_COLOR] as const;

export interface DonutRow {
  key: string;
  label: string;
  value: number;
}

export interface DonutSlice extends DonutRow {
  color: string;
  /** Share of the total, one decimal. */
  percent: number;
}

/**
 * The rows largest first, each with its colour and share. When there are more rows than colours, the smallest ones are added into one
 * last slice named `otherLabel` (it takes the quiet colour). Rows with nothing in them are left out.
 */
export function donutSlices(rows: readonly DonutRow[], otherLabel: string): DonutSlice[] {
  const sorted = rows.filter((row) => row.value > 0).sort((a, b) => b.value - a.value);
  const room = DONUT_COLORS.length;
  const shown: DonutRow[] = sorted.length <= room
    ? sorted
    : [...sorted.slice(0, room - 1), { key: "other", label: otherLabel, value: sorted.slice(room - 1).reduce((sum, row) => sum + row.value, 0) }];
  const total = shown.reduce((sum, row) => sum + row.value, 0);
  return shown.map((row, index) => ({
    ...row,
    color: row.key === "other" && shown.length === room ? QUIET_COLOR : DONUT_COLORS[index] ?? QUIET_COLOR,
    percent: total > 0 ? Math.round((row.value / total) * 1000) / 10 : 0
  }));
}

export interface DonutArc {
  /** Length of the visible dash, in the same unit as the circumference. */
  length: number;
  /** Where it starts along the circle (a negative dash offset). */
  offset: number;
}

/** The dash of each slice on a circle of the given circumference, with a small gap between neighbours (none for a single slice). */
export function donutArcs(slices: readonly { value: number }[], circumference: number, gap = 2): DonutArc[] {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);
  if (total <= 0) return [];
  const space = slices.length > 1 ? gap : 0;
  let start = 0;
  return slices.map((slice) => {
    const span = (slice.value / total) * circumference;
    const arc = { length: Math.max(span - space, 0), offset: start === 0 ? 0 : -start };
    start += span;
    return arc;
  });
}
