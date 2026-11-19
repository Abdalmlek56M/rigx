"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link, usePathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { HEADER_ICON_BUTTON } from "./nav-links";

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
      className={`${HEADER_ICON_BUTTON} px-3 font-mono text-sm font-semibold ${className}`}
    >
      {t("switchLabel")}
    </Link>
  );
}
