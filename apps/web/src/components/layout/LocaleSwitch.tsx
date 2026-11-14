"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";

/** Switches between Arabic and English keeping the current path. Rendered only when English is enabled. */
export function LocaleSwitch({ className = "" }: { className?: string }) {
  const t = useTranslations("locale");
  const locale = useLocale();
  const pathname = usePathname();
  const target: Locale = locale === "ar" ? "en" : "ar";

  return (
    <Link
      href={pathname}
      locale={target}
      hrefLang={target}
      aria-label={t("switchAria")}
      className={`flex h-11 min-w-11 items-center justify-center rounded-control border border-header-line px-3 font-mono text-sm font-semibold text-on-carbon ${className}`}
    >
      {t("switchLabel")}
    </Link>
  );
}
