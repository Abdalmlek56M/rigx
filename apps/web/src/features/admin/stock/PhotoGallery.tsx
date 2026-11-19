"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import type { Schemas } from "@rigx/api-client";
import { PlusIcon, TrashIcon } from "@/components/ui/icons";
import { IconButton, buttonClass, buttonIconSize } from "../kit/Button";

type Photo = Schemas["StockItemPhoto"];

/**
 * The photos of a stock item or a build: upload and remove. The server's own message (for example the minimum photo count to publish) is shown as it comes.
 * `onChanged` runs after every change so the owner can take the fresh copy.
 */
export function PhotoGallery({ photos, name, canManage, upload: uploadPhoto, remove, onChanged }: {
  photos: readonly Photo[];
  /** What the photos show (the model or build name), for the image description. */
  name: string;
  canManage: boolean;
  upload: (file: File) => Promise<unknown>;
  remove: (storageKey: string) => Promise<unknown>;
  onChanged: () => Promise<void>;
}) {
  const t = useTranslations("admin.inventory.photos");
  const toast = useToast();
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [removing, setRemoving] = useState<Photo | null>(null);

  async function change(action: () => Promise<unknown>, success?: string) {
    setBusy(true);
    try {
      await action();
      await onChanged();
      if (success) toast(success);
    } catch (error) {
      toast(toFormErrors(error, t("failed")).general ?? t("failed"), "error");
      throw error;
    } finally {
      setBusy(false);
      if (input.current) input.current.value = "";
    }
  }

  async function upload(file: File | undefined) {
    if (!file) return;
    await change(() => uploadPhoto(file)).catch(() => undefined);
  }

  return (
    <div className="flex flex-col gap-2 rounded-control border border-line bg-paper p-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title", { count: photos.length })}</h3>
      {photos.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("empty")}</p> : null}
      <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
        {photos.map((photo) => (
          <li key={photo.storageKey} className="relative">
            {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
            <img src={mediaUrl(photo.thumbnailStorageKey)} alt={t("alt", { model: name })} className="h-20 w-20 rounded-control border border-line object-cover" />
            {canManage ? (
              <IconButton icon={TrashIcon} label={t("remove")} variant="secondary" size="sm" className="absolute end-1 top-1" disabled={busy} onClick={() => setRemoving(photo)} />
            ) : null}
          </li>
        ))}
      </ul>
      {canManage ? (
        <label className={`${buttonClass()} cursor-pointer self-start ${busy ? "opacity-60" : ""}`}>
          <PlusIcon size={buttonIconSize()} />
          {t("add")}
          <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={(event) => void upload(event.target.files?.[0])} className="sr-only" />
        </label>
      ) : null}
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <ConfirmDialog
        open={removing !== null} title={t("removeTitle")} body={t("removeBody")} confirmLabel={t("remove")} danger
        onClose={() => setRemoving(null)}
        onConfirm={() => (removing ? change(() => remove(removing.storageKey), t("removed")) : Promise.resolve())}
      />
    </div>
  );
}
