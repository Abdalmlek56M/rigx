import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { LoginFlow } from "@/features/auth/LoginFlow";
import { routing } from "@/i18n/routing";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<{ next?: string | string[] }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  const t = await getTranslations({ locale, namespace: "auth" });
  return { title: t("title"), robots: { index: false } };
}

export default async function LoginPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("auth.aside");
  const { next } = await searchParams;

  return (
    <div className="flex min-h-[640px]">
      <aside className="hidden w-[360px] shrink-0 flex-col justify-end gap-3.5 bg-carbon bg-[linear-gradient(rgba(244,242,237,.035)_1px,transparent_1px),linear-gradient(90deg,rgba(244,242,237,.035)_1px,transparent_1px)] bg-[length:40px_40px] p-10 text-on-carbon md:flex">
        <span dir="ltr" className="font-mono text-xs tracking-[2px] text-orange">{t("kicker")}</span>
        <p className="m-0 text-[30px] leading-[1.4] font-bold">
          {t("line1")}
          <br />
          {t("line2")}
        </p>
      </aside>
      <div className="flex flex-1 items-start justify-center px-4 py-10 md:px-10 md:py-14">
        <div className="w-full max-w-[400px] rounded-[14px] border border-line bg-surface p-6 md:p-[30px]">
          <LoginFlow next={Array.isArray(next) ? next[0] : next} />
        </div>
      </div>
    </div>
  );
}
