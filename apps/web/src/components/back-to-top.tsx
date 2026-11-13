"use client";

import { useEffect, useState } from "react";
import { useLocale } from "@/lib/i18n";

export function BackToTopButton() {
  const { messages } = useLocale();
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    let frame = 0;
    const onScroll = () => {
      if (frame) return;
      frame = window.requestAnimationFrame(() => {
        setVisible(window.scrollY > 480);
        frame = 0;
      });
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();
    return () => {
      window.removeEventListener("scroll", onScroll);
      if (frame) window.cancelAnimationFrame(frame);
    };
  }, []);

  return (
    <button
      type="button"
      onClick={() => window.scrollTo({ top: 0, behavior: "smooth" })}
      aria-label={messages.backToTop}
      title={messages.backToTop}
      className={`rigx-back-to-top ${visible ? "is-visible" : ""}`}
    >
      <span aria-hidden="true">↑</span>
    </button>
  );
}
