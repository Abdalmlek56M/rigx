import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { WrenchIcon } from "@/components/ui/icons";
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
      <PageHeader
        title={<>{t("hero.line1")}<br />{t("hero.line2")}</>}
        lead={t("hero.body")}
        icon={WrenchIcon}
      />
      <section className="grid shell gap-4 pb-14 md:grid-cols-[repeat(auto-fit,minmax(340px,1fr))]">
        {SERVICES.map((service) => (
          <ServiceCard key={service.id} service={service} whatsApp={whatsApp} />
        ))}
      </section>
      <section id="sell" className="hero-glow scroll-mt-24 border-y border-line">
        <div className="shell grid gap-8 py-14 md:py-16 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)] lg:gap-12">
          <Card padding="lg" className="accent-panel self-start gap-3.5">
            <span className="type-label font-bold text-orange-text">{t("sell.eyebrow")}</span>
            <h2 className="m-0 type-display-title">{t("sell.title")}</h2>
            <p className="m-0 text-base leading-[1.8] text-ink-muted">{t("sell.body")}</p>
            <ol className="m-0 flex list-none flex-col gap-2 p-0 text-sm text-ink-muted">
              {([1, 2, 3, 4] as const).map((step) => (
                <li key={step} className="flex items-start gap-2.5">
                  <span aria-hidden="true" className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-paper font-mono text-[11px] font-bold text-ink">{step}</span>
                  {t(`sell.steps.${step}`)}
                </li>
              ))}
            </ol>
          </Card>
          <SellTradeForm />
        </div>
      </section>
    </>
  );
}
