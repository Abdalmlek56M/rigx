"use client";

import { useCallback, type ClipboardEvent } from "react";
import { imageFromItems } from "./clipboard-image";

/**
 * Props for the area around an image upload so that Ctrl+V (or a long-press paste) of a picture uploads it: the area takes the focus on
 * a click, and only a pasted picture is taken (a text paste into a field inside it behaves as usual). `disabled` while a save runs.
 */
export function useImagePaste(onImage: (file: File) => void, disabled = false) {
  const onPaste = useCallback((event: ClipboardEvent<HTMLElement>) => {
    const file = disabled ? null : imageFromItems(event.clipboardData.items);
    if (!file) return;
    event.preventDefault();
    onImage(file);
  }, [onImage, disabled]);
  return { tabIndex: 0, onPaste, "data-paste-target": "" } as const;
}
