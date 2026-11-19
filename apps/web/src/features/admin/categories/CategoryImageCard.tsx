"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { deleteCategoryImage, uploadCategoryImage, type Category } from "./categories-api";
import { TrashIcon, UploadIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

/** The category picture: upload, change and remove (the text save keeps the current key). */
export function CategoryImageCard({ category, onChanged }: { category: Category; onChanged: (saved: Category) => void }) {
  const t = useTranslations("admin.categories.image");
  const { has } = usePermissions();
  const toast = useToast();
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [removing, setRemoving] = useState(false);
  const canManage = has("Catalog.ManageMedia");
  if (!canManage && !category.iconStorageKey) return null;

  async function upload(file: File | undefined) {
    if (!file) return;
    setBusy(true);
    try {
      onChanged(await uploadCategoryImage(category.id, file));
    } catch {
      toast(t("failed"), "error");
    } finally {
      setBusy(false);
      if (input.current) input.current.value = "";
    }
  }

  async function remove() {
    setBusy(true);
    try {
      onChanged(await deleteCategoryImage(category.id));
      toast(t("removed"));
    } catch {
      toast(t("failed"), "error");
      throw new Error("remove failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-2 rounded-control border border-line bg-paper p-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <div className="flex flex-wrap items-center gap-3">
        <span className="flex h-16 w-24 shrink-0 items-center justify-center overflow-hidden rounded-control bg-surface">
          {category.iconStorageKey ? (
            // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
            <img src={mediaUrl(category.iconStorageKey)} alt={t("alt", { name: category.name })} className="h-full w-full object-contain" />
          ) : null}
        </span>
        {canManage ? (
          <div className="flex flex-wrap gap-2">
            <label className={`${buttonClass()} cursor-pointer ${busy ? "opacity-60" : ""}`}>
              <UploadIcon size={buttonIconSize()} />
              {category.iconStorageKey ? t("change") : t("upload")}
              <input
                ref={input} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy}
                onChange={(event) => void upload(event.target.files?.[0])} className="sr-only"
              />
            </label>
            {category.iconStorageKey ? (
              <Button variant="danger" icon={TrashIcon} disabled={busy} onClick={() => setRemoving(true)}>{t("remove")}</Button>
            ) : null}
          </div>
        ) : null}
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <ConfirmDialog
        open={removing} title={t("removeTitle")} body={t("removeBody")} confirmLabel={t("remove")} danger
        onClose={() => setRemoving(false)} onConfirm={remove}
      />
    </div>
  );
}
