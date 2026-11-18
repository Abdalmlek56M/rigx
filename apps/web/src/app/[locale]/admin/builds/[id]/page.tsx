import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { BuildEditor } from "@/features/admin/builds/BuildEditor";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.builds" });
  return { title: t("detailTitle") };
}

export default async function AdminBuildPage({ params }: { params: Promise<{ locale: string; id: string }> }) {
  const { id } = await params;
  return <BuildEditor id={id} />;
}
