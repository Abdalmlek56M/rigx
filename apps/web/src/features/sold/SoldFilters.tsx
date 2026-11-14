import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import type { CategoryNode } from "@/lib/categories";
import { soldHref, type SoldState } from "./sold-query";

const MAX_CATEGORY_CHIPS = 6;

/** Filter chips: all, builds, and the store's top categories; one is on at a time. */
export async function SoldFilters({ state, tree }: { state: SoldState; tree: CategoryNode[] }) {
  const t = await getTranslations("sold");
  const chips = [
    { label: t("all"), selected: !state.kind && !state.category, href: soldHref(state, { kind: null, category: null, page: 1 }) },
    { label: t("builds"), selected: state.kind === "Build", href: soldHref(state, { kind: "Build", category: null, page: 1 }) },
    ...tree.slice(0, MAX_CATEGORY_CHIPS).map((node) => ({
      label: node.name,
      selected: state.category === node.slug,
      href: soldHref(state, { kind: null, category: node.slug, page: 1 })
    }))
  ];

  return (
    <div role="group" aria-label={t("filter")} className="flex flex-wrap gap-1 self-start rounded-card border border-line bg-surface p-1">
      {chips.map((chip) => (
        <Link
          key={chip.href}
          href={chip.href}
          aria-current={chip.selected ? "true" : undefined}
          className={`flex h-11 items-center rounded-control px-3.5 text-[13px] whitespace-nowrap ${chip.selected ? "bg-carbon font-bold text-on-carbon" : ""}`}
        >
          {chip.label}
        </Link>
      ))}
    </div>
  );
}
