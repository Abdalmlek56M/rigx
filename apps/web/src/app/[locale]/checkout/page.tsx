import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CheckoutForm } from "@/features/checkout/CheckoutForm";
import { CheckoutSteps } from "@/features/checkout/CheckoutSteps";
import { loadShippingRates } from "@/features/checkout/load-shipping-rates";
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
    <section className="mx-auto flex max-w-[1240px] flex-col gap-5 px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <div className="flex flex-col gap-2.5">
        <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
        <CheckoutSteps current={2} />
      </div>
      {rates ? (
        <CheckoutForm
          settings={storefront.checkout}
          payment={storefront.payment}
          pickupEnabled={rates.pickupEnabled}
          pickupAddress={rates.pickupAddress}
          governorates={rates.governorates}
        />
      ) : (
        <p role="alert" className="rounded-card border border-line bg-surface p-6 text-center">{t("loadFailed")}</p>
      )}
    </section>
  );
}
