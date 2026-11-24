"use client";

import { useTranslations } from "next-intl";
import { SaveIcon, UndoIcon } from "@/components/ui/icons";
import { Button } from "./Button";

/**
 * The floating save bar of a long form: it appears at the bottom of the screen while the form has unsaved edits, with "save" (the form's
 * submit) and "undo the edits". It keeps the phone's home-bar gap (`safe-area`). The form is the bar's parent, so Enter still submits.
 */
export function StickySaveBar({ dirty, busy, onReset }: { dirty: boolean; busy: boolean; onReset: () => void }) {
  const t = useTranslations("admin.kit.saveBar");
  if (!dirty && !busy) return null;
  return (
    <div
      role="region" aria-label={t("label")}
      className="sticky bottom-3 z-20 -mx-1 mt-2 flex flex-wrap items-center justify-between gap-2 rounded-card border border-line bg-surface p-3 shadow-[0_8px_28px_rgba(21,23,27,.18)] max-md:bottom-[calc(0.75rem+env(safe-area-inset-bottom))]"
    >
      <span className="flex items-center gap-2 text-sm font-bold">
        <span aria-hidden="true" className="size-2 shrink-0 rounded-full bg-orange" />
        {t("unsaved")}
      </span>
      <span className="flex items-center gap-2">
        <Button variant="ghost" icon={UndoIcon} disabled={busy} onClick={onReset}>{t("reset")}</Button>
        <Button type="submit" variant="primary" icon={SaveIcon} loading={busy}>{t("save")}</Button>
      </span>
    </div>
  );
}
