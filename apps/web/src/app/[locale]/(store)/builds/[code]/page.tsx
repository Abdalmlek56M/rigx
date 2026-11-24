import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Gallery } from "@/features/product/Gallery";
import { BuildPartsTable } from "@/features/builds/BuildPartsTable";
import { BuildPurchasePanel } from "@/features/builds/BuildPurchasePanel";
import { RelatedBuilds } from "@/features/builds/RelatedBuilds";
import { loadBuild, loadBuilds } from "@/features/builds/load-builds";
import { pickRelatedBuilds } from "@/features/builds/related-builds";
import { buildSuitableFor, buildTitle } from "@/features/builds/build-view";
import { JsonLd } from "@/components/seo/JsonLd";
import { PageHeader } from "@/components/ui/PageHeader";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { routing } from "@/i18n/routing";
import { siteUrl, whatsAppNumber } from "@/lib/contact";
import { mediaUrl } from "@/lib/media";
import { openGraphBase, organizationId, pageSeo } from "@/lib/seo/page-seo";
import { breadcrumbJsonLd, productJsonLd } from "@/lib/seo/structured-data";
import { absoluteUrl, localizedPath } from "@/lib/seo/urls";
import { loadStorefront } from "@/lib/storefront";
import { parseUsage } from "@/lib/usage";

interface Props {
  params: Promise<{ locale: string; code: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale, code } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  setRequestLocale(locale);
  const build = await loadBuild(code);
  if (!build) return {};
  const title = buildTitle(build, locale);
  const path = `/builds/${build.code}`;
  const photo = build.photos[0];
  return {
    title,
    ...(await pageSeo(locale, path)),
    openGraph: { ...openGraphBase(locale), title, url: localizedPath(locale, path), ...(photo ? { images: [{ url: mediaUrl(photo.storageKey), alt: title }] } : {}) }
  };
}

export default async function BuildPage({ params }: Props) {
  const { locale, code } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const build = await loadBuild(code);
  if (!build) notFound();
  const t = await getTranslations("build");
  const tu = await getTranslations("usage");
  const [storefront, allBuilds] = await Promise.all([loadStorefront(), loadBuilds({ usage: null, budget: null })]);
  const usage = parseUsage(build.usage);
  const title = buildTitle(build, locale);
  const suitableFor = buildSuitableFor(build, locale);
  const related = pickRelatedBuilds(build.code, build.usage, allBuilds ?? []);
  const url = absoluteUrl(localizedPath(locale, `/builds/${build.code}`));
  const structuredData = [
    productJsonLd({
      name: title, url, images: build.photos.map((photo) => absoluteUrl(mediaUrl(photo.storageKey))), price: build.salePrice,
      available: build.copies > 0, sku: build.code, organizationId: organizationId(),
      returnPolicyUrl: absoluteUrl(localizedPath(locale, "/policies/returns"))
    }),
    breadcrumbJsonLd([
      { name: t("home"), url: absoluteUrl(localizedPath(locale, "/")) },
      { name: t("builds"), url: absoluteUrl(localizedPath(locale, "/builds")) },
      { name: title }
    ])
  ];

  return (
    <div className="pb-44 md:pb-0">
      <JsonLd data={structuredData} />
      <PageHeader
        breadcrumb={[{ label: t("home"), href: "/" }, { label: t("builds"), href: "/builds" }, { label: title }]}
        breadcrumbLabel={t("breadcrumb")}
        title={title}
        titleDir="auto"
        lead={suitableFor ? <><b>{t("suitableFor")}</b> {suitableFor}</> : undefined}
        actions={usage ? <StatusBadge tone="info">{tu(usage)}</StatusBadge> : undefined}
      />
      <section className="grid shell items-start gap-6 pb-8 md:grid-cols-2 md:gap-10">
        <Gallery photos={build.photos} alt={title} favourite={{ code: build.code, modelName: title }} />
        <div className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-24">
          {build.warrantyLabel ? <StatusBadge tone="success">{build.warrantyLabel}</StatusBadge> : null}
          <BuildPurchasePanel
            build={build}
            whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.build }}
            pageUrl={siteUrl(storefront.domain, `/builds/${build.code}`)}
          />
        </div>
      </section>
      <div className="flex shell flex-col gap-6">
        <BuildPartsTable build={build} />
      </div>

      <RelatedBuilds builds={related} />
    </div>
  );
}
