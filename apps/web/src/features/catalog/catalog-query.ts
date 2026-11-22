import { parseCondition, type Condition } from "../../lib/condition.ts";

/** "all" lists sold-out items too (after the available ones, marked "خلص"); "in" hides them. */
export type StockFilter = "all" | "in";
export type SortKey = "newest" | "price_asc" | "price_desc";
/** How the results are laid out: cards in a grid (default) or one row per item, for scanning prices. */
export type ViewMode = "grid" | "list";

export const PAGE_SIZE = 24;
const SORTS: readonly SortKey[] = ["newest", "price_asc", "price_desc"];
const SPEC_PREFIX = "spec.";

/** Everything the catalog URL says. Defaults (sold-out included, newest first, page 1) are left out of the URL. */
export interface CatalogState {
  search: string;
  /** Category slug. */
  category: string | null;
  /** Brand slug. */
  brand: string | null;
  condition: Condition | null;
  stock: StockFilter;
  min: number | null;
  max: number | null;
  sort: SortKey;
  view: ViewMode;
  page: number;
  /** One value per spec key (only meaningful with a category). */
  specs: Record<string, string>;
}

export type CatalogPatch = Partial<CatalogState>;
export type RawParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined): string | undefined => (Array.isArray(value) ? value[0] : value);

function parseAmount(value: string | undefined): number | null {
  const amount = value === undefined || value.trim() === "" ? NaN : Number(value);
  return Number.isFinite(amount) && amount >= 0 ? amount : null;
}

/** Reads the URL. */
export function parseCatalogParams(params: RawParams): CatalogState {
  const stock = first(params.stock);
  const sort = first(params.sort);
  const page = Math.floor(Number(first(params.page)));
  const specs: Record<string, string> = {};
  for (const [name, raw] of Object.entries(params)) {
    const value = first(raw)?.trim();
    if (name.startsWith(SPEC_PREFIX) && name.length > SPEC_PREFIX.length && value) specs[name.slice(SPEC_PREFIX.length)] = value;
  }
  return {
    search: first(params.search)?.trim() ?? "",
    category: first(params.category)?.trim() || null,
    brand: first(params.brand)?.trim().toLowerCase() || null,
    condition: parseCondition(first(params.condition)),
    stock: stock === "in" ? "in" : "all",
    min: parseAmount(first(params.min)),
    max: parseAmount(first(params.max)),
    sort: SORTS.find((key) => key === sort) ?? "newest",
    view: first(params.view) === "list" ? "list" : "grid",
    page: page >= 1 ? page : 1,
    specs
  };
}

/** The URL query for a state (defaults omitted, stable order). */
export function toSearchParams(state: CatalogState): URLSearchParams {
  const query = new URLSearchParams();
  if (state.search) query.set("search", state.search);
  if (state.category) query.set("category", state.category);
  if (state.brand) query.set("brand", state.brand);
  if (state.condition) query.set("condition", state.condition);
  if (state.stock === "in") query.set("stock", "in");
  if (state.min !== null) query.set("min", String(state.min));
  if (state.max !== null) query.set("max", String(state.max));
  if (state.sort !== "newest") query.set("sort", state.sort);
  if (state.view === "list") query.set("view", "list");
  if (state.page > 1) query.set("page", String(state.page));
  if (state.category) for (const [key, value] of Object.entries(state.specs)) query.set(`${SPEC_PREFIX}${key}`, value);
  return query;
}

/**
 * The state after a change. Any change goes back to page 1 (except switching the view, which keeps the page); changing the category
 * drops the spec filters (they belong to the old category) unless the patch brings its own.
 */
export function withPatch(state: CatalogState, patch: CatalogPatch): CatalogState {
  const onlyView = Object.keys(patch).length === 1 && patch.view !== undefined;
  const next = { ...state, page: onlyView ? state.page : 1, ...patch };
  if (patch.category !== undefined && patch.specs === undefined) next.specs = {};
  return next;
}

/** `/products?...` for a state after `patch` (the locale prefix is added by the router `Link`). */
export function catalogHref(state: CatalogState, patch: CatalogPatch = {}): string {
  const query = toSearchParams(withPatch(state, patch)).toString();
  return query ? `/products?${query}` : "/products";
}

