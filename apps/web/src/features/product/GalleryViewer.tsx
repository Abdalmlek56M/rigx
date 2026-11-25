"use client";

import { useEffect, useRef, type TouchEvent } from "react";
import { useTranslations } from "next-intl";
import { ArrowBackIcon, ArrowEndIcon, CloseIcon, ZoomInIcon, ZoomOutIcon } from "@/components/ui/icons";
import { mediaUrl } from "@/lib/media";
import type { Photo } from "./Gallery";

const SWIPE_MIN = 40;

interface GalleryViewerProps {
  open: boolean;
  photos: readonly Photo[];
  selected: number;
  alt: string;
  zoomed: boolean;
  onZoom: (zoomed: boolean) => void;
  onMove: (step: number) => void;
  onSelect: (index: number) => void;
  onClose: () => void;
}

/** The full-screen photo viewer: swipe or arrows, zoom, Escape, and a thumbnail strip. Rendered by `Gallery`. */
export function GalleryViewer({ open, photos, selected, alt, zoomed, onZoom, onMove, onSelect, onClose }: GalleryViewerProps) {
  const t = useTranslations("product.gallery");
  const dialog = useRef<HTMLDialogElement>(null);
  const touchStart = useRef<number | null>(null);
  const current = photos[Math.min(selected, photos.length - 1)];
  const count = photos.length;

  useEffect(() => {
    const element = dialog.current;
    if (!element) return;
    if (open && !element.open) element.showModal();
    if (!open && element.open) element.close();
  }, [open]);

  useEffect(() => {
    if (!open) return;
    function onKey(event: KeyboardEvent) {
      if (event.key === "ArrowRight") {
        event.preventDefault();
        onMove(1);
      }
      if (event.key === "ArrowLeft") {
        event.preventDefault();
        onMove(-1);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onMove]);

  function swipeStart(event: TouchEvent) {
    touchStart.current = event.touches[0]?.clientX ?? null;
  }

  function swipeEnd(event: TouchEvent) {
    const start = touchStart.current;
    touchStart.current = null;
    if (start === null) return;
    const end = event.changedTouches[0]?.clientX ?? start;
    if (Math.abs(end - start) < SWIPE_MIN) return;
    onMove(end < start ? 1 : -1);
  }

  return (
    <dialog
      ref={dialog}
      aria-label={t("viewer")}
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
      className="fixed inset-0 m-0 h-dvh max-h-none w-screen max-w-none border-0 bg-chrome/95 p-0 text-white backdrop:bg-black/90"
    >
      {current ? (
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between gap-3 p-4">
            <span dir="ltr" className="font-mono text-sm">{selected + 1} / {count}</span>
            <div className="flex items-center gap-2">
              <button
                type="button" aria-label={zoomed ? t("zoomOut") : t("zoomIn")} aria-pressed={zoomed} onClick={() => onZoom(!zoomed)}
                className="flex h-11 w-11 items-center justify-center rounded-full border border-white/25 text-white transition-colors hover:bg-white/10"
              >
                {zoomed ? <ZoomOutIcon size={20} /> : <ZoomInIcon size={20} />}
              </button>
              <button
                type="button" aria-label={t("close")} onClick={onClose}
                className="flex h-11 w-11 items-center justify-center rounded-full border border-white/25 text-white transition-colors hover:bg-white/10"
              >
                <CloseIcon size={20} />
              </button>
            </div>
          </div>
          <div className="relative min-h-0 flex-1" onTouchStart={swipeStart} onTouchEnd={swipeEnd}>
            <button
              type="button" onClick={() => onZoom(!zoomed)} aria-label={zoomed ? t("zoomOut") : t("zoomIn")}
              className="flex h-full w-full items-center justify-center overflow-auto"
              style={{ touchAction: "pinch-zoom" }}
            >
              {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
              <img
                key={current.storageKey}
                src={mediaUrl(current.storageKey)} alt={alt} className={`gallery-photo-in max-h-full max-w-full object-contain transition-transform duration-200 motion-reduce:transition-none ${zoomed ? "scale-[2]" : "scale-100"}`}
              />
            </button>
            {count > 1 ? (
              <>
                <button
                  type="button" aria-label={t("previous")} onClick={() => onMove(-1)}
                  className="absolute start-3 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/25 bg-chrome/60 text-white transition-colors hover:bg-chrome"
                >
                  <ArrowBackIcon size={22} />
                </button>
                <button
                  type="button" aria-label={t("next")} onClick={() => onMove(1)}
                  className="absolute end-3 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full border border-white/25 bg-chrome/60 text-white transition-colors hover:bg-chrome"
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
                    onClick={() => { onSelect(index); onZoom(false); }}
                    className={`block aspect-square w-14 overflow-hidden rounded-control bg-white/5 ${index === selected ? "border-2 border-white" : "border border-white/25"}`}
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                    <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" className="product-photo h-full w-full object-cover" />
                  </button>
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      ) : null}
    </dialog>
  );
}
