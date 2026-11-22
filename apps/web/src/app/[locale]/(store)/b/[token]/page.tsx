import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { BuildVisibilityControl } from "@/features/builder/BuildVisibilityControl";
import { SharedBuildParts } from "@/features/builder/SharedBuildParts";
import { SharedBuildSummary } from "@/features/builder/SharedBuildSummary";
import { isSavedToken } from "@/features/builder/builder-state";
import { specNameMap } from "@/features/builder/issue-text";
import { loadSharedBuild } from "@/features/builder/load-shared-build";
import { parseVisibility } from "@/features/builder/build-visibility";
import { loadSlots } from "@/features/builder/load-slots";
import { localizeSlots } from "@/features/builder/slot-names";
import { BuildReactions } from "@/features/community/BuildReactions";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { EyeIcon } from "@/components/ui/icons";
import { routing } from "@/i18n/routing";
import { siteUrl, whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";
import { parseUsage } from "@/lib/usage";

interface Props {
  params: Promise<{ locale: string; token: string }>;
}

/** A shared build is one person's link, not a page to find in search. */
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale, token } = await params;
  if (!hasLocale(routing.locales, locale) || !isSavedToken(token)) return {};
  const t = await getTranslations({ locale, namespace: "sharedBuild" });
  const build = await loadSharedBuild(token);
  return { title: build?.name ?? t("untitled"), robots: { index: false, follow: false } };
}

export default async function SharedBuildPage({ params }: Props) {
  const { locale, token } = await params;
  if (!hasLocale(routing.locales, locale) || !isSavedToken(token)) notFound();
  setRequestLocale(locale);
  const build = await loadSharedBuild(token);
  if (!build) notFound();
  const [t, tu, rawSlots, storefront] = await Promise.all([getTranslations("sharedBuild"), getTranslations("usage"), loadSlots(), loadStorefront()]);
  const slots = rawSlots ? localizeSlots(rawSlots, storefront.categoryTree, locale) : [];
  const slotNames = Object.fromEntries(slots.map((entry) => [entry.slot, entry.name]));
  const usage = parseUsage(build.usage);
  const evaluation = build.evaluation;
  const visibility = parseVisibility(build.visibility);

  return (
    <>
      <PageHeader
        breadcrumb={[{ label: t("home"), href: "/" }, { label: t("builder"), href: "/builder" }, { label: build.name ?? t("untitled") }]}
        breadcrumbLabel={t("breadcrumb")}
        title={build.name ?? t("untitled")}
        titleDir="auto"
        lead={build.authorName ? t("by", { name: build.authorName }) : undefined}
        actions={
          <div className="flex flex-wrap items-center gap-3">
            {usage ? <StatusBadge tone="info">{tu(usage)}</StatusBadge> : null}
            {visibility !== "Private" ? <BuildReactions token={token} initial={build.reactions} /> : null}
          </div>
        }
      />
      <div className="grid shell items-start gap-5 pb-16 lg:grid-cols-[minmax(0,1fr)_340px]">
        <div className="flex min-w-0 flex-col gap-4">
          {evaluation.missingCodes.length > 0 ? (
            <Card padding="sm"><p className="m-0 text-sm">{t("missing")}</p></Card>
          ) : null}
          <SharedBuildParts parts={evaluation.parts} slotNames={slotNames} />
          {build.isOwner ? (
            <Card title={t("visibilityTitle")} icon={EyeIcon}>
              <BuildVisibilityControl token={token} visibility={visibility} moderation={build.moderation ?? null} />
            </Card>
          ) : null}
        </div>
        <SharedBuildSummary
          token={token}
          evaluation={evaluation}
          slotNames={slotNames}
          specNames={specNameMap(storefront.categoryTree)}
          whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.build }}
          pageUrl={siteUrl(storefront.domain, `/b/${token}`)}
        />
      </div>
    </>
  );
}
