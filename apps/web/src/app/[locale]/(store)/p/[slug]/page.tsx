import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { FaqList } from "@/components/ui/FaqList";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { HelpIcon } from "@/components/ui/icons";
import { categoryHref } from "@/components/layout/nav-links";
import { Gallery } from "@/features/product/Gallery";
import { ProductInfoBoxes } from "@/features/product/ProductInfoBoxes";
import { PurchasePanel } from "@/features/product/PurchasePanel";
import { cardPrice } from "@/features/product/price-options";
import { RelatedItems } from "@/features/product/RelatedItems";
import { loadRelatedItems } from "@/features/product/load-related-items";
import { SpecsTable } from "@/features/product/SpecsTable";
import { loadItem } from "@/features/product/load-item";
import { specRows } from "@/features/product/spec-rows";
import { JsonLd } from "@/components/seo/JsonLd";
import { permanentRedirect } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";
import { categoryPath, effectiveSpecs } from "@/lib/categories";
import { localizedName } from "@/lib/localized-name";
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
  const title = condition ? `${item.modelName} — ${t(condition)}` : item.modelName;
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
  // Upper case, an RX code or a slug from before a rename in the URL: send it to the one canonical address.
  if (slug !== item.slug) permanentRedirect({ href: productPath(item.slug), locale });

  const t = await getTranslations("product");
  const storefront = await loadStorefront();
  const trail = item.categoryId ? categoryPath(storefront.categoryTree, item.categoryId) : [];
  const [faqs, related] = await Promise.all([
    loadItemFaqs(item.code),
    loadRelatedItems({ code: item.code, categoryId: item.categoryId }, trail, storefront.showSoldOut)
  ]);
  const moreHref = trail.at(-1) ? categoryHref(trail.at(-1)!.slug) : "/products";
  const defs = item.categoryId ? effectiveSpecs(storefront.categoryTree, item.categoryId) : [];
  const brand = item.brandId ? storefront.brands.find((entry) => entry.id === item.brandId) : undefined;
  const soldOut = item.available <= 0;
  const url = absoluteUrl(localizedPath(locale, productPath(item.slug)));
  const structuredData = [
    productJsonLd({
      name: item.modelName, url, images: item.photos.map((photo) => absoluteUrl(mediaUrl(photo.storageKey))), price: cardPrice(item),
      available: !soldOut, condition, sku: item.code, category: trail.at(-1) ? localizedName(trail.at(-1)!, locale) : undefined, brand: brand?.name,
      organizationId: organizationId()
    }),
    breadcrumbJsonLd([
      { name: t("home"), url: absoluteUrl(localizedPath(locale, "/")) },
      ...trail.map((node) => ({ name: localizedName(node, locale), url: absoluteUrl(localizedPath(locale, `/products?category=${encodeURIComponent(node.slug)}`)) })),
      { name: item.modelName }
    ])
  ];
  const availability = soldOut ? t("soldOut") : condition === "Used" ? t("availableUsed") : t("availableNew", { count: item.available });

  return (
    <div className="pb-44 md:pb-0">
      <JsonLd data={structuredData} />
      <PageHeader
        breadcrumb={[{ label: t("home"), href: "/" }, ...trail.map((node) => ({ label: localizedName(node, locale), href: `/products?category=${encodeURIComponent(node.slug)}` })), { label: item.modelName }]}
        breadcrumbLabel={t("breadcrumb")}
        title={item.modelName}
        titleDir="ltr"
        eyebrow={brand?.name}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <ConditionBadge condition={condition} />
            <StatusBadge tone={soldOut ? "neutral" : "success"}>{availability}</StatusBadge>
          </div>
        }
      />

      <section className="grid shell items-start gap-6 pb-8 md:grid-cols-2 md:gap-10">
        <Gallery photos={item.photos} alt={item.modelName} favourite={{ code: item.code, modelName: item.modelName }} />
        <div className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-24">
          <PurchasePanel
            item={{
              code: item.code,
              modelName: item.modelName,
              condition,
              warrantyLabel: item.warrantyLabel,
              thumbnailStorageKey: item.photos[0]?.thumbnailStorageKey ?? null,
              available: item.available,
              prices: item.prices,
              priceOptions: item.priceOptions
            }}
            whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.product }}
            pageUrl={siteUrl(storefront.domain, productPath(item.slug))}
          />
          <ProductInfoBoxes warrantyLabel={item.warrantyLabel} storefront={storefront} />
        </div>
      </section>

      <div className="flex shell flex-col gap-6">
        <SpecsTable rows={specRows(item.specs, defs)} />
        {faqs.length > 0 ? (
          <Card title={t("faqs")} icon={HelpIcon} padding="md">
            <FaqList entries={faqs} plain />
          </Card>
        ) : null}
      </div>

      <RelatedItems items={related} moreHref={moreHref} />
    </div>
  );
}
