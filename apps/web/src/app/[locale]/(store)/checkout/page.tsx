import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CheckoutForm } from "@/features/checkout/CheckoutForm";
import { CheckoutSteps } from "@/features/checkout/CheckoutSteps";
import { loadShippingRates } from "@/features/checkout/load-shipping-rates";
import { ErrorBlock } from "@/components/ui/ErrorBlock";
import { PageHeader } from "@/components/ui/PageHeader";
import { routing } from "@/i18n/routing";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "checkout" });
  return { title: t("title"), robots: { index: false } };
}

export default async function CheckoutPage({ params }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("checkout");
  const [storefront, rates] = await Promise.all([loadStorefront(), loadShippingRates()]);

  return (
    <div className="pb-16">
      <PageHeader title={t("title")} below={<CheckoutSteps current={2} />} />
      <section className="shell">
        {rates ? (
          <CheckoutForm
            settings={storefront.checkout}
            payment={storefront.payment}
            pickupEnabled={rates.pickupEnabled}
            pickupAddress={rates.pickupAddress}
            governorates={rates.governorates}
          />
        ) : (
          <ErrorBlock message={t("loadFailed")} />
        )}
      </section>
    </div>
  );
}
