export const SOLD_PAGE_SIZE = 24;

export interface SoldState {
  /** `Build` = only builds; `null` = everything (a category narrows to items). */
  kind: "Build" | null;
  /** Category slug. */
  category: string | null;
  page: number;
}

type RawParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined): string | undefined => (Array.isArray(value) ? value[0] : value);

/** `?kind=builds`, `?category=<slug>` and `?page=` → state; a category wins over `kind` (the two never combine). */
export function parseSoldParams(params: RawParams): SoldState {
  const category = first(params.category)?.trim() || null;
  const page = Math.floor(Number(first(params.page)));
  return {
    kind: !category && first(params.kind) === "builds" ? "Build" : null,
    category,
    page: Number.isFinite(page) && page > 1 ? page : 1
  };
}

/** `/sold?…` for a state with `patch` applied (defaults are left out). */
export function soldHref(state: SoldState, patch: Partial<SoldState> = {}): string {
  const next = { ...state, ...patch };
  const query = new URLSearchParams();
  if (next.kind) query.set("kind", "builds");
  if (next.category) query.set("category", next.category);
  if (next.page > 1) query.set("page", String(next.page));
  const text = query.toString();
  return text ? `/sold?${text}` : "/sold";
}

export function soldPageCount(total: number): number {
  return Math.max(1, Math.ceil(total / SOLD_PAGE_SIZE));
}

/** The governorate as the visitor reads it: the name for a known code, else the stored text as it is. */
export function governorateLabel(names: ReadonlyMap<string, string>, value: string | null): string | null {
  const text = value?.trim();
  return text ? (names.get(text.toUpperCase()) ?? text) : null;
}
