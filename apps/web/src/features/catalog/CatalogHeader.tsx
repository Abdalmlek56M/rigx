import { getLocale, getTranslations } from "next-intl/server";
import { PageHeader } from "@/components/ui/PageHeader";
import { HelpLink } from "@/components/ui/HelpLink";
import { categoryPath, type CategoryNode } from "@/lib/categories";
import { localizedName } from "@/lib/localized-name";
import { catalogHref, type CatalogState } from "./catalog-query";
import { ChoiceLinkTile } from "@/components/ui/ChoiceTile";
import { conditionIcon } from "@/components/ui/condition-icon";

interface CatalogHeaderProps {
  state: CatalogState;
  tree: CategoryNode[];
  category: CategoryNode | undefined;
  title: string;
  counts: { new: number; used: number };
}

/** Breadcrumb, title and the condition choice tiles (all / new / used, a count only when it is above zero). */
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
  const trailItems = trail.map((node) => ({ label: localizedName(node, locale), href: catalogHref(state, { category: node.slug }) }));
  const breadcrumb = [
    { label: t("home"), href: "/" },
    ...trailItems,
    // The category page's title is the category itself; do not repeat it in the trail.
    ...(trailItems.at(-1)?.label === title ? [] : [{ label: title }])
  ];

  return (
    <PageHeader atmosphere
      breadcrumb={breadcrumb}
      breadcrumbLabel={t("breadcrumb")}
      title={title}
      lead={t("subtitle")}
      below={<HelpLink href="/#recommend">{t("recommendLink")}</HelpLink>}
      actions={
        <div role="group" aria-label={t("conditionGroup")} className="flex flex-wrap gap-1.5">
          {tabs.map((tab) => (
            <ChoiceLinkTile
              key={tab.label}
              href={catalogHref(state, { condition: tab.condition })}
              icon={conditionIcon(tab.condition)}
              label={tab.label}
              badge={tab.count > 0 ? <span className="rounded-full bg-paper px-1.5 font-mono text-xs font-normal text-ink-muted">{tab.count}</span> : undefined}
              selected={state.condition === tab.condition}
              compact
              className="flex-auto md:min-w-24"
            />
          ))}
        </div>
      }
    />
  );
}
