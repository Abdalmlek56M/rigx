import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { routing } from "@/i18n/routing";

export default async function HomePage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("home");

  return (
    <section className="mx-auto max-w-[1240px] px-4 py-12 md:px-6 md:py-16">
      <h1 className="m-0 max-w-3xl text-4xl leading-[1.25] font-bold md:text-[56px]">{t("title")}</h1>
    </section>
  );
}
