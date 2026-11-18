import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { WhatsAppIcon } from "@/components/ui/icons";
import { lookupWarranty } from "@/features/warranty/load-warranty";
import { parseLookupQuery } from "@/features/warranty/warranty-query";
import { WarrantyResults } from "@/features/warranty/WarrantyResults";
import { WarrantySearchForm } from "@/features/warranty/WarrantySearchForm";
import { Link } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";
import { fillTemplate, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";
import { pageSeo } from "@/lib/seo/page-seo";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

export async function generateMetadata({ params, searchParams }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "warranty" });
  const query = parseLookupQuery((await searchParams).q);
  const seo = await pageSeo(locale, "/warranty");
  return query ? { title: t("title"), ...seo, robots: { index: false } } : { title: t("title"), ...seo };
}

export default async function WarrantyPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("warranty");
  const query = parseLookupQuery((await searchParams).q);
  const [storefront, lookup] = await Promise.all([loadStorefront(), query ? lookupWarranty(query) : Promise.resolve(null)]);
  const number = whatsAppNumber(storefront.contactLinks);
  const helpText = fillTemplate(storefront.whatsAppTemplates.general, {}) || t("helpMessage");
  const help = "inline-flex min-h-11 items-center gap-1.5 text-sm font-bold text-orange-text underline hover:text-orange-text-hover";

  return (
    <section className="mx-auto flex max-w-[860px] flex-col gap-6 px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <div className="flex flex-col gap-1.5">
        <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
        <span className="text-[15px] text-ink-muted">{t("subtitle")}</span>
      </div>
      <WarrantySearchForm key={query} initial={query} />
      {lookup === null ? null : lookup.status === "ok" ? (
        <div className="flex flex-col gap-3" aria-live="polite">
          <h2 className="m-0 text-lg font-bold">{t("resultsFor", { query })}</h2>
          {lookup.groups.length === 0 ? (
            <p role="status" className="m-0 rounded-card border border-line bg-surface p-8 text-center text-ink-muted">{t("empty")}</p>
          ) : (
            <WarrantyResults groups={lookup.groups} whatsApp={{ number, template: storefront.whatsAppTemplates.warranty }} />
          )}
        </div>
      ) : (
        <p role="alert" className="m-0 rounded-card border border-line bg-surface p-6 text-center">{t(lookup.status)}</p>
      )}
      <aside className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5 md:p-6">
        <p className="m-0 font-bold">{t("stickerNote")}</p>
        <div className="flex flex-wrap items-center gap-x-6">
          <Link href="/policies/warranty" className={help}>{t("policy")}</Link>
          {number ? (
            <a href={whatsAppUrl(number, helpText)} target="_blank" rel="noopener noreferrer" className={help}>
              <WhatsAppIcon size={16} />
              {t("helpLink")}
            </a>
          ) : null}
        </div>
      </aside>
    </section>
  );
}
