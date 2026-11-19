import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { DocumentIcon, ShieldIcon, TruckIcon } from "@/components/ui/icons";
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

/** Sign-in: the card (Google pill, then email code) beside a graphite panel listing what the account holds. */
export default async function LoginPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("auth.aside");
  const { next } = await searchParams;
  const points = [
    { icon: <TruckIcon size={20} />, text: t("orders") },
    { icon: <ShieldIcon size={20} />, text: t("warranty") },
    { icon: <DocumentIcon size={20} />, text: t("invoices") }
  ];

  return (
    <div className="shell grid items-stretch gap-6 py-8 md:py-12 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:gap-10">
      <div className="flex items-center justify-center">
        <div className="w-full max-w-[440px] rounded-panel border border-line bg-surface p-6 shadow-soft sm:p-9">
          <LoginFlow next={Array.isArray(next) ? next[0] : next} />
        </div>
      </div>
      <aside className="relative hidden min-h-[520px] flex-col justify-end gap-6 overflow-hidden accent-panel rounded-panel border p-10 lg:flex">
        <svg aria-hidden="true" viewBox="0 0 100 100" className="absolute -end-16 -top-16 h-[420px] w-[420px] text-orange opacity-90">
          <polygon points="70,0 100,0 30,100 0,100" fill="currentColor" />
          <polygon points="0,0 30,0 100,100 70,100" fill="currentColor" opacity="0.18" />
        </svg>
        <p className="relative m-0 text-[34px] font-bold leading-[1.35]" style={{ fontFamily: "var(--font-display)" }}>
          {t("line1")}
          <br />
          {t("line2")}
        </p>
        <ul className="relative m-0 flex list-none flex-col gap-3 p-0 text-[15px] text-ink-muted">
          {points.map((point) => (
            <li key={point.text} className="flex items-center gap-3">
              <span className="flex h-9 w-9 items-center justify-center rounded-full bg-orange-soft text-orange-text">{point.icon}</span>
              {point.text}
            </li>
          ))}
        </ul>
      </aside>
    </div>
  );
}
