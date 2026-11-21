"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { Dialog } from "../kit/Dialog";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import type { Schemas } from "@rigx/api-client";
import { EyeIcon, PlusIcon, TrashIcon } from "@/components/ui/icons";
import { IconButton, buttonClass, buttonIconSize } from "../kit/Button";

type Photo = Schemas["StockItemPhoto"];

/**
 * The photos of a stock item or a build: a numbered square grid, the first photo marked as the main one, a storefront
 * preview, upload and delete. Reordering needs an API call that does not exist yet, so the main photo stays the first
 * photo the server returns.
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
  const [previewing, setPreviewing] = useState(false);
  const [selected, setSelected] = useState(0);

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
    <div className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="m-0 text-[15px] font-bold">{t("title", { count: photos.length })}</h3>
        {photos.length > 0 ? (
          <button type="button" className="flex min-h-9 items-center gap-1.5 text-sm font-bold text-ink-muted hover:text-ink" onClick={() => { setSelected(0); setPreviewing(true); }}>
            <EyeIcon size={16} />{t("preview")}
          </button>
        ) : null}
      </div>
      {photos.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("empty")}</p> : (
        <ul className="m-0 grid list-none grid-cols-3 gap-2 p-0 md:grid-cols-4">
          {photos.map((photo, index) => (
            <li key={photo.storageKey} className="relative aspect-square overflow-hidden rounded-control border border-line bg-surface">
              {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
              <img src={mediaUrl(photo.thumbnailStorageKey)} alt={t("alt", { model: name })} className="h-full w-full object-cover" />
              <span dir="ltr" className="absolute left-1 top-1 rounded-full bg-carbon/80 px-1.5 py-0.5 font-mono text-[11px] text-on-carbon">{index + 1}</span>
              {index === 0 ? (
                <span className="absolute bottom-1 left-1 rounded-[4px] bg-orange px-1.5 py-0.5 text-[11px] font-bold text-on-orange">{t("main")}</span>
              ) : null}
              {canManage ? (
                <span className="absolute right-1 top-1 z-10">
                  <IconButton icon={TrashIcon} label={t("remove")} variant="danger" size="sm" disabled={busy} onClick={() => setRemoving(photo)} />
                </span>
              ) : null}
            </li>
          ))}
        </ul>
      )}
      {canManage ? (
        <label className={`${buttonClass()} cursor-pointer self-start ${busy ? "opacity-60" : ""}`}>
          <PlusIcon size={buttonIconSize()} />
          {t("add")}
          <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={(event) => void upload(event.target.files?.[0])} className="sr-only" />
        </label>
      ) : null}
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <Dialog open={previewing} onClose={() => setPreviewing(false)} title={t("previewTitle")} wide>
        {photos.length > 0 ? (
          <div className="mx-auto flex w-full min-w-0 max-w-md flex-col gap-3">
            <div className="aspect-square w-full overflow-hidden rounded-card border border-line bg-surface">
              {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
              <img src={mediaUrl(photos[Math.min(selected, photos.length - 1)]!.storageKey)} alt={t("alt", { model: name })} className="h-full w-full object-contain" />
            </div>
            {photos.length > 1 ? (
              <ul className="m-0 grid list-none grid-cols-4 gap-2 p-0">
                {photos.map((photo, index) => (
                  <li key={photo.storageKey}>
                    <button
                      type="button" aria-label={t("photoNumber", { number: index + 1 })} aria-pressed={index === Math.min(selected, photos.length - 1)}
                      onClick={() => setSelected(index)}
                      className={`block aspect-square w-full overflow-hidden rounded-control border bg-surface ${index === Math.min(selected, photos.length - 1) ? "border-2 border-ink" : "border-line"}`}
                    >
                      {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                      <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" className="h-full w-full object-cover" />
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}
          </div>
        ) : <p className="m-0 text-sm text-ink-muted">{t("empty")}</p>}
      </Dialog>
      <ConfirmDialog
        open={removing !== null} title={t("removeTitle")} body={t("removeBody")} confirmLabel={t("remove")} danger
        onClose={() => setRemoving(null)}
        onConfirm={() => (removing ? change(() => remove(removing.storageKey), t("removed")) : Promise.resolve())}
      />
    </div>
  );
}
