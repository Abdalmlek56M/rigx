import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { CustomerDetail } from "@/features/admin/customers/CustomerDetail";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.customers" });
  return { title: t("detailTitle") };
}

export default async function AdminCustomerPage({ params }: { params: Promise<{ locale: string; id: string }> }) {
  const { id } = await params;
  return <CustomerDetail id={id} />;
}
