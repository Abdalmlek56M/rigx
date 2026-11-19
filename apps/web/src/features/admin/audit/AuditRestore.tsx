"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { isConflict } from "@/lib/api/conflict";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { restoreEntry } from "./audit-api";
import { UndoIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** "Put these values back": asks first, then the step-up code; the server's refusal (e.g. the row changed since) shows as the message. */
export function AuditRestore({ id, onRestored }: { id: string; onRestored: () => void }) {
  const t = useTranslations("admin.audit.restore");
  const submit = useSubmit();
  const [confirming, setConfirming] = useState(false);

  async function restore() {
    await submit.run(async (headers) => {
      try {
        await restoreEntry(id, headers);
      } catch (error) {
        if (isConflict(error)) onRestored();
        throw error;
      }
      onRestored();
    }, t("done"));
  }

  return (
    <div className="flex flex-col items-start gap-2 border-t border-line pt-3">
      <Button variant="danger" icon={UndoIcon} disabled={submit.busy} onClick={() => setConfirming(true)}>{t("button")}</Button>
      <FormMessage message={submit.errors.general ?? Object.values(submit.errors.fields)[0] ?? null} />
      {submit.dialog}
      <ConfirmDialog open={confirming} title={t("title")} body={t("body")} confirmLabel={t("button")} danger onClose={() => setConfirming(false)} onConfirm={restore} />
    </div>
  );
}
