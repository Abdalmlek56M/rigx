import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { BookIcon, DocumentIcon, WhatsAppIcon } from "@/components/ui/icons";
import { loadPolicy } from "@/features/policies/load-policy";
import { POLICY_KINDS, policyApiKind, policyDescription, policyHeadings } from "@/features/policies/policy-body";
import { PolicyBody } from "@/features/policies/PolicyBody";
import { Link } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";
import { fillTemplate, whatsAppNumber, whatsAppUrl } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { loadStorefront } from "@/lib/storefront";
import { pageSeo } from "@/lib/seo/page-seo";

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
  if (!policy) return {};
  const description = policyDescription(localized(locale, policy.bodyAr, policy.bodyEn));
  return { title: localized(locale, policy.titleAr, policy.titleEn), ...(description ? { description } : {}), ...(await pageSeo(locale, `/policies/${kind}`)) };
}

export default async function PolicyPage({ params }: Props) {
  const { locale, kind } = await params;
  const apiKind = policyApiKind(kind);
  if (!hasLocale(routing.locales, locale) || !apiKind) notFound();
  setRequestLocale(locale);
  const [policy, storefront, t] = await Promise.all([loadPolicy(apiKind), loadStorefront(), getTranslations("policies")]);
  if (!policy) notFound();
  const title = localized(locale, policy.titleAr, policy.titleEn);
  const body = localized(locale, policy.bodyAr, policy.bodyEn);
  const headings = policyHeadings(body);
  const number = whatsAppNumber(storefront.contactLinks);
  const helpText = fillTemplate(storefront.whatsAppTemplates.general, {}) || t("helpMessage", { policy: title });

  return (
    <div className="shell pb-16">
      <PageHeader
        title={title}
        icon={DocumentIcon}
        lead={
          <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
            {policy.publishedAtUtc ? <span>{t("updated", { date: formatDate(policy.publishedAtUtc, locale) })}</span> : null}
            {policy.publishedAtUtc ? <span aria-hidden="true">·</span> : null}
            <span>{t("version", { version: policy.versionNumber })}</span>
          </span>
        }
      />
      <div className="grid items-start gap-6 lg:grid-cols-[240px_minmax(0,1fr)]">
        <nav aria-label={t("navLabel")} className="lg:sticky lg:top-24 lg:self-start">
          {headings.length > 0 ? (
            <Card title={t("toc")} icon={BookIcon} padding="sm" className="hidden gap-2 lg:flex">
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {headings.map((heading) => (
                  <li key={heading.id}>
                    <a href={`#${heading.id}`} className="flex min-h-9 items-center rounded-control px-2 text-ink-muted transition-colors hover:bg-paper hover:text-ink">
                      {heading.text}
                    </a>
                  </li>
                ))}
              </ul>
            </Card>
          ) : null}
          <ul className="m-0 flex list-none flex-row flex-wrap gap-2 p-0 lg:mt-4 lg:flex-col lg:gap-1">
            {POLICY_KINDS.map((item) => (
              <li key={item.slug}>
                <Link
                  href={`/policies/${item.slug}`}
                  aria-current={item.slug === kind ? "page" : undefined}
                  className={`flex min-h-11 items-center rounded-control px-4 text-sm font-bold ${item.slug === kind ? "surface-strong" : "border border-line bg-surface hover:border-orange"}`}
                >
                  {t(`kinds.${item.slug}`)}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
        <div className="flex min-w-0 flex-col gap-4">
          <Card padding="lg">
            <article className="min-w-0 max-w-[72ch] text-[15px] leading-8">
              <PolicyBody body={body} />
            </article>
          </Card>
          {number ? (
            <Card title={t("helpTitle")} icon={WhatsAppIcon} padding="md" className="gap-3">
              <WhatsAppButton href={whatsAppUrl(number, helpText)} variant="outline" className="self-start">{t("helpLink")}</WhatsAppButton>
            </Card>
          ) : null}
        </div>
      </div>
    </div>
  );
}
