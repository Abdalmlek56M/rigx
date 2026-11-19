import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { BuilderPromo } from "@/features/home/BuilderPromo";
import { CategoryGrid } from "@/features/home/CategoryGrid";
import { FaqSection } from "@/features/home/FaqSection";
import { GpuRail } from "@/features/home/GpuRail";
import { Hero } from "@/features/home/Hero";
import { LatestItems } from "@/features/home/LatestItems";
import { RecommendWidget } from "@/features/home/RecommendWidget";
import { ServiceCards } from "@/features/home/ServiceCards";
import { routing } from "@/i18n/routing";
import { fillTemplate, findLink, Platform, SOCIAL_PLATFORMS, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { loadGeneralFaqs } from "@/lib/faqs";
import { organizationId, pageSeo } from "@/lib/seo/page-seo";
import { organizationJsonLd } from "@/lib/seo/structured-data";
import { absoluteUrl, localizedPath } from "@/lib/seo/urls";
import { loadStorefront, type StorefrontData } from "@/lib/storefront";

const FAQ_LIMIT = 5;

/** The latest items and FAQs come from the API, so the page is rendered per request (the API calls are cached for a few minutes). */
export const dynamic = "force-dynamic";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  return pageSeo(locale, "/");
}

/** The store as an online business serving all governorates (no street address: service area only). */
function organization(storefront: StorefrontData, locale: string, description: string) {
  const phone = findLink(storefront.contactLinks, Platform.Phone) ?? findLink(storefront.contactLinks, Platform.WhatsApp);
  const social: readonly string[] = SOCIAL_PLATFORMS.filter((platform) => platform !== Platform.WhatsApp);
  return organizationJsonLd({
    id: organizationId(),
    name: storefront.storeName,
    url: absoluteUrl(localizedPath(locale, "/")),
    logo: absoluteUrl("/icon.svg"),
    description,
    email: storefront.email,
    telephone: phone ? `+${phone.value.replace(/\D/g, "")}` : null,
    sameAs: storefront.contactLinks.filter((link) => social.includes(link.platform) && /^https?:\/\//.test(link.value)).map((link) => link.value)
  });
}

export default async function HomePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const [storefront, faqs] = await Promise.all([loadStorefront(), loadGeneralFaqs(FAQ_LIMIT)]);
  const number = whatsAppNumber(storefront.contactLinks);
  const general = storefront.whatsAppTemplates.general;
  const t = await getTranslations("meta");

  return (
    <>
      <JsonLd data={organization(storefront, locale, t("description"))} />
      <Hero />
      <CategoryGrid categories={storefront.categories} />
      <LatestItems />
      <GpuRail tree={storefront.categoryTree} />
      <section className="flex shell flex-wrap items-stretch gap-[18px] pb-4 pt-12 md:pt-16">
        {number ? <RecommendWidget whatsApp={{ number, template: general }} /> : null}
        <BuilderPromo />
      </section>
      <ServiceCards />
      <FaqSection entries={faqs} whatsAppHref={number ? whatsAppUrl(number, fillTemplate(general, {})) : null} />
    </>
  );
}
