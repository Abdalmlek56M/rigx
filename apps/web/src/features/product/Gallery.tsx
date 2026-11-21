"use client";

import { useCallback, useRef, useState, type TouchEvent } from "react";
import { useTranslations } from "next-intl";
import { IconButton } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { ArrowBackIcon, ArrowEndIcon, ExpandIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { mediaUrl } from "@/lib/media";
import { GalleryViewer } from "./GalleryViewer";

export interface Photo {
  storageKey: string;
  thumbnailStorageKey: string;
}

const SWIPE_MIN = 40;

/** Main photo in a square frame, a row of square thumbnails, arrows and swipe, and a full-screen viewer with zoom. */
export function Gallery({ photos, alt }: { photos: readonly Photo[]; alt: string }) {
  const t = useTranslations("product.gallery");
  const [selected, setSelected] = useState(0);
  const [viewerOpen, setViewerOpen] = useState(false);
  const [zoomed, setZoomed] = useState(false);
  const mainButton = useRef<HTMLButtonElement>(null);
  const thumbnails = useRef<(HTMLButtonElement | null)[]>([]);
  const touchStart = useRef<number | null>(null);
  const current = photos[Math.min(selected, photos.length - 1)];
  const count = photos.length;

  const move = useCallback((step: number) => {
    if (count < 2) return;
    setSelected((index) => (index + step + count) % count);
  }, [count]);

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

  function closeViewer() {
    const index = selected;
    setViewerOpen(false);
    window.setTimeout(() => (thumbnails.current[index] ?? mainButton.current)?.focus(), 0);
  }

  return (
    <div className="flex min-w-0 flex-col gap-3">
      <div
        className={`${cardClass({ padding: "none" })} relative aspect-square w-full overflow-hidden`}
        onTouchStart={swipeStart}
        onTouchEnd={swipeEnd}
        style={{ touchAction: "pan-y" }}
      >
        {current ? (
          <button ref={mainButton} type="button" onClick={() => { setZoomed(false); setViewerOpen(true); }} aria-label={t("open")} className="group block h-full w-full cursor-zoom-in">
            {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
            <img src={mediaUrl(current.storageKey)} alt={alt} width={720} height={720} fetchPriority={selected === 0 ? "high" : "auto"} className="h-full w-full object-contain p-4" />
            <span aria-hidden="true" className="absolute end-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-surface/90 text-ink shadow-soft transition-colors group-hover:text-orange-text">
              <ExpandIcon size={18} />
            </span>
          </button>
        ) : (
          <div className="flex h-full items-center justify-center text-muted"><ImagePlaceholderIcon size={220} /></div>
        )}
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
                onClick={() => setSelected(index)}
                className={`block aspect-square w-16 overflow-hidden rounded-control bg-surface transition-colors duration-150 md:w-20 ${index === selected ? "border-2 border-ink" : "border border-line hover:border-ink"} motion-reduce:transition-none`}
              >
                {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={80} height={80} loading="lazy" className="h-full w-full object-contain p-1" />
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
        onSelect={setSelected}
        onClose={closeViewer}
      />
    </div>
  );
}
