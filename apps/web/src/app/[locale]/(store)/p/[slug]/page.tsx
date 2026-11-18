import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { FaqList } from "@/components/ui/FaqList";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { categoryHref } from "@/components/layout/nav-links";
import { Gallery } from "@/features/product/Gallery";
import { ProductInfoBoxes } from "@/features/product/ProductInfoBoxes";
import { PurchasePanel } from "@/features/product/PurchasePanel";
import { SpecsTable } from "@/features/product/SpecsTable";
import { loadItem } from "@/features/product/load-item";
import { specRows } from "@/features/product/spec-rows";
import { JsonLd } from "@/components/seo/JsonLd";
import { Link, permanentRedirect } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";
import { categoryPath, effectiveSpecs } from "@/lib/categories";
import { parseCondition } from "@/lib/condition";
import { siteUrl, whatsAppNumber } from "@/lib/contact";
import { mediaUrl } from "@/lib/media";
import { openGraphBase, organizationId, pageSeo } from "@/lib/seo/page-seo";
import { breadcrumbJsonLd, productJsonLd } from "@/lib/seo/structured-data";
import { absoluteUrl, localizedPath, productPath } from "@/lib/seo/urls";
import { loadItemFaqs } from "@/lib/faqs";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string; slug: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale, slug } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  setRequestLocale(locale);
  const item = await loadItem(slug);
  if (!item) return {};
  const t = await getTranslations({ locale, namespace: "common.condition" });
  const condition = parseCondition(item.condition);
  const title = condition ? `${item.modelName} · ${t(condition)}` : item.modelName;
  const photo = item.photos[0];
  return {
    title,
    ...(await pageSeo(locale, productPath(item.slug))),
    openGraph: { ...openGraphBase(locale), title, url: localizedPath(locale, productPath(item.slug)), ...(photo ? { images: [{ url: mediaUrl(photo.storageKey), alt: item.modelName }] } : {}) }
  };
}

export default async function ProductPage({ params }: Props) {
  const { locale, slug } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const item = await loadItem(slug);
  const condition = parseCondition(item?.condition);
  if (!item || !condition) notFound();
  // Upper case or an RX code in the URL: send it to the one canonical address.
  if (slug !== item.slug) permanentRedirect({ href: productPath(item.slug), locale });

  const t = await getTranslations("product");
  const tc = await getTranslations("common.condition");
  const [storefront, faqs] = await Promise.all([loadStorefront(), loadItemFaqs(item.code)]);
  const trail = item.categoryId ? categoryPath(storefront.categoryTree, item.categoryId) : [];
  const defs = item.categoryId ? effectiveSpecs(storefront.categoryTree, item.categoryId) : [];
  const soldOut = item.available <= 0;
  const url = absoluteUrl(localizedPath(locale, productPath(item.slug)));
  const structuredData = [
    productJsonLd({
      name: item.modelName, url, images: item.photos.map((photo) => absoluteUrl(mediaUrl(photo.storageKey))), price: item.prices.alone,
      available: !soldOut, condition, sku: item.code, category: trail.at(-1)?.name, organizationId: organizationId()
    }),
    breadcrumbJsonLd([
      { name: t("home"), url: absoluteUrl(localizedPath(locale, "/")) },
      ...trail.map((node) => ({ name: node.name, url: absoluteUrl(localizedPath(locale, categoryHref(node.slug))) })),
      { name: item.modelName }
    ])
  ];
  const availability = soldOut ? t("soldOut") : condition === "Used" ? t("availableUsed") : t("availableNew", { count: item.available });

  return (
    <div className="pb-28 md:pb-0">
      <JsonLd data={structuredData} />
      <nav aria-label={t("breadcrumb")} className="mx-auto max-w-[1240px] px-4 pt-6 text-sm text-ink-muted md:px-6">
        <ol className="m-0 flex list-none flex-wrap gap-2 p-0">
          <li><Link href="/" className="hover:underline">{t("home")}</Link></li>
          {trail.map((node) => (
            <li key={node.id} className="flex gap-2">
              <span aria-hidden="true">/</span>
              <Link href={categoryHref(node.slug)} className="hover:underline">{node.name}</Link>
            </li>
          ))}
          <li className="flex gap-2">
            <span aria-hidden="true">/</span>
            <span aria-current="page">{tc(condition)}</span>
          </li>
        </ol>
      </nav>

      <section className="mx-auto grid max-w-[1240px] items-start gap-6 px-4 py-6 md:grid-cols-2 md:gap-10 md:px-6 md:py-9">
        <Gallery photos={item.photos} alt={item.modelName} />
        <div className="flex min-w-0 flex-col gap-4">
          <div className="flex flex-wrap items-center gap-2.5">
            <ConditionBadge condition={condition} />
            <span dir="ltr" className="font-mono text-[13px] text-ink-muted">{item.code}</span>
            <span className={`text-[13px] font-bold ${soldOut ? "text-ink-muted" : "text-success"}`}>{availability}</span>
          </div>
          <h1 dir="ltr" className="m-0 text-start text-2xl leading-[1.3] font-bold [unicode-bidi:isolate] md:text-[32px]">{item.modelName}</h1>
          <PurchasePanel
            item={{
              code: item.code,
              modelName: item.modelName,
              condition,
              warrantyLabel: item.warrantyLabel,
              thumbnailStorageKey: item.photos[0]?.thumbnailStorageKey ?? null,
              available: item.available,
              prices: item.prices
            }}
            whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.product }}
            pageUrl={siteUrl(storefront.domain, productPath(item.slug))}
          />
          <ProductInfoBoxes warrantyLabel={item.warrantyLabel} storefront={storefront} />
        </div>
      </section>

      <div className="mx-auto flex max-w-[1240px] flex-col gap-10 px-4 pb-14 md:px-6">
        <SpecsTable rows={specRows(item.specs, defs)} />
        {faqs.length > 0 ? (
          <section className="flex flex-col gap-3">
            <h2 className="m-0 text-2xl font-bold">{t("faqs")}</h2>
            <FaqList entries={faqs} />
          </section>
        ) : null}
      </div>
    </div>
  );
}
