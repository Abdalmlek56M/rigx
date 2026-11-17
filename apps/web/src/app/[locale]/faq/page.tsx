import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { FaqList } from "@/components/ui/FaqList";
import { WhatsAppIcon } from "@/components/ui/icons";
import { loadFaqPage } from "@/features/faq/load-faq-page";
import { routing } from "@/i18n/routing";
import { fillTemplate, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "faq" });
  return { title: t("title") };
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
    <section className="mx-auto flex max-w-[860px] flex-col gap-8 px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <div className="flex flex-col gap-1.5">
        <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
        <span className="text-[15px] text-ink-muted">{t("subtitle")}</span>
      </div>
      {groups.length === 0 ? (
        <p role="status" className="m-0 rounded-card border border-line bg-surface p-10 text-center text-ink-muted">{t("empty")}</p>
      ) : (
        groups.map((group) => (
          <div key={group.topic ?? ""} className="flex flex-col gap-3">
            <h2 className="m-0 text-xl font-bold">{group.topic ?? t("other")}</h2>
            <FaqList entries={group.entries} videoTitle={(question) => t("video", { question })} />
          </div>
        ))
      )}
      {number ? (
        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 rounded-[10px] border border-line bg-surface p-5">
          <span className="font-bold">{t("moreTitle")}</span>
          <a href={whatsAppUrl(number, text)} target="_blank" rel="noopener noreferrer" className="inline-flex min-h-11 items-center gap-1.5 text-sm font-bold text-orange-text underline hover:text-orange-text-hover">
            <WhatsAppIcon size={16} />
            {t("moreLink")}
          </a>
        </div>
      ) : null}
    </section>
  );
}
