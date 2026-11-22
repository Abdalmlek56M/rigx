import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { Card } from "@/components/ui/Card";
import { Logo } from "@/components/ui/Logo";
import { LoginFlow } from "@/features/auth/LoginFlow";
import { Link } from "@/i18n/navigation";
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

/** Sign-in: one centred card under the logo (Google pill, then the email code). Nothing else competes with the form. */
export default async function LoginPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("header");
  const { next } = await searchParams;

  return (
    <div className="shell flex flex-col items-center gap-6 py-10 md:py-16">
      <Link href="/" aria-label={t("home")} className="press flex text-ink"><Logo width={132} /></Link>
      <Card padding="lg" className="w-full max-w-[440px] shadow-soft">
        <LoginFlow next={Array.isArray(next) ? next[0] : next} />
      </Card>
    </div>
  );
}
