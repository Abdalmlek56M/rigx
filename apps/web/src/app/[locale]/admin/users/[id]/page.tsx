import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { StaffProfileScreen } from "@/features/admin/users/StaffProfileScreen";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.users.profile" });
  return { title: t("title") };
}

export default async function AdminUserPage({ params }: { params: Promise<{ locale: string; id: string }> }) {
  const { id } = await params;
  return <StaffProfileScreen id={id} />;
}