/** The canonical catalog address of a state: only the category and the page; filters, sort, search and the view are views of it. */
export function catalogCanonicalPath(state: CatalogState): string {
  const query = new URLSearchParams();
  if (state.category) query.set("category", state.category);
  if (state.page > 1) query.set("page", String(state.page));
  const text = query.toString();
  return text ? `/products?${text}` : "/products";
}

export interface ApiItemsQuery {
  search?: string;
  categoryId?: string;
  brandId?: string;
  condition?: Condition;
  inStock?: boolean;
  minPrice?: number;
  maxPrice?: number;
  sort: SortKey;
  page: number;
  pageSize: number;
  [spec: `spec.${string}`]: string;
}

/** The ids of the state's category and brand (null = none or unknown). */
export interface CatalogIds {
  categoryId: string | null;
  brandId: string | null;
}

/**
 * The `/api/v1/store/items` query for a state and the ids of its category and brand.
 * With sold-out items hidden by the owner (`showSoldOut` off) only available items are asked for.
 */
export function toApiQuery(state: CatalogState, { categoryId, brandId }: CatalogIds, showSoldOut = true): ApiItemsQuery {
  const query: ApiItemsQuery = { sort: state.sort, page: state.page, pageSize: PAGE_SIZE };
  if (state.search) query.search = state.search;
  if (brandId) query.brandId = brandId;
  if (categoryId) {
    query.categoryId = categoryId;
    for (const [key, value] of Object.entries(state.specs)) query[`spec.${key}`] = value;
  }
  if (state.condition) query.condition = state.condition;
  if (state.stock === "in" || !showSoldOut) query.inStock = true;
  if (state.min !== null) query.minPrice = state.min;
  if (state.max !== null) query.maxPrice = state.max;
  return query;
}

export interface ActiveFilter {
  id: string;
  kind: "search" | "category" | "brand" | "condition" | "stock" | "price" | "spec";
  /** For `spec`: the spec key (its value is in `value`). */
  specKey?: string;
  value: string;
  /** What to change to remove this filter. */
  clear: CatalogPatch;
}

/** The filters that are on, as removable chips (the default availability "all" is not a chip). */
export function activeFilters(state: CatalogState): ActiveFilter[] {
  const chips: ActiveFilter[] = [];
  if (state.search) chips.push({ id: "search", kind: "search", value: state.search, clear: { search: "" } });
  if (state.category) chips.push({ id: "category", kind: "category", value: state.category, clear: { category: null } });
  if (state.brand) chips.push({ id: "brand", kind: "brand", value: state.brand, clear: { brand: null } });
  if (state.condition) chips.push({ id: "condition", kind: "condition", value: state.condition, clear: { condition: null } });
  if (state.stock === "in") chips.push({ id: "stock", kind: "stock", value: state.stock, clear: { stock: "all" } });
  if (state.min !== null || state.max !== null) {
    chips.push({ id: "price", kind: "price", value: `${state.min ?? ""}–${state.max ?? ""}`, clear: { min: null, max: null } });
  }
  if (state.category) {
    for (const [key, value] of Object.entries(state.specs)) {
      const rest = Object.fromEntries(Object.entries(state.specs).filter(([name]) => name !== key));
      chips.push({ id: `spec.${key}`, kind: "spec", specKey: key, value, clear: { specs: rest } });
    }
  }
  return chips;
}

export function pageCount(total: number): number {
  return Math.max(1, Math.ceil(total / PAGE_SIZE));
}

/** Page numbers to show: first, last and the neighbours of the current page; `"gap"` stands for skipped pages. */
export function pageWindow(current: number, total: number): (number | "gap")[] {
  const wanted = new Set([1, total, current - 1, current, current + 1].filter((page) => page >= 1 && page <= total));
  const pages = [...wanted].sort((a, b) => a - b);
  return pages.flatMap((page, index) => (index > 0 && page - (pages[index - 1] ?? 0) > 1 ? ["gap" as const, page] : [page]));
}
