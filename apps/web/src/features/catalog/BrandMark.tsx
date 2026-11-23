"use client";

import { useState } from "react";
import { mediaUrl } from "@/lib/media";

/**
 * A brand's logo on a small framed chip (the dark theme multiplies a white logo background into the same soft backdrop as a product photo).
 * The brand's name is always written next to it, so the picture is decorative (empty alt). A brand with no logo, or a logo that fails to load,
 * draws nothing at all: no empty frame and no gap.
 */
export function BrandMark({ logoStorageKey, size = "md", className = "" }: { logoStorageKey: string | null; size?: "sm" | "md"; className?: string }) {
  const [failed, setFailed] = useState(false);
  if (!logoStorageKey || failed) return null;
  return (
    <span className={`photo-frame flex shrink-0 items-center justify-center rounded-control border border-line bg-surface px-1.5 ${size === "sm" ? "h-6" : "h-8"}${className ? ` ${className}` : ""}`}>
      {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
      <img
        src={mediaUrl(logoStorageKey)} alt="" loading="lazy" onError={() => setFailed(true)}
        className={`w-auto max-w-20 object-contain ${size === "sm" ? "h-4" : "h-5"}`}
      />
    </span>
  );
}
