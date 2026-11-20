"use client";

import { useTranslations } from "next-intl";
import { MoonIcon, SunIcon } from "@/components/ui/icons";
import { HEADER_ICON_BUTTON } from "./nav-links";
import { setTheme, useTheme } from "./use-theme";

export function ThemeToggle({ className = "" }: { className?: string }) {
  const t = useTranslations("header");
  const theme = useTheme();

  return (
    <button
      type="button"
      aria-label={theme === "dark" ? t("lightMode") : t("darkMode")}
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
      className={`${HEADER_ICON_BUTTON} ${className}`}
    >
      {theme === "dark" ? <SunIcon /> : <MoonIcon />}
    </button>
  );
}
