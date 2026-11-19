import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { PoliciesScreen } from "@/features/admin/policies/PoliciesScreen";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.policies" });
  return { title: t("title") };
}

export default function AdminPoliciesPage() {
  return <PoliciesScreen />;
}
