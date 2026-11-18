"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { isConflict } from "@/lib/api/conflict";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { MIN_REASON } from "./user-rules.ts";
import { reinstateRestriction, type Restriction } from "./users-api";

/** Lifts a restriction: one sentence on what happens, a reason (kept in the audit log) and the confirm button. */
export function ReinstateDialog({ restriction, onClose, onDone }: { restriction: Restriction; onClose: () => void; onDone: () => void }) {
  const t = useTranslations("admin.users.restrictions");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [reason, setReason] = useState("");
  const valid = reason.trim().length >= MIN_REASON;

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      try {
        await reinstateRestriction(restriction.id, reason.trim(), restriction.rowVersion);
      } catch (error) {
        if (isConflict(error)) onDone();
        throw error;
      }
      onDone();
      onClose();
    }, t("reinstated"));
  }

  return (
    <Dialog open onClose={onClose} title={t("reinstate")}>
      <p className="m-0 text-sm text-ink-muted">{t("confirm.reinstate")}</p>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        <TextAreaField label={t("reinstateReason")} name="reason" value={reason} onChange={setReason} error={submit.errors.fields.reason} maxLength={1000} />
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap justify-end gap-2">
          <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{kit("cancel")}</button>
          <button type="submit" disabled={submit.busy || !valid} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("reinstate")}</button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}
