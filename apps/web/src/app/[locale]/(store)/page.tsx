import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { setRequestLocale } from "next-intl/server";
import { BuilderPromo } from "@/features/home/BuilderPromo";
import { CategoryGrid } from "@/features/home/CategoryGrid";
import { FaqSection } from "@/features/home/FaqSection";
import { Hero } from "@/features/home/Hero";
import { LatestItems } from "@/features/home/LatestItems";
import { RecommendWidget } from "@/features/home/RecommendWidget";
import { ServiceCards } from "@/features/home/ServiceCards";
import { routing } from "@/i18n/routing";
import { fillTemplate, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { loadGeneralFaqs } from "@/lib/faqs";
import { loadStorefront } from "@/lib/storefront";

const FAQ_LIMIT = 5;

/** The latest items and FAQs come from the API, so the page is rendered per request (the API calls are cached for a few minutes). */
export const dynamic = "force-dynamic";

export default async function HomePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const [storefront, faqs] = await Promise.all([loadStorefront(), loadGeneralFaqs(FAQ_LIMIT)]);
  const number = whatsAppNumber(storefront.contactLinks);
  const general = storefront.whatsAppTemplates.general;

  return (
    <>
      <Hero />
      <CategoryGrid categories={storefront.categories} />
      <LatestItems />
      <section className="mx-auto flex max-w-[1240px] flex-wrap gap-[18px] px-4 pb-4 md:px-6">
        {number ? <RecommendWidget whatsApp={{ number, template: general }} /> : null}
        <BuilderPromo />
      </section>
      <ServiceCards />
      <FaqSection entries={faqs} whatsAppHref={number ? whatsAppUrl(number, fillTemplate(general, {})) : null} />
    </>
  );
}
