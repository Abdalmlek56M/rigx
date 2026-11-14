import { parseUsage, type Usage } from "../../lib/usage.ts";

/** Budget ranges in EGP (the selects and the home recommend chips share them); the labels live in `home.recommend.budgets`. */
export const BUDGETS = ["low", "mid", "high"] as const;
export type Budget = (typeof BUDGETS)[number];

const RANGES: Record<Budget, { min: number | null; max: number | null }> = {
  low: { min: null, max: 20000 },
  mid: { min: 20000, max: 35000 },
  high: { min: 35000, max: null }
};

export interface BuildsState {
  usage: Usage | null;
  budget: Budget | null;
}

type RawParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined): string | undefined => (Array.isArray(value) ? value[0] : value);

/** Reads `usage` and `budget` from the URL; unknown values are ignored. */
export function parseBuildsParams(params: RawParams): BuildsState {
  const budget = first(params.budget);
  return { usage: parseUsage(first(params.usage)), budget: BUDGETS.find((entry) => entry === budget) ?? null };
}

export function isBudget(value: string): value is Budget {
  return BUDGETS.some((entry) => entry === value);
}

/** The price bounds the API takes (`minPrice` / `maxPrice`); absent bounds are left out. */
export function toApiQuery(state: BuildsState): { usage?: string; minPrice?: number; maxPrice?: number } {
  const range = state.budget ? RANGES[state.budget] : null;
  return {
    ...(state.usage ? { usage: state.usage } : {}),
    ...(range?.min != null ? { minPrice: range.min } : {}),
    ...(range?.max != null ? { maxPrice: range.max } : {})
  };
}

/** `/builds?usage=…&budget=…` for a state with `patch` applied (defaults are left out of the URL). */
export function buildsHref(state: BuildsState, patch: Partial<BuildsState> = {}): string {
  const next = { ...state, ...patch };
  const query = new URLSearchParams();
  if (next.usage) query.set("usage", next.usage);
  if (next.budget) query.set("budget", next.budget);
  const text = query.toString();
  return text ? `/builds?${text}` : "/builds";
}
