import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CartPage } from "@/features/cart/CartPage";
import { PageHeader } from "@/components/ui/PageHeader";
import { CartIcon } from "@/components/ui/icons";
import { routing } from "@/i18n/routing";
import { whatsAppNumber } from "@/lib/contact";
import { loadStorefront } from "@/lib/storefront";

interface Props {
  params: Promise<{ locale: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "cartPage" });
  return { title: t("title"), robots: { index: false } };
}

export default async function CartRoute({ params }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("cartPage");
  const storefront = await loadStorefront();

  return (
    <div className="pb-16">
      <PageHeader title={t("title")} icon={CartIcon} />
      <section className="shell">
        <CartPage whatsAppNumber={whatsAppNumber(storefront.contactLinks)} />
      </section>
    </div>
  );
}
