"use client";

import { useEffect, useRef, useState, type PointerEvent } from "react";
import { useLocale, useTranslations } from "next-intl";
import { CategoryIcon } from "@/components/ui/category-icon";
import { ChevronDownIcon, ChevronStartIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import type { MenuCategory } from "@/lib/storefront";
import { conditionLinks } from "./mega-menu-rules";
import { categoryHref } from "./nav-links";
import { useCategoryStock, type CategoryStock } from "./use-category-stock";

/** The hovered category's newest available items; placeholders while loading, nothing when it has none. */
function FeaturedItems({ stock, onPick }: { stock: CategoryStock | null | undefined; onPick: () => void }) {
  const t = useTranslations("header");
  const locale = useLocale();
  if (stock === null || stock?.items.length === 0) return null;

  return (
    <div className="mt-auto flex flex-col gap-2 border-t border-line pt-3">
      <h3 className="m-0 text-xs font-bold text-ink-muted">{t("megaLatest")}</h3>
      <ul className="m-0 grid list-none grid-cols-3 gap-2 p-0">
        {stock === undefined
          ? [0, 1, 2].map((index) => <li key={index} aria-hidden="true" className="h-[132px] animate-pulse rounded-control bg-paper" />)
          : stock.items.map((item) => (
              <li key={item.code} className="flex">
                <Link href={productPath(item.slug)} onClick={onPick} className="press group flex w-full flex-col gap-1.5 rounded-control border border-line p-2 transition-colors hover:border-orange/60">
                  <span className="flex h-16 items-center justify-center overflow-hidden rounded-[6px] bg-paper text-muted">
                    {item.thumbnailStorageKey ? (
                      // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                      <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={96} height={64} loading="lazy" className="h-full w-full object-contain p-1" />
                    ) : (
                      <ImagePlaceholderIcon />
                    )}
                  </span>
                  <span dir="ltr" className="line-clamp-2 text-start text-xs font-bold leading-snug [unicode-bidi:isolate]">{item.modelName}</span>
                  <span className="mt-auto text-[13px] font-bold">{formatEgp(item.prices.alone, locale)}</span>
                </Link>
              </li>
            ))}
      </ul>
    </div>
  );
}

/** Hover intent: open after a short rest on the trigger, close after a short grace period so the pointer can cross into the panel. */
const OPEN_DELAY = 90;
const CLOSE_DELAY = 220;

/**
 * "كل الأقسام" mega menu: a compact panel under the trigger. Main categories with their icons on the start side; beside them
 * the hovered (or focused) category's new/used links (only where it has used stock), its sub-categories and its newest items.
 * Opens on hover with a mouse, on click or tap otherwise; Escape or a press outside closes it.
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
  const stock = useCategoryStock(active?.id ?? "", open && active !== undefined);
  if (!active) return null;
  const links = conditionLinks(stock?.counts ?? null);

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
    <div ref={ref} onPointerEnter={onEnter} onPointerLeave={onLeave} className="relative">
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
        <div id="categories-panel" className="mega-in absolute start-0 top-full z-30 mt-2 w-[min(760px,calc(100vw-48px))] rounded-panel border border-line bg-surface text-ink shadow-lift">
          <div className="grid grid-cols-[220px_minmax(0,1fr)] gap-4 p-3">
            <ul className="m-0 flex max-h-[min(60vh,420px)] list-none flex-col gap-0.5 overflow-y-auto overscroll-contain border-e border-line p-0 pe-3">
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
                      className={`group flex min-h-10 items-center gap-2.5 rounded-control px-2 text-sm font-bold transition-colors ${current ? "bg-paper text-ink" : "text-ink-muted hover:text-ink"}`}
                    >
                      <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full transition-colors ${current ? "accent-strong" : "bg-orange-soft text-orange-text"}`}>
                        <CategoryIcon slug={category.slug} size={16} />
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
            <div key={active.slug} className="mega-pane flex min-w-0 flex-col gap-3 py-1">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h2 className="m-0 text-lg font-bold">{active.name}</h2>
                <Link href={categoryHref(active.slug)} onClick={close} className="press inline-flex min-h-9 items-center gap-1.5 rounded-full border border-input-line px-3.5 text-[13px] font-bold transition-colors hover:border-orange hover:text-orange-text">
                  {t("seeAll", { name: active.name })}
                  <span aria-hidden="true" className="flex ltr:-scale-x-100"><ChevronStartIcon size={16} /></span>
                </Link>
              </div>
              {links.length > 0 ? (
                <div className="flex flex-wrap gap-1.5">
                  {links.map((condition) => (
                    <Link
                      key={condition}
                      href={`${categoryHref(active.slug)}&condition=${condition}`}
                      onClick={close}
                      className="press flex min-h-10 flex-1 basis-[150px] items-center justify-between gap-2 rounded-control bg-orange-soft px-3 text-sm font-bold text-orange-text transition-[filter] hover:brightness-[0.97]"
                    >
                      {t(condition === "New" ? "conditionNew" : "conditionUsed", { name: active.name })}
                      <span aria-hidden="true" className="flex ltr:-scale-x-100"><ChevronStartIcon size={16} /></span>
                    </Link>
                  ))}
                </div>
              ) : null}
              {active.children.length > 0 ? (
                <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-1.5 p-0">
                  {active.children.map((child) => (
                    <li key={child.slug}>
                      <Link
                        href={categoryHref(child.slug)}
                        onClick={close}
                        className="press flex min-h-11 flex-col justify-center gap-0.5 rounded-control border border-line px-3 py-2 transition-colors hover:border-orange/60 hover:bg-paper"
                      >
                        <b className="text-sm">{child.name}</b>
                        {child.children.length > 0 ? (
                          <span className="truncate text-xs text-ink-muted">{child.children.map((grandchild) => grandchild.name).join(" · ")}</span>
                        ) : null}
                      </Link>
                    </li>
                  ))}
                </ul>
              ) : null}
              <FeaturedItems stock={stock} onPick={close} />
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
