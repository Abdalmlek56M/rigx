"use client";

import { useCallback, useEffect, useRef, useState, type CSSProperties, type PointerEvent, type TouchEvent } from "react";
import { useTranslations } from "next-intl";
import { IconButton } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { ArrowBackIcon, ArrowEndIcon, ExpandIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { FavouriteButton } from "@/features/favourites/FavouriteButton";
import { mediaUrl } from "@/lib/media";
import { GalleryViewer } from "./GalleryViewer";

export interface Photo {
  storageKey: string;
  thumbnailStorageKey: string;
}

const SWIPE_MIN = 40;

/**
 * The main photo in a frame that takes the photo's own shape (a wide card photo stays wide, a tall case photo stays tall), so the
 * whole photo shows with no empty edges; a row of square thumbnails, arrows and swipe, and a full-screen viewer with zoom. A new photo
 * fades in, drifting a little from the side it came from (`.gallery-photo-in`), and the thumbnail row keeps the chosen one in view.
 */
export function Gallery({ photos, alt, favourite }: { photos: readonly Photo[]; alt: string; favourite?: { code: string; modelName: string } }) {
  const t = useTranslations("product.gallery");
  const [selected, setSelected] = useState(0);
  /** Which way the last move went (1 = next, -1 = previous, 0 = none yet), for the side the photo drifts in from. */
  const [step, setStep] = useState(0);
  const [viewerOpen, setViewerOpen] = useState(false);
  const [zoomed, setZoomed] = useState(false);
  const mainButton = useRef<HTMLButtonElement>(null);
  const thumbnails = useRef<(HTMLButtonElement | null)[]>([]);
  const touchStart = useRef<number | null>(null);
  const current = photos[Math.min(selected, photos.length - 1)];
  const count = photos.length;

  const move = useCallback((by: number) => {
    if (count < 2) return;
    setStep(Math.sign(by));
    setSelected((index) => (index + by + count) % count);
  }, [count]);

  const choose = useCallback((index: number) => {
    setStep(Math.sign(index - selected));
    setSelected(index);
  }, [selected]);

  // The thumbnail row follows the chosen photo (only after a move, so opening the page never scrolls it).
  useEffect(() => {
    if (step === 0) return;
    const smooth = !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    thumbnails.current[selected]?.scrollIntoView({ block: "nearest", inline: "nearest", behavior: smooth ? "smooth" : "auto" });
  }, [selected, step]);

  function swipeStart(event: TouchEvent) {
    touchStart.current = event.touches[0]?.clientX ?? null;
  }

  function swipeEnd(event: TouchEvent) {
    const start = touchStart.current;
    touchStart.current = null;
    if (start === null) return;
    const end = event.changedTouches[0]?.clientX ?? start;
    if (Math.abs(end - start) < SWIPE_MIN) return;
    move(end < start ? 1 : -1);
  }

  /** The photo magnifier (mouse only, styled in globals.css) zooms around the pointer. */
  function aim(event: PointerEvent<HTMLButtonElement>) {
    const box = event.currentTarget.getBoundingClientRect();
    event.currentTarget.style.setProperty("--zoom-x", `${(((event.clientX - box.left) / box.width) * 100).toFixed(1)}%`);
    event.currentTarget.style.setProperty("--zoom-y", `${(((event.clientY - box.top) / box.height) * 100).toFixed(1)}%`);
  }

  function closeViewer() {
    const index = selected;
    setViewerOpen(false);
    window.setTimeout(() => (thumbnails.current[index] ?? mainButton.current)?.focus(), 0);
  }

  return (
    <div className="flex min-w-0 flex-col gap-3">
      <div
        className={`${cardClass({ padding: "none" })} relative mx-auto w-fit max-w-full overflow-hidden ${current ? "" : "aspect-square w-full"}`}
        onTouchStart={swipeStart}
        onTouchEnd={swipeEnd}
        style={{ touchAction: "pan-y" }}
      >
        {current ? (
          <button ref={mainButton} type="button" onClick={() => { setZoomed(false); setViewerOpen(true); }} aria-label={t("open")} onPointerMove={aim} className="zoom-photo group relative block">
            {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
            <img
              key={current.storageKey}
              src={mediaUrl(current.storageKey)}
              alt={alt}
              fetchPriority={selected === 0 ? "high" : "auto"}
              style={{ "--enter-step": step } as CSSProperties}
              className="product-photo gallery-photo-in block h-auto max-h-[min(78vh,760px)] w-auto max-w-full"
            />
            <span aria-hidden="true" className="absolute start-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-surface/90 text-orange-text shadow-soft transition-colors group-hover:bg-surface">
              <ExpandIcon size={18} />
            </span>
          </button>
        ) : (
          <div className="flex h-full items-center justify-center text-muted"><ImagePlaceholderIcon size={220} /></div>
        )}
        {favourite ? <FavouriteButton code={favourite.code} modelName={favourite.modelName} className="absolute end-3 top-3" /> : null}
        {count > 1 ? (
          <>
            <IconButton
              icon={ArrowBackIcon} label={t("previous")} variant="secondary" onClick={() => move(-1)}
              className="absolute start-3 top-1/2 -translate-y-1/2 shadow-soft"
            />
            <IconButton
              icon={ArrowEndIcon} label={t("next")} variant="secondary" onClick={() => move(1)}
              className="absolute end-3 top-1/2 -translate-y-1/2 shadow-soft"
            />
            <span dir="ltr" className="absolute bottom-3 start-1/2 -translate-x-1/2 rounded-full surface-strong px-2.5 py-1 font-mono text-xs">
              {selected + 1} / {count}
            </span>
          </>
        ) : null}
      </div>
      {count > 1 ? (
        <ul className="m-0 flex list-none gap-2 overflow-x-auto p-0 md:flex-wrap">
          {photos.map((photo, index) => (
            <li key={photo.storageKey} className="shrink-0">
              <button
                ref={(element) => { thumbnails.current[index] = element; }}
                type="button"
                aria-label={t("photo", { number: index + 1 })}
                aria-pressed={index === selected}
                onClick={() => choose(index)}
                className={`block aspect-square w-16 overflow-hidden rounded-control bg-surface transition-[border-color,opacity] duration-200 md:w-20 ${index === selected ? "border-2 border-orange" : "border border-line opacity-80 hover:border-ink hover:opacity-100"} motion-reduce:transition-none`}
              >
                {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={80} height={80} loading="lazy" className="product-photo h-full w-full object-cover" />
              </button>
            </li>
          ))}
        </ul>
      ) : null}

      <GalleryViewer
        open={viewerOpen}
        photos={photos}
        selected={selected}
        alt={alt}
        zoomed={zoomed}
        onZoom={setZoomed}
        onMove={move}
        onSelect={choose}
        onClose={closeViewer}
      />
    </div>
  );
}
