import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CheckoutSteps } from "@/features/checkout/CheckoutSteps";
import { OrderSuccess } from "@/features/checkout/OrderSuccess";
import { routing } from "@/i18n/routing";
import { whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "orderSuccess" });
  return { title: t("pageTitle"), robots: { index: false } };
}

export default async function OrderSuccessPage({ params }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const storefront = await loadStorefront();

  return (
    <section className="shell pb-16 pt-8 md:pt-10">
      <div className="mx-auto w-full max-w-[760px]">
        <CheckoutSteps current={3} />
      </div>
      <OrderSuccess whatsApp={{ number: whatsAppNumber(storefront.contactLinks), template: storefront.whatsAppTemplates.order }} />
    </section>
  );
}
