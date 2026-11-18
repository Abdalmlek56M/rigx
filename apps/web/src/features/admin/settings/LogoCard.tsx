"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { deleteLogo, uploadLogo, type SettingsView, type StoreLogoKind } from "./settings-api";

const button = "inline-flex h-11 cursor-pointer items-center rounded-control border border-input-line bg-surface px-4 text-sm font-bold";

/** One logo: preview, upload/change and remove (the settings form keeps the rest of the section). */
export function LogoCard({ kind, storageKey, onSaved }: { kind: StoreLogoKind; storageKey: string | null; onSaved: (view: SettingsView) => void }) {
  const t = useTranslations(`admin.settings.logo.${kind}`);
  const toast = useToast();
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [removing, setRemoving] = useState(false);

  async function upload(file: File | undefined) {
    if (!file) return;
    setBusy(true);
    try {
      onSaved(await uploadLogo(kind, file));
      toast(t("saved"));
    } catch (error) {
      toast(error instanceof Error ? error.message : t("failed"), "error");
    } finally {
      setBusy(false);
      if (input.current) input.current.value = "";
    }
  }

  async function remove() {
    setBusy(true);
    try {
      onSaved(await deleteLogo(kind));
      toast(t("removed"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-2 rounded-control border border-line bg-paper p-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <div className="flex flex-wrap items-center gap-3">
        <span className="flex h-16 w-24 shrink-0 items-center justify-center overflow-hidden rounded-control bg-surface">
          {storageKey ? (
            // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
            <img src={mediaUrl(storageKey)} alt={t("alt")} className="h-full w-full object-contain" />
          ) : null}
        </span>
        <div className="flex flex-wrap gap-2">
          <label className={`${button} ${busy ? "pointer-events-none opacity-60" : ""}`}>
            {storageKey ? t("change") : t("upload")}
            <input
              ref={input} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy}
              onChange={(event) => void upload(event.target.files?.[0])} className="sr-only"
            />
          </label>
          {storageKey ? (
            <button type="button" disabled={busy} onClick={() => setRemoving(true)} className={`${button} text-error disabled:opacity-50`}>
              {t("remove")}
            </button>
          ) : null}
        </div>
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <ConfirmDialog
        open={removing} title={t("removeTitle")} body={t("removeBody")} confirmLabel={t("remove")} danger
        onClose={() => setRemoving(false)}
        onConfirm={async () => {
          try {
            await remove();
          } catch (error) {
            toast(error instanceof Error ? error.message : t("failed"), "error");
          }
        }}
      />
    </div>
  );
}
