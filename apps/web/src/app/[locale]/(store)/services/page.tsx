import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { ComingSoon } from "@/features/services/ComingSoon";
import { SellTradeForm } from "@/features/services/SellTradeForm";
import { ServiceCard } from "@/features/services/ServiceCard";
import { SERVICES } from "@/features/services/services-data";
import { routing } from "@/i18n/routing";
import { whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";
import { pageSeo } from "@/lib/seo/page-seo";

interface Props {
  params: Promise<{ locale: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "services" });
  return { title: t("title"), ...(await pageSeo(locale, "/services")) };
}

export default async function ServicesPage({ params }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("services");
  const storefront = await loadStorefront();
  const whatsApp = { number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.general };

  return (
    <>
      <section className="mx-auto flex max-w-[1240px] flex-col gap-4 px-4 pb-10 pt-12 md:px-6 md:pt-16">
        <span dir="ltr" className="font-mono text-[13px] font-semibold tracking-[2px] text-orange-text">RIGX SERVICES</span>
        <h1 className="m-0 text-[34px] leading-[1.2] font-bold md:text-[54px]">
          {t("hero.line1")}
          <br />
          {t("hero.line2")}
        </h1>
        <p className="m-0 max-w-[680px] text-base leading-[1.75] text-ink-muted md:text-lg">{t("hero.body")}</p>
      </section>
      <section className="mx-auto grid max-w-[1240px] gap-4 px-4 pb-14 md:grid-cols-[repeat(auto-fit,minmax(340px,1fr))] md:px-6">
        {SERVICES.map((service) => (
          <ServiceCard key={service.id} service={service} whatsApp={whatsApp} />
        ))}
      </section>
      <section className="bg-carbon text-on-carbon">
        <div className="mx-auto flex max-w-[1240px] flex-wrap gap-12 px-4 py-14 md:px-6 md:py-16">
          <div className="flex flex-1 basis-[340px] flex-col gap-3.5">
            <span className="text-[13px] font-bold text-orange">{t("sell.eyebrow")}</span>
            <h2 className="m-0 text-[28px] leading-[1.35] font-bold md:text-4xl">{t("sell.title")}</h2>
            <p className="m-0 text-base leading-[1.8] text-footer-text">{t("sell.body")}</p>
            <ol className="m-0 flex list-none flex-col gap-2 p-0 text-sm text-footer-text">
              {([1, 2, 3, 4] as const).map((step) => (
                <li key={step}>{t(`sell.steps.${step}`)}</li>
              ))}
            </ol>
          </div>
          <SellTradeForm />
        </div>
      </section>
      <ComingSoon />
    </>
  );
}
