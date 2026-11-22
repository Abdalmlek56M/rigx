"use client";

import { useLayoutEffect, useRef } from "react";
import { useTranslations } from "next-intl";

const DURATION_MS = 900;

/**
 * "120 يومًا" that counts up from zero the first time it scrolls into view. The server markup already holds the final number
 * (no layout shift); people who asked for less motion keep it as it is.
 */
export function DaysCountUp({ days }: { days: number }) {
  const t = useTranslations("account.warranties");
  const element = useRef<HTMLSpanElement>(null);

  useLayoutEffect(() => {
    const span = element.current;
    const text = span?.firstChild;
    if (!span || !(text instanceof Text) || days < 1 || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    let frame = 0;
    const show = (value: number) => { text.data = t("daysCount", { days: value }); };
    const run = () => {
      const start = performance.now();
      const step = (now: number) => {
        const progress = Math.min(1, (now - start) / DURATION_MS);
        show(Math.round(days * (1 - (1 - progress) ** 3)));
        if (progress < 1) frame = requestAnimationFrame(step);
      };
      frame = requestAnimationFrame(step);
    };

    show(0);
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        observer.disconnect();
        run();
      }
    });
    observer.observe(span);
    return () => {
      observer.disconnect();
      cancelAnimationFrame(frame);
      show(days);
    };
  }, [days, t]);

  return <span ref={element}>{t("daysCount", { days })}</span>;
}
