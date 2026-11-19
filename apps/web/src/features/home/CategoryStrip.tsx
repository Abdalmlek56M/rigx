"use client";

import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ChevronStartIcon } from "@/components/ui/icons";

/** Seconds between automatic steps; the strip stands still while the visitor hovers, touches or focuses it. */
const AUTO_STEP_MS = 3500;

interface CategoryStripProps {
  title: string;
  children: ReactNode;
}

/**
 * The home category cards as one moving strip: it scrolls a card at a time on its own, with left/right arrows,
 * swipe and keyboard. Auto-advance is off under reduced motion and pauses on hover, touch, focus or a hidden tab.
 */
export function CategoryStrip({ title, children }: CategoryStripProps) {
  const t = useTranslations("home.categories");
  const track = useRef<HTMLUListElement>(null);
  const paused = useRef(false);
  const [edges, setEdges] = useState({ left: false, right: true });

  /** Scroll bounds that hold in both directions (in RTL `scrollLeft` runs from 0 down to a negative number). */
  const bounds = useCallback(() => {
    const el = track.current;
    if (!el) return null;
    const max = el.scrollWidth - el.clientWidth;
    const rtl = getComputedStyle(el).direction === "rtl";
    return { el, min: rtl ? -max : 0, max: rtl ? 0 : max, rtl };
  }, []);

  const update = useCallback(() => {
    const b = bounds();
    if (b) setEdges({ left: b.el.scrollLeft > b.min + 2, right: b.el.scrollLeft < b.max - 2 });
  }, [bounds]);

  const step = useCallback((towards: "left" | "right") => {
    const b = bounds();
    if (!b) return;
    const card = b.el.querySelector("li");
    const distance = (card?.getBoundingClientRect().width ?? 180) + 12;
    b.el.scrollBy({ left: towards === "left" ? -distance : distance, behavior: "smooth" });
  }, [bounds]);

  useEffect(() => {
    const el = track.current;
    if (!el) return;
    // Fires once on observe as well, which sets the first arrow state.
    const observer = new ResizeObserver(update);
    observer.observe(el);
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return () => observer.disconnect();

    const timer = setInterval(() => {
      const b = bounds();
      if (!b || paused.current || document.hidden || b.max === b.min) return;
      // Reading order: forward is leftwards in Arabic, rightwards in English; at the end it returns to the start.
      const atEnd = b.rtl ? b.el.scrollLeft <= b.min + 2 : b.el.scrollLeft >= b.max - 2;
      if (atEnd) b.el.scrollTo({ left: 0, behavior: "smooth" });
      else step(b.rtl ? "left" : "right");
    }, AUTO_STEP_MS);
    return () => {
      clearInterval(timer);
      observer.disconnect();
    };
  }, [bounds, step, update]);

  const hold = () => {
    paused.current = true;
  };
  const release = () => {
    paused.current = false;
  };
  const arrow = "press flex h-10 w-10 items-center justify-center rounded-full border border-line bg-surface text-ink shadow-soft transition-[opacity,border-color,color] hover:border-orange hover:text-orange-text disabled:pointer-events-none disabled:opacity-35";

  return (
    <section
      aria-roledescription="carousel"
      aria-label={title}
      className="flex shell flex-col gap-5 pb-6 pt-10 md:pt-14"
      onPointerEnter={hold}
      onPointerLeave={release}
      onFocus={hold}
      onBlur={release}
      onTouchStart={hold}
    >
      <div className="flex items-center justify-between gap-4">
        <h2 className="m-0 text-[22px] font-bold md:text-[30px]">{title}</h2>
        <div className="flex gap-2" dir="ltr">
          <button type="button" aria-label={t("scrollLeft")} disabled={!edges.left} onClick={() => step("left")} className={arrow}>
            <ChevronStartIcon size={18} />
          </button>
          <button type="button" aria-label={t("scrollRight")} disabled={!edges.right} onClick={() => step("right")} className={arrow}>
            <span className="flex -scale-x-100"><ChevronStartIcon size={18} /></span>
          </button>
        </div>
      </div>
      <ul
        ref={track}
        onScroll={update}
        className="strip-fade m-0 flex snap-x snap-mandatory list-none gap-3 overflow-x-auto overscroll-x-contain scroll-smooth p-0 pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {children}
      </ul>
    </section>
  );
}
