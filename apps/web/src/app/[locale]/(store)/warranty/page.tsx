import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorBlock } from "@/components/ui/ErrorBlock";
import { PageHeader } from "@/components/ui/PageHeader";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { ShieldIcon } from "@/components/ui/icons";
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

  return (
    <section className="mx-auto flex w-full max-w-[860px] flex-col gap-5 px-4 pb-16 md:px-6">
      <PageHeader contained={false} title={t("title")} lead={t("subtitle")} icon={ShieldIcon} />
      <WarrantySearchForm key={query} initial={query} />
      <p className="m-0 text-xs text-ink-muted">
        {t.rich("accountNote", { link: (chunks) => <Link href="/account/warranties" className="font-bold text-orange-text underline hover:text-orange-text-hover">{chunks}</Link> })}
      </p>
      {lookup === null ? null : lookup.status === "ok" ? (
        <div className="flex flex-col gap-3" aria-live="polite">
          <h2 className="m-0 text-lg font-bold">{t("resultsFor", { query })}</h2>
          {lookup.items.length === 0 ? (
            <EmptyState icon={ShieldIcon} title={t("empty")} />
          ) : (
            <WarrantyResults items={lookup.items} whatsApp={{ number, template: storefront.whatsAppTemplates.warranty }} />
          )}
        </div>
      ) : (
        <ErrorBlock message={t(lookup.status)} />
      )}
      <Card padding="md" className="gap-3">
        <h2 className="m-0 type-section-title">{t("stickerNote")}</h2>
        <div className="flex flex-wrap items-center gap-2">
          <ButtonLink href="/policies/warranty" variant="secondary">{t("policy")}</ButtonLink>
          {number ? <WhatsAppButton href={whatsAppUrl(number, helpText)} variant="outline">{t("helpLink")}</WhatsAppButton> : null}
        </div>
      </Card>
    </section>
  );
}
