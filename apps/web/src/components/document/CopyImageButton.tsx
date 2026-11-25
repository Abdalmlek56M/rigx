"use client";

import { useState, type RefObject } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { CheckIcon, CopyIcon } from "@/components/ui/icons";
import { deliverPng, renderCardToPng, type PngDelivery } from "@/lib/document-image/render";
import { imageFileName } from "@/lib/document-image/svg";

interface CopyImageButtonProps {
  /** The document at its full size (a fixed-size paper, scaled to fit the screen: the picture is made from its real size). */
  target: RefObject<HTMLElement | null>;
  /** The picture's size; omit the height to take the document's own height (a document whose length depends on its lines). */
  width: number;
  height?: number;
  /** The file name when the picture is saved instead of copied: `rigx-<kind>-<label>.png`. */
  fileKind: string;
  fileLabel: string;
}

/** "نسخ كصورة": the document as a PNG on the clipboard (or saved as a file where the browser cannot copy a picture), next to the PDF. One button for the invoice and the quote. */
export function CopyImageButton({ target, width, height, fileKind, fileLabel }: CopyImageButtonProps) {
  const t = useTranslations("documentImage");
  const [state, setState] = useState<"idle" | "working" | PngDelivery | "failed">("idle");

  async function copy() {
    const element = target.current;
    if (!element) return;
    setState("working");
    try {
      setState(await deliverPng(renderCardToPng(element, width, height ?? element.offsetHeight), imageFileName(fileKind, fileLabel)));
    } catch {
      setState("failed");
    }
  }

  return (
    <>
      <Button type="button" variant="secondary" icon={state === "copied" ? CheckIcon : CopyIcon} loading={state === "working"} onClick={() => void copy()}>
        {t("copy")}
      </Button>
      <span role="status" className={`order-last basis-full text-end text-sm empty:sr-only ${state === "failed" ? "text-error-text" : "text-ink-muted"}`}>
        {state === "copied" || state === "saved" || state === "failed" ? t(state) : ""}
      </span>
    </>
  );
}
