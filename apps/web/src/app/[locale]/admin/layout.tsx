import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import type { ReactNode } from "react";
import { AdminShell } from "@/features/admin/AdminShell";
import { routing } from "@/i18n/routing";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.shell" });
  return { title: { absolute: t("title"), template: `%s · ${t("title")}` }, robots: { index: false } };
}

/** Rendered per request like the storefront: a copy built during a deploy (API not up yet) would hold empty lists (the POS governorates, lead sources). */
export const dynamic = "force-dynamic";

/** The admin has its own frame (no storefront header or footer); the guard and sidebar run in the browser from `/identity/me`. */
export default async function AdminLayout({ children, params }: { children: ReactNode; params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  return <AdminShell>{children}</AdminShell>;
}
