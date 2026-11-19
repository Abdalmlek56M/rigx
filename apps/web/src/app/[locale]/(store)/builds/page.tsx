import type { Metadata } from "next";
import { EmptyIcon } from "@/components/ui/EmptyIcon";
import { CaseIcon } from "@/components/ui/icons";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { AskUsBanner } from "@/features/builds/AskUsBanner";
import { BuildCard } from "@/features/builds/BuildCard";
import { BuildsFilters } from "@/features/builds/BuildsFilters";
import { parseBuildsParams } from "@/features/builds/build-query";
import { loadBuilds } from "@/features/builds/load-builds";
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

  return (
    <>
      <section className="flex shell flex-col gap-5 pb-5 pt-8 md:pt-10">
        <div className="flex flex-col gap-2">
          <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
          <span className="text-base text-ink-muted">{t("subtitle")}</span>
        </div>
        <BuildsFilters state={state} />
      </section>
      <section className="shell pb-10">
        {builds === null ? (
          <p role="alert" className="rounded-card border border-line bg-surface p-6 text-center">{t("loadFailed")}</p>
        ) : builds.length === 0 ? (
          <div className="flex flex-col items-center gap-2 rounded-card border border-line bg-surface p-10 text-center">
            <EmptyIcon icon={CaseIcon} />
            <b className="text-lg">{t("empty.title")}</b>
            <p className="m-0 text-ink-muted">{t("empty.body")}</p>
          </div>
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
