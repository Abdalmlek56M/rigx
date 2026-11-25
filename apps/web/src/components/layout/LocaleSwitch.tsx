"use client";

import { useLocale, useTranslations } from "next-intl";
import { GlobeIcon } from "@/components/ui/icons";
import { Link, usePathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { STRIP_BUTTON } from "./nav-links";

const ROW = "flex min-h-11 w-full items-center gap-2.5 rounded-card px-3 text-sm font-bold text-ink transition-colors hover:bg-paper";

/**
 * One button that switches to the other language, keeping the current path; it shows the target language by name.
 * On the top strip the name stands beside a small globe; the menu row keeps its full-width style.
 */
export function LocaleSwitch({ className = "", fullWidth = false }: { className?: string; fullWidth?: boolean }) {
  const t = useTranslations("locale");
  const locale = useLocale();
  const pathname = usePathname();
  const target: Locale = locale === "en" ? "ar" : "en";
  const label = t("switchLabel");
  const aria = target === "ar" ? t("arabicAria") : t("englishAria");
  const font = target === "en" ? "font-mono tracking-wide" : "font-sans";
  const classes = fullWidth ? ROW : STRIP_BUTTON;

  return (
    <Link
      href={pathname}
      locale={target}
      hrefLang={target}
      lang={target}
      aria-label={aria}
      data-tip={aria}
      className={`tip ${classes}${className ? ` ${className}` : ""}`}
    >
      <GlobeIcon size={fullWidth ? 18 : 15} />
      <span className={font}>{label}</span>
    </Link>
  );
}
