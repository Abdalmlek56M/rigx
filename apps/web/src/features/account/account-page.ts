import "server-only";
import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { routing } from "@/i18n/routing";
import { whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";

export interface AccountPageProps {
  params: Promise<{ locale: string }>;
}

export type AccountPageKey = "orders" | "order" | "favourites" | "warranties" | "invoices" | "requests" | "builds" | "addresses" | "profile";

/** Metadata of an account page: its own title, never indexed. `titleKey` is under `account.pages`. */
export async function accountMetadata({ params }: AccountPageProps, titleKey: AccountPageKey): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "account.pages" });
  return { title: t(titleKey), robots: { index: false } };
}

/** Validates the locale and fixes it for static rendering; call first in every account page. */
export async function enterAccountPage({ params }: AccountPageProps): Promise<void> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
}

/** The store's WhatsApp number and the owner's templates, for the "ask about this" links. */
export async function loadAccountContact() {
  const storefront = await loadStorefront();
  return {
    number: whatsAppNumber(storefront.contactLinks),
    orderTemplate: storefront.whatsAppTemplates.order,
    warrantyTemplate: storefront.whatsAppTemplates.warranty,
    pickupAddress: storefront.pickupAddress
  };
}

export type AccountContact = Awaited<ReturnType<typeof loadAccountContact>>;
