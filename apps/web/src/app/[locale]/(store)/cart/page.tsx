import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CartPage } from "@/features/cart/CartPage";
import { routing } from "@/i18n/routing";

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

  return (
    <section className="mx-auto flex max-w-[1240px] flex-col gap-5 px-4 pb-16 pt-8 md:px-6 md:pt-10">
      <h1 className="m-0 text-3xl font-bold md:text-[40px]">{t("title")}</h1>
      <CartPage />
    </section>
  );
}
