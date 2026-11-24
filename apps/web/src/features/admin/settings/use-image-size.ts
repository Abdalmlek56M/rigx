"use client";

import { useEffect, useState } from "react";

interface Size {
  src: string;
  width: number;
  height: number;
}

/** The natural size of the image at `src`, once it has loaded; null before that, for no address, or when it fails to load. */
export function useImageSize(src: string | null): { width: number; height: number } | null {
  const [size, setSize] = useState<Size | null>(null);
  useEffect(() => {
    if (!src) return;
    let cancelled = false;
    const image = new Image();
    image.onload = () => {
      if (!cancelled && image.naturalWidth > 0 && image.naturalHeight > 0) setSize({ src, width: image.naturalWidth, height: image.naturalHeight });
    };
    image.src = src;
    return () => {
      cancelled = true;
    };
  }, [src]);
  return size && size.src === src ? { width: size.width, height: size.height } : null;
}
