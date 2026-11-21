import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { StockItemPage } from "@/features/admin/inventory/StockItemPage";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.inventory" });
  return { title: t("detail.pageTitle") };
}

export default async function AdminStockItemPage({ params }: { params: Promise<{ locale: string; id: string }> }) {
  const { id } = await params;
  return <StockItemPage id={id} />;
}
