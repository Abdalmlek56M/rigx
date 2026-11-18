import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { PurchaseEditor } from "@/features/admin/purchases/PurchaseEditor";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.purchases" });
  return { title: t("newTitle") };
}

export default function AdminNewPurchasePage() {
  return <PurchaseEditor id={null} />;
}
