"use client";

import { useTranslations } from "next-intl";
import { MoonIcon, SunIcon } from "@/components/ui/icons";
import { HEADER_ICON_BUTTON, STRIP_BUTTON } from "./nav-links";
import { setTheme, useTheme } from "./use-theme";

/** Switches the light and dark themes; `strip` draws it small for the dark top strip. */
export function ThemeToggle({ className = "", strip = false }: { className?: string; strip?: boolean }) {
  const t = useTranslations("header");
  const theme = useTheme();

  return (
    <button
      type="button"
      aria-label={theme === "dark" ? t("lightMode") : t("darkMode")}
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
      className={`${strip ? STRIP_BUTTON : HEADER_ICON_BUTTON} ${className}`}
    >
      {theme === "dark" ? <SunIcon size={strip ? 16 : 20} /> : <MoonIcon size={strip ? 16 : 20} />}
    </button>
  );
}
