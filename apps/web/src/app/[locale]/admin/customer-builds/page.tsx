import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { CustomerBuilds } from "@/features/admin/customer-builds/CustomerBuilds";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.customerBuilds" });
  return { title: t("title") };
}

export default function AdminCustomerBuildsPage() {
  return <CustomerBuilds />;
}
