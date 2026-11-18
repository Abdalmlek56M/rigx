import type { Metadata } from "next";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { Pos } from "@/features/admin/pos/Pos";
import { loadShippingRates } from "@/features/checkout/load-shipping-rates";
import { routing } from "@/i18n/routing";
import { loadStorefront } from "@/lib/storefront";

export async function generateMetadata({ params }: { params: Promise<{ locale: string }> }): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "admin.pos" });
  return { title: t("title") };
}

export default async function AdminPosPage() {
  const [storefront, rates] = await Promise.all([loadStorefront(), loadShippingRates()]);
  const governorates = (rates?.governorates ?? []).map(({ code, name }) => ({ code, name }));
  return <Pos leadSources={storefront.checkout.leadSources} governorates={governorates} />;
}
