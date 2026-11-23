"use client";

import { useState, type DragEvent } from "react";
import { useTranslations } from "next-intl";
import { CloseIcon, UploadIcon } from "@/components/ui/icons";
import { PASTE_IMAGE_TYPES } from "./clipboard-image";
import { buttonClass, buttonIconSize, IconButton } from "./Button";
import { PasteImageButton } from "./PasteImageButton";
import { useImagePaste } from "./use-image-paste";

/**
 * Chooses one picture that is saved later with its form (an expense receipt): a button instead of the browser's own file input, a
 * dashed area that takes a dropped picture, a paste (Ctrl+V, or the paste button where the browser allows it), the chosen file's name
 * and a way to clear it. Only png, jpeg and webp are taken.
 */
export function ImageDropPicker({ label, hint, file, onFile }: { label: string; hint: string; file: File | null; onFile: (file: File | null) => void }) {
  const t = useTranslations("admin.kit.picker");
  const [over, setOver] = useState(false);
  const paste = useImagePaste(onFile);

  function drop(event: DragEvent<HTMLElement>) {
    event.preventDefault();
    setOver(false);
    const dropped = Array.from(event.dataTransfer.files).find((candidate) => (PASTE_IMAGE_TYPES as readonly string[]).includes(candidate.type));
    if (dropped) onFile(dropped);
  }

  return (
    <div
      {...paste}
      onDragOver={(event) => { event.preventDefault(); setOver(true); }} onDragLeave={() => setOver(false)} onDrop={drop}
      className={`flex flex-col gap-2 rounded-control border border-dashed p-3 focus-visible:outline-2 focus-visible:outline-orange ${over ? "border-orange bg-orange-soft" : "border-input-line bg-paper"}`}
    >
      <span className="text-[13px] font-bold">{label}</span>
      <div className="flex flex-wrap items-center gap-2">
        <label className={`${buttonClass()} cursor-pointer`}>
          <UploadIcon size={buttonIconSize()} />
          {file ? t("change") : t("choose")}
          <input
            type="file" accept="image/jpeg,image/png,image/webp" className="sr-only"
            onChange={(event) => { onFile(event.target.files?.[0] ?? null); event.target.value = ""; }}
          />
        </label>
        <PasteImageButton onImage={onFile} />
        {file ? (
          <span className="flex min-w-0 items-center gap-1 text-sm">
            <span dir="ltr" className="min-w-0 truncate">{file.name}</span>
            <IconButton icon={CloseIcon} label={t("clear")} size="sm" onClick={() => onFile(null)} />
          </span>
        ) : null}
      </div>
      <span className="text-xs text-ink-muted">{hint} {t("dropHint")}</span>
    </div>
  );
}
