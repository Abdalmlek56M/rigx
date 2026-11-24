"use client";

import { useId } from "react";
import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { placeWatermark, previewNumber } from "./preview-rules";
import { useImageSize } from "./use-image-size";

/** The reference photo the preview is measured on (any 4:3 photo gives the same picture: the logo's place is a share of it). */
const PHOTO = { width: 1200, height: 900 };

/** A drawn stand-in for a product photo: a graphics card on a dark studio backdrop. */
function SamplePhoto() {
  const light = useId();
  return (
    <svg viewBox="0 0 400 300" preserveAspectRatio="none" aria-hidden="true" className="absolute inset-0 size-full">
      <defs>
        <radialGradient id={light} cx="50%" cy="38%" r="70%">
          <stop offset="0" stopColor="white" stopOpacity="0.2" />
          <stop offset="1" stopColor="white" stopOpacity="0" />
        </radialGradient>
      </defs>
      <rect width="400" height="300" fill={`url(#${light})`} />
      <g fill="white" fillOpacity="0.16">
        <rect x="70" y="105" width="260" height="96" rx="10" />
        <rect x="70" y="201" width="150" height="8" rx="2" fillOpacity="0.1" />
      </g>
      <g fill="none" stroke="white" strokeOpacity="0.4" strokeWidth="3">
        <circle cx="140" cy="153" r="34" />
        <circle cx="260" cy="153" r="34" />
      </g>
      <g fill="white" fillOpacity="0.35">
        <circle cx="140" cy="153" r="8" />
        <circle cx="260" cy="153" r="8" />
      </g>
    </svg>
  );
}

/**
 * A photo with the watermark on it, drawn from the form as it is being edited (position, size, opacity, on or off), so a change shows before
 * the setting is saved. The logo is the saved watermark logo, or the store logo when there is none (what the server uses).
 */
export function WatermarkPreview({ logoKey, enabled, position, opacityText, sizeText }: {
  logoKey: string | null;
  enabled: boolean;
  position: string;
  opacityText: string;
  sizeText: string;
}) {
  const t = useTranslations("admin.settings.watermark.preview");
  const size = useImageSize(logoKey ? mediaUrl(logoKey) : null);
  const opacity = previewNumber(opacityText, 5, 100, 40);
  const share = previewNumber(sizeText, 5, 50, 20);
  const placed = enabled && logoKey && size ? placeWatermark(PHOTO.width, PHOTO.height, size.width, size.height, position, share) : null;
  const note = !logoKey ? t("noLogo") : !enabled ? t("off") : t("hint");

  return (
    <figure className="m-0 flex max-w-md flex-col gap-2">
      <figcaption className="type-label font-bold">{t("title")}</figcaption>
      <div dir="ltr" role="img" aria-label={t("alt")} className="relative aspect-[4/3] w-full overflow-hidden rounded-control border border-line bg-carbon">
        <SamplePhoto />
        {placed && logoKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img
            src={mediaUrl(logoKey)} alt=""
            className="absolute object-fill"
            style={{
              left: `${(placed.x / PHOTO.width) * 100}%`, top: `${(placed.y / PHOTO.height) * 100}%`,
              width: `${(placed.width / PHOTO.width) * 100}%`, height: `${(placed.height / PHOTO.height) * 100}%`, opacity: opacity / 100
            }}
          />
        ) : null}
      </div>
      <p className="m-0 text-xs text-ink-muted">{note}</p>
    </figure>
  );
}
