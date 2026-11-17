import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { loadTracking } from "@/features/tracking/load-tracking";
import { TrackingOrder } from "@/features/tracking/TrackingOrder";
import { routing } from "@/i18n/routing";
import { whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string; token: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "tracking" });
  return { title: t("title"), robots: { index: false } };
}

export default async function TrackingPage({ params }: Props) {
  const { locale, token } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const [view, storefront] = await Promise.all([loadTracking(token), loadStorefront()]);
  if (!view) notFound();

  return (
    <section className="mx-auto max-w-[1240px] px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <TrackingOrder
        view={view}
        token={token}
        whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.order }}
      />
    </section>
  );
}
