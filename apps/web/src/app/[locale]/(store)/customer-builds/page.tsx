import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Pagination } from "@/features/catalog/Pagination";
import { CommunityBuildCard } from "@/features/community/CommunityBuildCard";
import { COMMUNITY_PAGE_SIZE, loadCommunityBuilds } from "@/features/community/load-community";
import { ButtonLink } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorBlock } from "@/components/ui/ErrorBlock";
import { PageHeader } from "@/components/ui/PageHeader";
import { CaseIcon, CpuIcon } from "@/components/ui/icons";
import { routing } from "@/i18n/routing";
import { pageSeo } from "@/lib/seo/page-seo";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

export async function generateMetadata({ params }: Pick<Props, "params">): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "communityBuilds" });
  return { title: t("title"), ...(await pageSeo(locale, "/customer-builds")) };
}

/** The page number in the address; anything that is not a whole number from 1 up is the first page. */
function pageOf(value: string | string[] | undefined): number {
  const page = Number(Array.isArray(value) ? value[0] : value);
  return Number.isInteger(page) && page >= 1 ? page : 1;
}

export default async function CustomerBuildsPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("communityBuilds");
  const page = pageOf((await searchParams).page);
  const result = await loadCommunityBuilds(page);

  return (
    <>
      <PageHeader
        atmosphere
        title={t("title")}
        lead={t("subtitle")}
        icon={CaseIcon}
        actions={<ButtonLink href="/builder" variant="primary" icon={CpuIcon}>{t("startOwn")}</ButtonLink>}
      />
      <section className="shell pb-16 md:pb-[72px]">
        {result === null ? (
          <ErrorBlock message={t("loadFailed")} />
        ) : result.items.length === 0 ? (
          <EmptyState icon={CaseIcon} title={t("empty.title")} help={t("empty.body")} action={<ButtonLink href="/builder" variant="secondary">{t("startOwn")}</ButtonLink>} />
        ) : (
          <>
            <ul className="m-0 grid list-none gap-[18px] p-0 md:grid-cols-2 xl:grid-cols-3">
              {result.items.map((build) => (
                <li key={build.token} className="flex">
                  <CommunityBuildCard build={build} />
                </li>
              ))}
            </ul>
            <Pagination current={result.page} pages={Math.ceil(result.total / COMMUNITY_PAGE_SIZE)} hrefFor={(next) => (next > 1 ? `/customer-builds?page=${next}` : "/customer-builds")} />
          </>
        )}
      </section>
    </>
  );
}
