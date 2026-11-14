"use client";

import { useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { ArrowUpIcon } from "@/components/ui/icons";

const SHOW_AFTER_PX = 600;

function subscribe(onChange: () => void): () => void {
  window.addEventListener("scroll", onChange, { passive: true });
  return () => window.removeEventListener("scroll", onChange);
}

const isScrolled = () => window.scrollY > SHOW_AFTER_PX;
const isScrolledOnServer = () => false;

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
      className="fixed bottom-5 start-4 z-30 flex h-11 w-11 items-center justify-center rounded-full border border-line bg-surface text-ink shadow-[0_8px_24px_rgba(0,0,0,.15)] md:bottom-6 md:start-6"
    >
      <ArrowUpIcon />
    </button>
  );
}
