import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { WhatsAppIcon } from "@/components/ui/icons";
import { loadPolicy } from "@/features/policies/load-policy";
import { POLICY_KINDS, policyApiKind } from "@/features/policies/policy-body";
import { PolicyBody } from "@/features/policies/PolicyBody";
import { Link } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";
import { fillTemplate, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string; kind: string }>;
}

function localized(locale: string, ar: string, en: string): string {
  return locale === "en" && en ? en : ar;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale, kind } = await params;
  const apiKind = policyApiKind(kind);
  if (!hasLocale(routing.locales, locale) || !apiKind) return {};
  const policy = await loadPolicy(apiKind);
  return policy ? { title: localized(locale, policy.titleAr, policy.titleEn) } : {};
}

export default async function PolicyPage({ params }: Props) {
  const { locale, kind } = await params;
  const apiKind = policyApiKind(kind);
  if (!hasLocale(routing.locales, locale) || !apiKind) notFound();
  setRequestLocale(locale);
  const [policy, storefront, t] = await Promise.all([loadPolicy(apiKind), loadStorefront(), getTranslations("policies")]);
  if (!policy) notFound();
  const title = localized(locale, policy.titleAr, policy.titleEn);
  const number = whatsAppNumber(storefront.contactLinks);
  const helpText = fillTemplate(storefront.whatsAppTemplates.general, {}) || t("helpMessage", { policy: title });

  return (
    <section className="mx-auto grid max-w-[1240px] gap-8 px-4 pb-16 pt-8 md:px-6 md:pt-10 lg:grid-cols-[240px_minmax(0,1fr)]">
      <nav aria-label={t("navLabel")} className="lg:sticky lg:top-24 lg:self-start">
        <ul className="m-0 flex list-none flex-row flex-wrap gap-2 p-0 lg:flex-col lg:gap-1">
          {POLICY_KINDS.map((item) => (
            <li key={item.slug}>
              <Link
                href={`/policies/${item.slug}`}
                aria-current={item.slug === kind ? "page" : undefined}
                className={`flex min-h-11 items-center rounded-control px-4 text-sm font-bold ${item.slug === kind ? "bg-carbon text-on-carbon" : "border border-line bg-surface hover:border-orange"}`}
              >
                {t(`kinds.${item.slug}`)}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
      <article className="flex min-w-0 flex-col gap-6">
        <header className="flex flex-col gap-2">
          <h1 className="m-0 text-3xl font-bold md:text-[40px]">{title}</h1>
          <span className="text-sm text-ink-muted">
            {policy.publishedAtUtc ? t("updated", { date: formatDate(policy.publishedAtUtc, locale) }) : null}
            {policy.publishedAtUtc ? " · " : ""}
            {t("version", { version: policy.versionNumber })}
          </span>
        </header>
        <div className="rounded-[10px] border border-line bg-surface p-5 md:p-8">
          <PolicyBody body={localized(locale, policy.bodyAr, policy.bodyEn)} />
        </div>
        {number ? (
          <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
            <span className="font-bold">{t("helpTitle")}</span>
            <a href={whatsAppUrl(number, helpText)} target="_blank" rel="noopener noreferrer" className="inline-flex min-h-11 items-center gap-1.5 text-sm font-bold text-orange-text underline hover:text-orange-text-hover">
              <WhatsAppIcon size={16} />
              {t("helpLink")}
            </a>
          </div>
        ) : null}
      </article>
    </section>
  );
}
