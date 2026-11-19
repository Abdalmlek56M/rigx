"use client";

import { useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { MoonIcon, SunIcon } from "@/components/ui/icons";
import { HEADER_ICON_BUTTON } from "./nav-links";
import { THEME_STORAGE_KEY } from "./theme-script";

type Theme = "light" | "dark";

function subscribe(onChange: () => void): () => void {
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
  return () => observer.disconnect();
}

const read = (): Theme => (document.documentElement.dataset.theme === "dark" ? "dark" : "light");
const readServer = (): Theme => "light";

export function ThemeToggle({ className = "" }: { className?: string }) {
  const t = useTranslations("header");
  const theme = useSyncExternalStore(subscribe, read, readServer);

  const toggle = () => {
    const next: Theme = theme === "dark" ? "light" : "dark";
    document.documentElement.dataset.theme = next;
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, next);
    } catch {
      // Storage unavailable: the choice lasts for this page only.
    }
  };

  return (
    <button
      type="button"
      aria-label={theme === "dark" ? t("lightMode") : t("darkMode")}
      onClick={toggle}
      className={`${HEADER_ICON_BUTTON} ${className}`}
    >
      {theme === "dark" ? <SunIcon /> : <MoonIcon />}
    </button>
  );
}
