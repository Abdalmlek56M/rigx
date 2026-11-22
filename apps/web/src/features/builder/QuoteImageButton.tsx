"use client";

import { useState, type RefObject } from "react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { CheckIcon, CopyIcon } from "@/components/ui/icons";
import { deliverPng, renderCardToPng, type PngDelivery } from "./quote-image";
import { quoteFileName } from "./quote-image-svg";

interface QuoteImageButtonProps {
  /** The quote card, at its full size. */
  card: RefObject<HTMLElement | null>;
  width: number;
  height: number;
  /** What the saved file is named after (the quote's title). */
  fileLabel: string;
}

/** "نسخ كصورة": the quote card as a PNG on the clipboard (or saved as a file where the browser cannot copy a picture), next to the PDF. */
export function QuoteImageButton({ card, width, height, fileLabel }: QuoteImageButtonProps) {
  const t = useTranslations("builder.quote");
  const [state, setState] = useState<"idle" | "working" | PngDelivery | "failed">("idle");

  async function copy() {
    if (!card.current) return;
    setState("working");
    try {
      setState(await deliverPng(renderCardToPng(card.current, width, height), quoteFileName(fileLabel)));
    } catch {
      setState("failed");
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-3">
      <Button type="button" variant="secondary" icon={state === "copied" ? CheckIcon : CopyIcon} loading={state === "working"} onClick={() => void copy()}>
        {t("copyImage")}
      </Button>
      <span role="status" className={`text-sm ${state === "failed" ? "text-error-text" : "text-ink-muted"}`}>
        {state === "copied" || state === "saved" || state === "failed" ? t(`image.${state}`) : ""}
      </span>
    </div>
  );
}
