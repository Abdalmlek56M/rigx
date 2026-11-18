import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { BuilderApp } from "@/features/builder/BuilderApp";
import { QuickStart } from "@/features/builder/QuickStart";
import { parseBuilderParams } from "@/features/builder/builder-state";
import { specNameMap } from "@/features/builder/issue-text";
import { loadSlots } from "@/features/builder/load-slots";
import { loadBuilds } from "@/features/builds/load-builds";
import { Link } from "@/i18n/navigation";
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
  const t = await getTranslations({ locale, namespace: "builder" });
  return { title: t("title"), ...(await pageSeo(locale, "/builder")) };
}

export default async function BuilderPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("builder");
  const start = parseBuilderParams(await searchParams);
  const [slots, builds, storefront] = await Promise.all([loadSlots(), loadBuilds({ usage: null, budget: null }), loadStorefront()]);

  return (
    <>
      <section className="mx-auto flex max-w-[1320px] flex-wrap items-end justify-between gap-4 px-4 pb-4 pt-8 md:px-6">
        <div className="flex flex-col gap-1.5">
          <h1 className="m-0 text-3xl font-bold md:text-[38px]">{t("title")}</h1>
          <span className="text-[15px] text-ink-muted">
            {t("subtitle")}{" "}
            <Link href="/#recommend" className="text-orange-text underline hover:text-orange-text-hover">{t("recommendLink")}</Link>
          </span>
        </div>
        {builds ? <QuickStart builds={builds} /> : null}
      </section>
      {slots === null ? (
        <p role="alert" className="mx-auto max-w-[1320px] px-4 py-16 text-center md:px-6">{t("loadFailed")}</p>
      ) : (
        <BuilderApp
          start={start}
          slots={slots}
          specNames={specNameMap(storefront.categoryTree)}
          whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.build }}
          domain={storefront.domain}
        />
      )}
    </>
  );
}
