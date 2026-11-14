import type { ReactNode } from "react";
import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { categoryBySlug, effectiveSpecs, type CategoryNode } from "@/lib/categories";
import { activeFilters, catalogHref, type CatalogState, type SortKey } from "./catalog-query";
import { FiltersDrawer } from "./FiltersDrawer";
import { SortSelect } from "./SortSelect";

const SORT_KEYS: readonly SortKey[] = ["newest", "price_asc", "price_desc"];

interface CatalogToolbarProps {
  state: CatalogState;
  tree: CategoryNode[];
  total: number;
  /** The filter panel, shown inside the mobile drawer. */
  panel: ReactNode;
  queryKey: string;
}

/** Above the grid: the mobile filter button, removable chips of the active filters, the result count and the sort select. */
export async function CatalogToolbar({ state, tree, total, panel, queryKey }: CatalogToolbarProps) {
  const t = await getTranslations("catalog");
  const tc = await getTranslations("common.condition");
  const filters = activeFilters(state);

  function label(filter: ReturnType<typeof activeFilters>[number]): string {
    switch (filter.kind) {
      case "search":
        return t("chips.search", { query: filter.value });
      case "category":
        return categoryBySlug(tree, filter.value)?.name ?? filter.value;
      case "condition":
        return tc(filter.value === "New" ? "New" : "Used");
      case "stock":
        return t(filter.value === "out" ? "chips.stockOut" : "chips.stockAll");
      case "price":
        return state.min !== null && state.max !== null
          ? t("chips.priceBetween", { min: state.min, max: state.max })
          : state.min !== null
            ? t("chips.priceFrom", { min: state.min })
            : t("chips.priceTo", { max: state.max ?? 0 });
      case "spec": {
        const category = state.category ? categoryBySlug(tree, state.category) : undefined;
        const specs = category ? effectiveSpecs(tree, category.id) : [];
        const name = specs.find((s) => s.key === filter.specKey)?.name ?? filter.specKey ?? "";
        return `${name}: ${filter.value}`;
      }
    }
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <FiltersDrawer queryKey={queryKey} activeCount={filters.length}>{panel}</FiltersDrawer>
        <ul className="m-0 flex list-none flex-wrap gap-1.5 p-0 text-[13px]">
          {filters.map((filter) => {
            const text = label(filter);
            return (
              <li key={filter.id}>
                <Link
                  href={catalogHref(state, filter.clear)}
                  aria-label={t("chips.remove", { label: text })}
                  className="flex min-h-9 items-center gap-1.5 rounded-full bg-carbon px-3 text-on-carbon"
                >
                  <span dir="auto" className="[unicode-bidi:isolate]">{text}</span>
                  <span aria-hidden="true">✕</span>
                </Link>
              </li>
            );
          })}
        </ul>
      </div>
      <div className="flex items-center gap-4">
        <span className="text-sm text-ink-muted">{t("resultCount", { count: total })}</span>
        <SortSelect
          current={state.sort}
          options={SORT_KEYS.map((key) => ({ value: key, label: t(`sort.${key}`), href: catalogHref(state, { sort: key }) }))}
        />
      </div>
    </div>
  );
}
