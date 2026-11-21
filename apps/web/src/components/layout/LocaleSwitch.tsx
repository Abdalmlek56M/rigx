"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";

const LINK = "flex h-11 min-w-11 items-center justify-center rounded-full px-2 text-[13px] transition-colors";
const ACTIVE = "bg-surface font-bold text-ink shadow-soft";
const IDLE = "font-semibold text-ink-muted hover:bg-surface hover:text-ink";

/** Switches between Arabic and English keeping the current path; each label keeps its own font and language. */
export function LocaleSwitch({ className = "" }: { className?: string }) {
  const t = useTranslations("locale");
  const locale = useLocale();
  const pathname = usePathname();
  const options = [
    { locale: "ar" as Locale, label: t("arabic"), aria: t("arabicAria"), font: "font-sans", lang: "ar" },
    { locale: "en" as Locale, label: t("english"), aria: t("englishAria"), font: "font-mono tracking-wide", lang: "en" }
  ];

  return (
    <div role="group" aria-label={t("switchAria")} className={`flex items-center gap-0.5 ${className}`}>
      {options.map((option) => {
        const selected = locale === option.locale;
        return (
          <Link
            key={option.locale}
            href={pathname}
            locale={option.locale}
            hrefLang={option.locale}
            lang={option.lang}
            aria-label={option.aria}
            aria-current={selected ? "true" : undefined}
            className={`${LINK} ${selected ? ACTIVE : IDLE} ${option.font}`}
          >
            {option.label}
          </Link>
        );
      })}
    </div>
  );
}
