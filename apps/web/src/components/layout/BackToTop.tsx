"use client";

import { useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { ArrowUpIcon } from "@/components/ui/icons";

const SHOW_AFTER_PX = 480;

function subscribe(onChange: () => void): () => void {
  window.addEventListener("scroll", onChange, { passive: true });
  return () => window.removeEventListener("scroll", onChange);
}

const isScrolled = () => window.scrollY > SHOW_AFTER_PX;
const isScrolledOnServer = () => false;

/** Round arrow at the bottom start corner (bottom-right in Arabic), shown once the page is scrolled. */
export function BackToTop() {
  const t = useTranslations();
  const visible = useSyncExternalStore(subscribe, isScrolled, isScrolledOnServer);

  if (!visible) return null;

  const scrollTop = () => {
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    window.scrollTo({ top: 0, behavior: reduce ? "auto" : "smooth" });
  };

  return (
    <button
      type="button"
      aria-label={t("backToTop")}
      onClick={scrollTop}
      className="back-to-top floating-lift fixed bottom-[max(1.25rem,env(safe-area-inset-bottom))] start-4 z-30 flex h-12 w-12 items-center justify-center rounded-full border border-line bg-surface text-ink shadow-lift transition-colors hover:border-orange hover:text-orange-text md:bottom-6 md:start-6"
    >
      <ArrowUpIcon />
    </button>
  );
}
