"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { mediaUrl } from "@/lib/media";

export interface Photo {
  storageKey: string;
  thumbnailStorageKey: string;
}

/** Main photo with a thumbnail row; without photos, the outline placeholder. */
export function Gallery({ photos, alt }: { photos: Photo[]; alt: string }) {
  const t = useTranslations("product.gallery");
  const [selected, setSelected] = useState(0);
  const current = photos[selected];

  return (
    <div className="flex min-w-0 flex-col gap-3">
      <div className="relative flex h-[300px] items-center justify-center rounded-card border border-line bg-surface text-muted md:h-[430px]">
        {current ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(current.storageKey)} alt={alt} width={640} height={430} fetchPriority={selected === 0 ? "high" : "auto"} className="h-full w-full rounded-card object-contain" />
        ) : (
          <ImagePlaceholderIcon size={220} />
        )}
        {photos.length > 1 ? (
          <span className="absolute bottom-3 start-1/2 -translate-x-1/2 rounded-full bg-carbon px-2.5 py-1 font-mono text-xs text-on-carbon md:hidden" dir="ltr">
            {selected + 1} / {photos.length}
          </span>
        ) : null}
      </div>
      {photos.length > 1 ? (
        <ul className="m-0 flex list-none gap-2 overflow-x-auto p-0 md:flex-wrap">
          {photos.map((photo, index) => (
            <li key={photo.storageKey} className="shrink-0">
              <button
                type="button"
                aria-label={t("photo", { number: index + 1 })}
                aria-pressed={index === selected}
                onClick={() => setSelected(index)}
                className={`block h-14 w-16 overflow-hidden rounded-control bg-surface md:h-[88px] md:w-[104px] ${index === selected ? "border-2 border-ink" : "border border-line"}`}
              >
                {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={104} height={88} loading="lazy" className="h-full w-full object-cover" />
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
