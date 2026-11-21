import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { USAGES } from "@/lib/usage";
import { CatalogClickTracker } from "@/features/catalog/CatalogClickTracker";
import { BudgetSelect } from "./BudgetSelect";
import { buildsHref, type BuildsState } from "./build-query";

const chip = "flex h-11 items-center rounded-full px-4 text-sm transition-colors duration-150 motion-reduce:transition-none";

/** Usage chips (all + each usage) and the budget select; both live in the URL. */
export async function BuildsFilters({ state }: { state: BuildsState }) {
  const t = await getTranslations("builds");
  const tu = await getTranslations("usage");
  const chips = [{ usage: null, label: t("all") }, ...USAGES.map((usage) => ({ usage, label: tu(usage) }))];

  return (
    <div className="flex flex-wrap items-center gap-x-[18px] gap-y-3">
      <CatalogClickTracker queryKey={JSON.stringify(state)} />
      <div role="group" aria-label={t("usageGroup")} className="flex flex-wrap gap-1.5">
        {chips.map((entry) => {
          const selected = state.usage === entry.usage;
          return (
            <Link
              key={entry.label}
              href={buildsHref(state, { usage: entry.usage })}
              aria-current={selected ? "true" : undefined}
              className={`${chip} ${selected ? "surface-strong" : "border border-input-line bg-surface hover:border-ink"}`}
            >
              {entry.label}
            </Link>
          );
        })}
      </div>
      <BudgetSelect state={state} />
    </div>
  );
}
