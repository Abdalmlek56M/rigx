"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";

interface ReasonDialogProps {
  open: boolean;
  title: string;
  body?: string;
  /** The text field's label. */
  label: string;
  /** The server's name for the field, so its error shows under it. */
  field: string;
  maxLength: number;
  /** Disables the confirm button until the text is acceptable (the server still decides). */
  valid?: (text: string) => boolean;
  confirmLabel: string;
  danger?: boolean;
  success: string;
  onConfirm: (text: string) => Promise<void>;
  onClose: () => void;
}

/** A confirmation that also asks for a short text (a cancel note, a waiver or blacklist reason). */
export function ReasonDialog({ open, title, body, label, field, maxLength, valid = () => true, confirmLabel, danger = false, success, onConfirm, onClose }: ReasonDialogProps) {
  const t = useTranslations("admin.kit");
  const submit = useSubmit();
  const [text, setText] = useState("");

  async function confirm() {
    await submit.run(async () => {
      await onConfirm(text.trim());
      setText("");
      onClose();
    }, success);
  }

  return (
    <Dialog open={open} onClose={onClose} title={title}>
      {body ? <p className="m-0 text-sm text-ink-muted">{body}</p> : null}
      <TextAreaField label={label} name={field} value={text} onChange={setText} maxLength={maxLength} error={submit.errors.fields[field]} />
      <FormMessage message={submit.errors.general} />
      <div className="flex flex-wrap justify-end gap-2">
        <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button>
        <button
          type="button" disabled={submit.busy || !valid(text)} onClick={() => void confirm()}
          className={`h-11 rounded-control px-5 font-bold disabled:opacity-50 ${danger ? "bg-error text-surface" : "bg-carbon text-on-carbon"}`}
        >
          {confirmLabel}
        </button>
      </div>
      {submit.dialog}
    </Dialog>
  );
}
