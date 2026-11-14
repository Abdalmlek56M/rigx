import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Pagination } from "@/features/catalog/Pagination";
import { SoldCard } from "@/features/sold/SoldCard";
import { SoldFilters } from "@/features/sold/SoldFilters";
import { loadGovernorateNames, loadSold } from "@/features/sold/load-sold";
import { governorateLabel, parseSoldParams, soldHref, soldPageCount } from "@/features/sold/sold-query";
import { routing } from "@/i18n/routing";
import { categoryBySlug } from "@/lib/categories";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

export async function generateMetadata({ params }: Pick<Props, "params">): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "sold" });
  return { title: t("title") };
}

export default async function SoldPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("sold");
  const storefront = await loadStorefront();
  const state = parseSoldParams(await searchParams);
  const category = state.category ? categoryBySlug(storefront.categoryTree, state.category) : undefined;
  const [sold, names] = await Promise.all([
    loadSold({ ...(state.kind ? { kind: state.kind } : {}), ...(category ? { categoryId: category.id } : {}), page: state.page }),
    loadGovernorateNames()
  ]);
  if (sold.status === "off") notFound();

  return (
    <section className="mx-auto flex max-w-[1240px] flex-col gap-5 px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <div className="flex flex-col gap-1.5">
        <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
        <span className="text-[15px] text-ink-muted">{t("subtitle")}</span>
      </div>
      <SoldFilters state={state} tree={storefront.categoryTree} />
      {sold.status === "failed" ? (
        <p role="alert" className="rounded-card border border-line bg-surface p-6 text-center">{t("loadFailed")}</p>
      ) : sold.page.items.length === 0 ? (
        <p role="status" className="rounded-card border border-line bg-surface p-10 text-center text-ink-muted">{t("empty")}</p>
      ) : (
        <ul className="m-0 grid list-none grid-cols-2 gap-3.5 p-0 md:grid-cols-3 lg:grid-cols-4">
          {sold.page.items.map((item) => (
            <li key={`${item.kind}-${item.code}-${item.soldAtUtc}`} className="flex">
              <SoldCard item={item} governorate={governorateLabel(names, item.governorate)} />
            </li>
          ))}
        </ul>
      )}
      {sold.status === "ok" ? (
        <Pagination current={state.page} pages={soldPageCount(sold.page.total)} hrefFor={(page) => soldHref(state, { page })} />
      ) : null}
    </section>
  );
}
