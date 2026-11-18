import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Gallery } from "@/features/product/Gallery";
import { BuildPartsTable } from "@/features/builds/BuildPartsTable";
import { BuildPurchasePanel } from "@/features/builds/BuildPurchasePanel";
import { buildSuitableFor, buildTitle } from "@/features/builds/build-view";
import { loadBuild } from "@/features/builds/load-builds";
import { JsonLd } from "@/components/seo/JsonLd";
import { Link } from "@/i18n/navigation";
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
  const storefront = await loadStorefront();
  const usage = parseUsage(build.usage);
  const title = buildTitle(build, locale);
  const suitableFor = buildSuitableFor(build, locale);
  const url = absoluteUrl(localizedPath(locale, `/builds/${build.code}`));
  const structuredData = [
    productJsonLd({
      name: title, url, images: build.photos.map((photo) => absoluteUrl(mediaUrl(photo.storageKey))), price: build.salePrice,
      available: build.copies > 0, sku: build.code, organizationId: organizationId()
    }),
    breadcrumbJsonLd([
      { name: t("home"), url: absoluteUrl(localizedPath(locale, "/")) },
      { name: t("builds"), url: absoluteUrl(localizedPath(locale, "/builds")) },
      { name: title }
    ])
  ];

  return (
    <div className="pb-28 md:pb-0">
      <JsonLd data={structuredData} />
      <nav aria-label={t("breadcrumb")} className="mx-auto max-w-[1240px] px-4 pt-6 text-sm text-ink-muted md:px-6">
        <ol className="m-0 flex list-none flex-wrap gap-2 p-0">
          <li><Link href="/" className="hover:underline">{t("home")}</Link></li>
          <li className="flex gap-2">
            <span aria-hidden="true">/</span>
            <Link href="/builds" className="hover:underline">{t("builds")}</Link>
          </li>
          <li className="flex gap-2">
            <span aria-hidden="true">/</span>
            <span aria-current="page">{title}</span>
          </li>
        </ol>
      </nav>
      <section className="mx-auto grid max-w-[1240px] items-start gap-6 px-4 py-6 md:grid-cols-2 md:gap-10 md:px-6 md:py-9">
        <Gallery photos={build.photos} alt={title} />
        <div className="flex min-w-0 flex-col gap-4">
          <div className="flex flex-wrap items-center gap-2.5">
            {usage ? <span className="rounded bg-carbon px-2.5 py-1 text-xs font-bold text-on-carbon">{tu(usage)}</span> : null}
            <span dir="ltr" className="font-mono text-[13px] text-ink-muted">{build.code}</span>
            {build.warrantyLabel ? <span className="text-[13px] text-ink-muted">{build.warrantyLabel}</span> : null}
          </div>
          <h1 className="m-0 text-2xl leading-[1.3] font-bold md:text-[32px]">{title}</h1>
          {suitableFor ? (
            <p className="m-0 rounded-card bg-paper px-3.5 py-3 text-sm leading-7">
              <b>{t("suitableFor")}</b> {suitableFor}
            </p>
          ) : null}
          <BuildPurchasePanel
            build={build}
            whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.build }}
            pageUrl={siteUrl(storefront.domain, `/builds/${build.code}`)}
          />
        </div>
      </section>
      <div className="mx-auto flex max-w-[1240px] flex-col gap-10 px-4 pb-14 md:px-6">
        <BuildPartsTable build={build} />
      </div>
    </div>
  );
}
