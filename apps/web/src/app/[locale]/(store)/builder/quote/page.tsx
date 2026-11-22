import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { QuoteSheet } from "@/features/builder/QuoteSheet";
import { parseBuilderParams } from "@/features/builder/builder-state";
import { loadSlots } from "@/features/builder/load-slots";
import { localizeSlots } from "@/features/builder/slot-names";
import { routing } from "@/i18n/routing";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

export async function generateMetadata({ params }: Pick<Props, "params">): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "builder.quote" });
  return { title: t("title"), robots: { index: false } };
}

export default async function QuotePage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const { parts, saved } = parseBuilderParams(await searchParams);
  if (parts.length === 0 && !saved) notFound();

  const [rawSlots, storefront] = await Promise.all([loadSlots(), loadStorefront()]);
  const slots = localizeSlots(rawSlots ?? [], storefront.categoryTree, locale);
  const slotNames = Object.fromEntries(slots.map((entry) => [entry.slot, entry.name]));

  return <QuoteSheet parts={parts} saved={saved} slotNames={slotNames} domain={storefront.domain ?? ""} />;
}
