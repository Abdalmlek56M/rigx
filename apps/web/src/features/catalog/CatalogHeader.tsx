import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { categoryPath, type CategoryNode } from "@/lib/categories";
import { catalogHref, type CatalogState } from "./catalog-query";

interface CatalogHeaderProps {
  state: CatalogState;
  tree: CategoryNode[];
  category: CategoryNode | undefined;
  title: string;
  counts: { new: number; used: number };
}

/** Breadcrumb, title and the condition segmented control (all / new / used, each with its count). */
export async function CatalogHeader({ state, tree, category, title, counts }: CatalogHeaderProps) {
  const t = await getTranslations("catalog");
  const tc = await getTranslations("common.condition");
  const trail = category ? categoryPath(tree, category.id) : [];
  const tabs = [
    { condition: null, label: t("all"), count: counts.new + counts.used },
    { condition: "New", label: tc("New"), count: counts.new },
    { condition: "Used", label: tc("Used"), count: counts.used }
  ] as const;

  return (
    <section className="flex shell flex-col gap-4 pb-4 pt-6 md:pt-8">
      <nav aria-label={t("breadcrumb")} className="text-sm text-ink-muted">
        <ol className="m-0 flex list-none flex-wrap gap-2 p-0">
          <li><Link href="/" className="hover:underline">{t("home")}</Link></li>
          {trail.map((node, index) => (
            <li key={node.id} className="flex gap-2">
              <span aria-hidden="true">/</span>
              {index === trail.length - 1 ? (
                <span aria-current="page">{node.name}</span>
              ) : (
                <Link href={catalogHref(state, { category: node.slug })} className="hover:underline">{node.name}</Link>
              )}
            </li>
          ))}
        </ol>
      </nav>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-col gap-1.5">
          <h1 className="m-0 text-3xl font-bold md:text-[38px]">{title}</h1>
          <span className="text-[15px] text-ink-muted">
            {t("subtitle")}{" "}
            <Link href="/#recommend" className="text-orange-text underline hover:text-orange-text-hover">{t("recommendLink")}</Link>
          </span>
        </div>
        <div role="group" aria-label={t("conditionGroup")} className="flex gap-1 rounded-card border border-line bg-surface p-1">
          {tabs.map((tab) => {
            const selected = state.condition === tab.condition;
            return (
              <Link
                key={tab.label}
                href={catalogHref(state, { condition: tab.condition })}
                aria-current={selected ? "true" : undefined}
                className={`flex h-11 items-center rounded-control px-3.5 text-sm md:px-[18px] ${selected ? "bg-carbon font-bold text-on-carbon" : ""}`}
              >
                {tab.label} · <span className="font-mono">{tab.count}</span>
              </Link>
            );
          })}
        </div>
      </div>
    </section>
  );
}
