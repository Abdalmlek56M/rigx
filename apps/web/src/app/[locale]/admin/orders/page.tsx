import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { Orders } from "@/features/admin/orders/Orders";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.orders" });
  return { title: t("title") };
}

export default function AdminOrdersPage() {
  return <Orders />;
}
