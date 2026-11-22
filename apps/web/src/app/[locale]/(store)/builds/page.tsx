import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AskUsBanner } from "@/features/builds/AskUsBanner";
import { BuildCard } from "@/features/builds/BuildCard";
import { BuildsFilters } from "@/features/builds/BuildsFilters";
import { parseBuildsParams } from "@/features/builds/build-query";
import { loadBuilds } from "@/features/builds/load-builds";
import { ButtonLink } from "@/components/ui/Button";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorBlock } from "@/components/ui/ErrorBlock";
import { PageHeader } from "@/components/ui/PageHeader";
import { CaseIcon } from "@/components/ui/icons";
import { routing } from "@/i18n/routing";
import { whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";
import { pageSeo } from "@/lib/seo/page-seo";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

export async function generateMetadata({ params }: Pick<Props, "params">): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "builds" });
  return { title: t("title"), ...(await pageSeo(locale, "/builds")) };
}

export default async function BuildsPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("builds");
  const state = parseBuildsParams(await searchParams);
  const [builds, storefront] = await Promise.all([loadBuilds(state), loadStorefront()]);
  const filtered = state.usage !== null || state.budget !== null;

  return (
    <>
      <PageHeader atmosphere title={t("title")} lead={t("subtitle")} icon={CaseIcon} />
      <section className="shell pb-5">
        <BuildsFilters state={state} />
      </section>
      <section className="shell pb-10">
        {builds === null ? (
          <ErrorBlock message={t("loadFailed")} />
        ) : builds.length === 0 ? (
          <EmptyState
            icon={CaseIcon}
            title={t("empty.title")}
            help={t("empty.body")}
            action={filtered ? <ButtonLink href="/builds" variant="secondary">{t("empty.all")}</ButtonLink> : undefined}
          />
        ) : (
          <ul className="m-0 grid list-none gap-[18px] p-0 md:grid-cols-[repeat(auto-fit,minmax(360px,1fr))]">
            {builds.map((build) => (
              <li key={build.code} className="flex">
                <BuildCard build={build} />
              </li>
            ))}
          </ul>
        )}
      </section>
      <section className="shell pb-16 md:pb-[72px]">
        <AskUsBanner number={whatsAppNumber(storefront.contactLinks)} template={storefront.whatsAppTemplates.general} />
      </section>
    </>
  );
}
