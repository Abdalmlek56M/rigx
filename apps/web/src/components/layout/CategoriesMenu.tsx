"use client";

import { useEffect, useRef, useState, type PointerEvent } from "react";
import { useTranslations } from "next-intl";
import { CategoryIcon } from "@/components/ui/category-icon";
import { ChevronDownIcon, ChevronStartIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import type { MenuCategory } from "@/lib/storefront";
import { categoryHref } from "./nav-links";

/** Hover intent: open after a short rest on the trigger, close after a short grace period so the pointer can cross into the panel. */
const OPEN_DELAY = 90;
const CLOSE_DELAY = 220;

/**
 * "كل الأقسام" mega menu: a full-width panel under the header. Main categories with their icons on the start side,
 * the hovered (or focused) category's sub-categories beside them. Opens on hover with a mouse, on click or tap otherwise;
 * Escape or a press outside closes it.
 */
export function CategoriesMenu({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("header");
  const [open, setOpen] = useState(false);
  const [activeSlug, setActiveSlug] = useState<string | null>(null);
  const ref = useRef<HTMLDivElement>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearTimer = () => {
    if (timer.current) clearTimeout(timer.current);
    timer.current = null;
  };
  const close = () => {
    clearTimer();
    setOpen(false);
  };
  useDismiss(ref, open, close);
  useEffect(() => clearTimer, []);

  const active = categories.find((category) => category.slug === activeSlug) ?? categories[0];
  if (!active) return null;

  const later = (action: () => void, delay: number) => {
    clearTimer();
    timer.current = setTimeout(action, delay);
  };
  const onEnter = (event: PointerEvent) => {
    if (event.pointerType === "mouse") later(() => setOpen(true), OPEN_DELAY);
  };
  const onLeave = (event: PointerEvent) => {
    if (event.pointerType === "mouse") later(() => setOpen(false), CLOSE_DELAY);
  };

  return (
    <div ref={ref} onPointerEnter={onEnter} onPointerLeave={onLeave}>
      <button
        type="button"
        aria-expanded={open}
        aria-controls="categories-panel"
        onClick={() => {
          clearTimer();
          setOpen((value) => !value);
        }}
        className="flex min-h-11 items-center gap-1 rounded-full px-3 text-[15px] font-medium transition-colors hover:bg-paper aria-expanded:bg-paper"
      >
        {t("allCategories")}
        <span className="transition-transform duration-200 [[aria-expanded=true]>&]:rotate-180"><ChevronDownIcon size={16} /></span>
      </button>
      {open ? (
        <div id="categories-panel" className="mega-in absolute inset-x-0 top-full z-30 border-b border-line bg-surface text-ink shadow-lift">
          <div className="shell grid grid-cols-[260px_minmax(0,1fr)] gap-6 py-5">
            <ul className="m-0 flex max-h-[min(64vh,520px)] list-none flex-col gap-0.5 overflow-y-auto overscroll-contain border-e border-line p-0 pe-4">
              {categories.map((category) => {
                const current = category.slug === active.slug;
                return (
                  <li key={category.slug}>
                    <Link
                      href={categoryHref(category.slug)}
                      onClick={close}
                      onPointerEnter={() => setActiveSlug(category.slug)}
                      onFocus={() => setActiveSlug(category.slug)}
                      aria-current={current ? "true" : undefined}
                      className={`group flex min-h-11 items-center gap-3 rounded-card px-2.5 text-[15px] font-bold transition-colors ${current ? "bg-paper text-ink" : "text-ink-muted hover:text-ink"}`}
                    >
                      <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full transition-colors ${current ? "bg-orange text-on-orange" : "bg-orange-soft text-orange-text"}`}>
                        <CategoryIcon slug={category.slug} size={18} />
                      </span>
                      <span className="min-w-0 flex-1 truncate">{category.name}</span>
                      {category.children.length > 0 ? (
                        <span aria-hidden="true" className={`flex transition-opacity ltr:-scale-x-100 ${current ? "opacity-100 text-orange-text" : "opacity-0"}`}>
                          <ChevronStartIcon size={16} />
                        </span>
                      ) : null}
                    </Link>
                  </li>
                );
              })}
            </ul>
            <div key={active.slug} className="mega-pane flex min-w-0 flex-col gap-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h2 className="m-0 text-xl font-bold">{active.name}</h2>
                <Link href={categoryHref(active.slug)} onClick={close} className="press inline-flex min-h-10 items-center gap-1.5 rounded-full border border-input-line px-4 text-sm font-bold transition-colors hover:border-orange hover:text-orange-text">
                  {t("seeAll", { name: active.name })}
                  <span aria-hidden="true" className="flex ltr:-scale-x-100"><ChevronStartIcon size={16} /></span>
                </Link>
              </div>
              {active.children.length > 0 ? (
                <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(200px,1fr))] gap-2 p-0">
                  {active.children.map((child) => (
                    <li key={child.slug}>
                      <Link
                        href={categoryHref(child.slug)}
                        onClick={close}
                        className="path-tile press flex min-h-14 flex-col justify-center gap-0.5 rounded-card border border-line px-4 py-2.5 transition-colors hover:border-orange/60 hover:bg-paper"
                      >
                        <b className="text-[15px]">{child.name}</b>
                        {child.children.length > 0 ? (
                          <span className="truncate text-xs text-ink-muted">{child.children.map((grandchild) => grandchild.name).join(" · ")}</span>
                        ) : null}
                      </Link>
                    </li>
                  ))}
                </ul>
              ) : null}
              <div className="flex flex-wrap gap-2 border-t border-line pt-4">
                {(["New", "Used"] as const).map((condition) => (
                  <Link
                    key={condition}
                    href={`${categoryHref(active.slug)}&condition=${condition}`}
                    onClick={close}
                    className="press flex min-h-12 flex-1 basis-[200px] items-center justify-between gap-3 rounded-card bg-paper px-4 font-bold transition-colors hover:bg-orange-soft hover:text-orange-text"
                  >
                    {t(condition === "New" ? "conditionNew" : "conditionUsed", { name: active.name })}
                    <span aria-hidden="true" className="flex ltr:-scale-x-100"><ChevronStartIcon size={16} /></span>
                  </Link>
                ))}
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
