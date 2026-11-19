"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { CheckIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "./Button";
import { Dialog } from "./Dialog";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  body?: string;
  /** The action's own verb ("احذف", "ألغي الطلب"); defaults to "confirm". */
  confirmLabel?: string;
  /** Red confirm button (with a bin icon) for deletes and cancellations. */
  danger?: boolean;
  /** Runs the action; the dialog stays open and busy until it settles, and closes on success. Show the failure with a toast. */
  onConfirm: () => Promise<void>;
  onClose: () => void;
}

/** "Are you sure?" before an action that changes or removes data. */
export function ConfirmDialog({ open, title, body, confirmLabel, danger = false, onConfirm, onClose }: ConfirmDialogProps) {
  const t = useTranslations("admin.kit");
  const [busy, setBusy] = useState(false);

  async function confirm() {
    setBusy(true);
    try {
      await onConfirm();
      onClose();
    } catch {
      // The caller reports the failure; keep the dialog open to retry or cancel.
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title={title}>
      {body ? <p className="m-0 text-sm text-ink-muted">{body}</p> : null}
      <div className="flex flex-wrap justify-end gap-2">
        <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
        <Button variant={danger ? "danger" : "primary"} icon={danger ? TrashIcon : CheckIcon} loading={busy} onClick={() => void confirm()}>
          {confirmLabel ?? t("confirm")}
        </Button>
      </div>
    </Dialog>
  );
}
