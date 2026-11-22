"use client";

import { useCallback, useEffect, useRef, useState, type MouseEvent, type PointerEvent, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ChevronStartIcon } from "@/components/ui/icons";

/** Milliseconds between automatic steps; the rail stands still while the visitor hovers, touches, drags or focuses it. */
const AUTO_STEP_MS = 3500;
/** Pointer travel (px) after which a press counts as a drag, so the card under it is not opened on release. */
const DRAG_THRESHOLD = 6;

interface RailProps {
  title: string;
  /** Optional link beside the arrows (e.g. "شوف الكل"). */
  action?: ReactNode;
  /** Step forward on its own (off under reduced motion). */
  autoAdvance?: boolean;
  /** Keep the arrow buttons on phones too (the related rails); home rails stay swipe-only there. */
  arrowsOnMobile?: boolean;
  children: ReactNode;
}

/**
 * A horizontal home rail of `<li>` cards: swipe on touch, drag with the mouse, arrows and keyboard. Scroll-snap settles
 * the cards after a drag; a drag never opens the card it started on. Auto-advance pauses on hover, touch, focus or a hidden tab.
 */
export function Rail({ title, action, autoAdvance = false, arrowsOnMobile = false, children }: RailProps) {
  const t = useTranslations("home.rail");
  const track = useRef<HTMLUListElement>(null);
  const paused = useRef(false);
  const drag = useRef<{ x: number; left: number; moved: boolean } | null>(null);
  const [dragging, setDragging] = useState(false);
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
    if (!autoAdvance || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return () => observer.disconnect();

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
  }, [autoAdvance, bounds, step, update]);

  // Mouse drag only: touch and pen already scroll natively.
  const onPointerDown = (event: PointerEvent<HTMLUListElement>) => {
    if (event.pointerType !== "mouse" || event.button !== 0 || !track.current) return;
    drag.current = { x: event.clientX, left: track.current.scrollLeft, moved: false };
  };
  const onPointerMove = (event: PointerEvent<HTMLUListElement>) => {
    const state = drag.current;
    const el = track.current;
    if (!state || !el) return;
    const dx = event.clientX - state.x;
    if (!state.moved) {
      if (Math.abs(dx) < DRAG_THRESHOLD) return;
      state.moved = true;
      el.setPointerCapture(event.pointerId);
      setDragging(true);
    }
    el.scrollLeft = state.left - dx;
  };
  const endDrag = (event: PointerEvent<HTMLUListElement>) => {
    if (!drag.current) return;
    if (track.current?.hasPointerCapture(event.pointerId)) track.current.releasePointerCapture(event.pointerId);
    // Keep `moved` until the click that follows this release has been swallowed.
    if (!drag.current.moved) drag.current = null;
    setDragging(false);
  };
  const onClickCapture = (event: MouseEvent) => {
    if (drag.current?.moved) {
      event.preventDefault();
      event.stopPropagation();
    }
    drag.current = null;
  };

  const hold = () => {
    paused.current = true;
  };
  const release = () => {
    paused.current = false;
  };
  const arrow = "press flex h-10 w-10 items-center justify-center rounded-full border border-line bg-surface text-ink shadow-soft transition-[opacity,border-color,color] hover:border-orange hover:text-orange-text disabled:pointer-events-none disabled:bg-paper disabled:text-muted disabled:shadow-none";

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
      <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
        <h2 className="m-0 type-display-title">{title}</h2>
        <div className="ms-auto flex items-center gap-3">
          {action}
          <div className={`flex gap-2 ${arrowsOnMobile ? "" : "max-sm:hidden"}`} dir="ltr">
            <button type="button" aria-label={t("scrollLeft")} disabled={!edges.left} onClick={() => step("left")} className={arrow}>
              <ChevronStartIcon size={18} />
            </button>
            <button type="button" aria-label={t("scrollRight")} disabled={!edges.right} onClick={() => step("right")} className={arrow}>
              <span className="flex -scale-x-100"><ChevronStartIcon size={18} /></span>
            </button>
          </div>
        </div>
      </div>
      <ul
        ref={track}
        onScroll={update}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={endDrag}
        onPointerCancel={endDrag}
        onClickCapture={onClickCapture}
        onDragStart={(event) => event.preventDefault()}
        data-dragging={dragging || undefined}
        className="rail strip-fade m-0 flex list-none gap-3 overflow-x-auto overscroll-x-contain p-0 pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {children}
      </ul>
    </section>
  );
}
