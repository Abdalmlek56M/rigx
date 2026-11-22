"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { TrashIcon, UploadIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "./Button";
import { ConfirmDialog } from "./ConfirmDialog";
import { PasteImageButton } from "./PasteImageButton";
import { useToast } from "./Toast";
import { useImagePaste } from "./use-image-paste";

export interface ImageSlotText {
  title: string;
  hint: string;
  alt: string;
  upload: string;
  change: string;
  remove: string;
  removeTitle: string;
  removeBody: string;
  removed: string;
  failed: string;
  /** Toast after a successful upload (none when absent). */
  saved?: string;
}

/**
 * One picture that belongs to a record (a category picture, a brand or store logo): preview, upload or change, paste from the clipboard
 * and remove after a confirmation. The caller does the saving and returns when it is done; a failure shows `text.failed`.
 */
export function ImageSlot({ storageKey, canManage, text, onUpload, onRemove }: {
  storageKey: string | null;
  canManage: boolean;
  text: ImageSlotText;
  onUpload: (file: File) => Promise<void>;
  onRemove: () => Promise<void>;
}) {
  const toast = useToast();
  const tPaste = useTranslations("admin.kit.paste");
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [removing, setRemoving] = useState(false);

  async function upload(file: File | undefined) {
    if (!file || busy) return;
    setBusy(true);
    try {
      await onUpload(file);
      if (text.saved) toast(text.saved);
    } catch {
      toast(text.failed, "error");
    } finally {
      setBusy(false);
      if (input.current) input.current.value = "";
    }
  }

  async function remove() {
    setBusy(true);
    try {
      await onRemove();
      toast(text.removed);
    } catch {
      toast(text.failed, "error");
      throw new Error("remove failed");
    } finally {
      setBusy(false);
    }
  }

  const paste = useImagePaste((file) => void upload(file), busy || !canManage);
  if (!canManage && !storageKey) return null;
  return (
    <div className="flex min-w-0 flex-col gap-2 rounded-control border border-line bg-paper p-3 focus-visible:outline-2 focus-visible:outline-orange" {...(canManage ? paste : {})}>
      <h3 className="m-0 text-[15px] font-bold">{text.title}</h3>
      <div className="flex flex-wrap items-center gap-3">
        <span className="flex h-16 w-24 shrink-0 items-center justify-center overflow-hidden rounded-control bg-surface">
          {storageKey ? (
            // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
            <img src={mediaUrl(storageKey)} alt={text.alt} className="h-full w-full object-contain" />
          ) : null}
        </span>
        {canManage ? (
          <div className="flex flex-wrap gap-2">
            <label className={`${buttonClass()} cursor-pointer ${busy ? "pointer-events-none" : ""}`} aria-disabled={busy || undefined}>
              <UploadIcon size={buttonIconSize()} />
              {storageKey ? text.change : text.upload}
              <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={(event) => void upload(event.target.files?.[0])} className="sr-only" />
            </label>
            <PasteImageButton onImage={(file) => void upload(file)} disabled={busy} />
            {storageKey ? <Button variant="danger" icon={TrashIcon} disabled={busy} onClick={() => setRemoving(true)}>{text.remove}</Button> : null}
          </div>
        ) : null}
      </div>
      <p className="m-0 text-xs text-ink-muted">{text.hint}</p>
      {canManage ? <p className="m-0 text-xs text-ink-muted">{tPaste("hint")}</p> : null}
      <ConfirmDialog open={removing} title={text.removeTitle} body={text.removeBody} confirmLabel={text.remove} danger onClose={() => setRemoving(false)} onConfirm={remove} />
    </div>
  );
}
