import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { Card } from "@/components/ui/Card";
import { FaqList } from "@/components/ui/FaqList";
import { PageHeader } from "@/components/ui/PageHeader";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { GpuIcon, HelpIcon, MonitorIcon, ShieldIcon, TruckIcon, WalletIcon, WhatsAppIcon, WrenchIcon } from "@/components/ui/icons";
import { loadFaqPage } from "@/features/faq/load-faq-page";
import { routing } from "@/i18n/routing";
import { fillTemplate, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";
import { pageSeo } from "@/lib/seo/page-seo";
import { faqJsonLd } from "@/lib/seo/structured-data";

interface Props {
  params: Promise<{ locale: string }>;
}

/** The group's mark: a loose keyword match on the owner's topic, falling back to the help icon. */
function topicIcon(topic: string | null) {
  const text = (topic ?? "").toLowerCase();
  if (/شحن|توصيل|delivery|shipping/.test(text)) return TruckIcon;
  if (/ضمان|warranty|إرجاع|استرجاع|return/.test(text)) return ShieldIcon;
  if (/دفع|سعر|payment|price|تقسيط/.test(text)) return WalletIcon;
  if (/كارت|شاشة|gpu|graphics|monitor/.test(text)) return GpuIcon;
  if (/معالج|بورد|رام|تجميع|pc|cpu|build/.test(text)) return WrenchIcon;
  if (/قطع|parts|accessor/.test(text)) return MonitorIcon;
  return HelpIcon;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "faq" });
  return { title: t("title"), ...(await pageSeo(locale, "/faq")) };
}

export default async function FaqPage({ params }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("faq");
  const [groups, storefront] = await Promise.all([loadFaqPage(locale === "en"), loadStorefront()]);
  const number = whatsAppNumber(storefront.contactLinks);
  const text = fillTemplate(storefront.whatsAppTemplates.general, {}) || t("moreMessage");

  return (
    <div className="shell mx-auto flex max-w-[860px] flex-col gap-5 pb-16">
      <PageHeader contained={false} title={t("title")} lead={t("subtitle")} icon={HelpIcon} />
      {groups.length > 0 ? <JsonLd data={faqJsonLd(groups.flatMap((group) => group.entries))} /> : null}
      {groups.length === 0 ? (
        <Card padding="lg"><p role="status" className="m-0 text-center text-ink-muted">{t("empty")}</p></Card>
      ) : (
        groups.map((group) => (
          <Card key={group.topic ?? ""} title={group.topic ?? t("other")} icon={topicIcon(group.topic)} padding="md" className="gap-1">
            <FaqList entries={group.entries} plain videoTitle={(question) => t("video", { question })} />
          </Card>
        ))
      )}
      {number ? (
        <Card title={t("moreTitle")} icon={WhatsAppIcon} padding="md" className="gap-3">
          <WhatsAppButton href={whatsAppUrl(number, text)} variant="outline" className="self-start">{t("moreLink")}</WhatsAppButton>
        </Card>
      ) : null}
    </div>
  );
}
