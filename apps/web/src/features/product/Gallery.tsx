"use client";

import { useCallback, useEffect, useRef, useState, type TouchEvent } from "react";
import { useTranslations } from "next-intl";
import {
  ArrowBackIcon, ArrowEndIcon, CloseIcon, ExpandIcon, ImagePlaceholderIcon, ZoomInIcon, ZoomOutIcon
} from "@/components/ui/icons";
import { mediaUrl } from "@/lib/media";

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
  const dialog = useRef<HTMLDialogElement>(null);
  const mainButton = useRef<HTMLButtonElement>(null);
  const thumbnails = useRef<(HTMLButtonElement | null)[]>([]);
  const touchStart = useRef<number | null>(null);
  const current = photos[Math.min(selected, photos.length - 1)];
  const count = photos.length;

  const move = useCallback((step: number) => {
    if (count < 2) return;
    setSelected((index) => (index + step + count) % count);
  }, [count]);

  useEffect(() => {
    const element = dialog.current;
    if (!element) return;
    if (viewerOpen && !element.open) element.showModal();
    if (!viewerOpen && element.open) element.close();
  }, [viewerOpen]);

  useEffect(() => {
    if (!viewerOpen) return;
    function onKey(event: KeyboardEvent) {
      if (event.key === "ArrowRight") {
        event.preventDefault();
        move(1);
      }
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        move(-1);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [viewerOpen, move]);

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
        className="relative aspect-square w-full overflow-hidden rounded-card border border-line bg-surface"
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
            <button
              type="button" aria-label={t("previous")} onClick={() => move(-1)}
              className="absolute start-3 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full border border-line bg-surface/90 text-ink shadow-soft transition-colors hover:text-orange-text"
            >
              <ArrowBackIcon size={20} />
            </button>
            <button
              type="button" aria-label={t("next")} onClick={() => move(1)}
              className="absolute end-3 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full border border-line bg-surface/90 text-ink shadow-soft transition-colors hover:text-orange-text"
            >
              <ArrowEndIcon size={20} />
            </button>
            <span dir="ltr" className="absolute bottom-3 start-1/2 -translate-x-1/2 rounded-full bg-carbon px-2.5 py-1 font-mono text-xs text-on-carbon">
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

      <dialog
        ref={dialog}
        aria-label={t("viewer")}
        onClose={() => setViewerOpen(false)}
        onCancel={(event) => {
          event.preventDefault();
          closeViewer();
        }}
        onClick={(event) => {
          if (event.target === event.currentTarget) closeViewer();
        }}
        className="fixed inset-0 m-0 h-dvh max-h-none w-screen max-w-none border-0 bg-carbon/95 p-0 text-white backdrop:bg-carbon/90"
      >
        {current ? (
          <div className="flex h-full flex-col">
            <div className="flex items-center justify-between gap-3 p-4">
              <span dir="ltr" className="font-mono text-sm">{selected + 1} / {count}</span>
              <div className="flex items-center gap-2">
                <button
                  type="button" aria-label={zoomed ? t("zoomOut") : t("zoomIn")} aria-pressed={zoomed} onClick={() => setZoomed((value) => !value)}
                  className="flex h-11 w-11 items-center justify-center rounded-full border border-white/25 text-white transition-colors hover:bg-white/10"
                >
                  {zoomed ? <ZoomOutIcon size={20} /> : <ZoomInIcon size={20} />}
                </button>
                <button
                  type="button" aria-label={t("close")} onClick={closeViewer}
                  className="flex h-11 w-11 items-center justify-center rounded-full border border-white/25 text-white transition-colors hover:bg-white/10"
                >
                  <CloseIcon size={20} />
                </button>
              </div>
            </div>
            <div className="relative min-h-0 flex-1" onTouchStart={swipeStart} onTouchEnd={swipeEnd}>
              <button
                type="button" onClick={() => setZoomed((value) => !value)} aria-label={zoomed ? t("zoomOut") : t("zoomIn")}
                className="flex h-full w-full items-center justify-center overflow-auto"
                style={{ touchAction: "pinch-zoom" }}
              >
                {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                <img
                  src={mediaUrl(current.storageKey)} alt={alt} className={`max-h-full max-w-full object-contain transition-transform duration-200 motion-reduce:transition-none ${zoomed ? "scale-[2]" : "scale-100"}`}
                />
              </button>
              {count > 1 ? (
                <>
                  <button
                    type="button" aria-label={t("previous")} onClick={() => move(-1)}
                    className="absolute start-3 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/25 bg-carbon/60 text-white transition-colors hover:bg-carbon"
                  >
                    <ArrowBackIcon size={22} />
                  </button>
                  <button
                    type="button" aria-label={t("next")} onClick={() => move(1)}
                    className="absolute end-3 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/25 bg-carbon/60 text-white transition-colors hover:bg-carbon"
                  >
                    <ArrowEndIcon size={22} />
                  </button>
                </>
              ) : null}
            </div>
            {count > 1 ? (
              <ul className="m-0 flex list-none justify-center gap-2 overflow-x-auto p-4">
                {photos.map((photo, index) => (
                  <li key={photo.storageKey} className="shrink-0">
                    <button
                      type="button" aria-label={t("photo", { number: index + 1 })} aria-pressed={index === selected}
                      onClick={() => { setSelected(index); setZoomed(false); }}
                      className={`block aspect-square w-14 overflow-hidden rounded-control bg-white/5 ${index === selected ? "border-2 border-white" : "border border-white/25"}`}
                    >
                      {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                      <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" className="h-full w-full object-contain p-1" />
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        ) : null}
      </dialog>
    </div>
  );
}
