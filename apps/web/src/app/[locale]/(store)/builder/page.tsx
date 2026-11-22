import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { BuilderApp } from "@/features/builder/BuilderApp";
import { QuickStart } from "@/features/builder/QuickStart";
import { parseBuilderParams } from "@/features/builder/builder-state";
import { specNameMap } from "@/features/builder/issue-text";
import { loadSlots } from "@/features/builder/load-slots";
import { localizeSlots } from "@/features/builder/slot-names";
import { loadBuilds } from "@/features/builds/load-builds";
import { HelpLink } from "@/components/ui/HelpLink";
import { PageHeader } from "@/components/ui/PageHeader";
import { CpuIcon } from "@/components/ui/icons";
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
  const [rawSlots, builds, storefront] = await Promise.all([loadSlots(), loadBuilds({ usage: null, budget: null }), loadStorefront()]);
  const slots = rawSlots ? localizeSlots(rawSlots, storefront.categoryTree, locale) : null;

  return (
    <>
      <PageHeader atmosphere
        title={t("title")}
        icon={CpuIcon}
        lead={t("subtitle")}
        below={<HelpLink href="/#recommend">{t("recommendLink")}</HelpLink>}
        actions={builds ? <QuickStart builds={builds} /> : undefined}
      />
      {slots === null ? (
        <p role="alert" className="shell py-16 text-center">{t("loadFailed")}</p>
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
