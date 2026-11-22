import { getLocale, getTranslations } from "next-intl/server";
import { cardClass } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { HelpLink } from "@/components/ui/HelpLink";
import { Link } from "@/i18n/navigation";
import { categoryPath, type CategoryNode } from "@/lib/categories";
import { localizedName } from "@/lib/localized-name";
import { catalogHref, type CatalogState } from "./catalog-query";

interface CatalogHeaderProps {
  state: CatalogState;
  tree: CategoryNode[];
  category: CategoryNode | undefined;
  title: string;
  counts: { new: number; used: number };
}

/** Breadcrumb, title and the condition segmented control (all / new / used, a count only when it is above zero). */
export async function CatalogHeader({ state, tree, category, title, counts }: CatalogHeaderProps) {
  const t = await getTranslations("catalog");
  const tc = await getTranslations("common.condition");
  const locale = await getLocale();
  const trail = category ? categoryPath(tree, category.id) : [];
  const tabs = [
    { condition: null, label: t("all"), count: counts.new + counts.used },
    { condition: "New", label: tc("New"), count: counts.new },
    { condition: "Used", label: tc("Used"), count: counts.used }
  ] as const;
  const selected = tabs.findIndex((tab) => tab.condition === state.condition);
  const trailItems = trail.map((node) => ({ label: localizedName(node, locale), href: catalogHref(state, { category: node.slug }) }));
  const breadcrumb = [
    { label: t("home"), href: "/" },
    ...trailItems,
    // The category page's title is the category itself; do not repeat it in the trail.
    ...(trailItems.at(-1)?.label === title ? [] : [{ label: title }])
  ];

  return (
    <PageHeader
      breadcrumb={breadcrumb}
      breadcrumbLabel={t("breadcrumb")}
      title={title}
      lead={t("subtitle")}
      below={<HelpLink href="/#recommend">{t("recommendLink")}</HelpLink>}
      actions={
        <div role="group" aria-label={t("conditionGroup")} className={`${cardClass({ layout: "grid", padding: "none", className: "relative grid-cols-3 gap-0 p-1" })}`}>
          <span
            aria-hidden="true"
            style={{ transform: `translateX(calc(var(--dir) * ${Math.max(0, selected)} * 100%))` }}
            className="absolute inset-y-1 start-1 w-[calc((100%-0.5rem)/3)] rounded-control surface-strong transition-transform duration-150 ease-out motion-reduce:transition-none"
          />
          {tabs.map((tab) => {
            const active = state.condition === tab.condition;
            return (
              <Link
                key={tab.label}
                href={catalogHref(state, { condition: tab.condition })}
                aria-current={active ? "true" : undefined}
                className={`relative z-10 flex h-11 items-center justify-center rounded-control px-3 text-sm transition-colors duration-150 md:px-[18px] motion-reduce:transition-none ${active ? "font-bold text-on-carbon" : "hover:bg-paper"}`}
              >
                {tab.label}
                {tab.count > 0 ? <span className={`ms-2 rounded-full px-1.5 font-mono text-xs ${active ? "bg-white/20" : "bg-paper text-ink-muted"}`}>{tab.count}</span> : null}
              </Link>
            );
          })}
        </div>
      }
    />
  );
}
