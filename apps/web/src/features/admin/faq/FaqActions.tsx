"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { useUndoDelete } from "../system/use-undo-delete";
import { deleteFaq, publishFaq, unpublishFaq, type Faq } from "./faq-api";
import { faqStatusKey, type FaqStatusKey } from "./faq-rules";
import { isConflict } from "@/lib/api/conflict";
import { TrashIcon, UndoIcon, UploadIcon } from "@/components/ui/icons";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { Button } from "../kit/Button";

const TONES: Record<FaqStatusKey, StatusTone> = { Draft: "neutral", Review: "warning", Published: "success", Archived: "neutral", Other: "neutral" };

export function FaqStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.faq.statuses");
  const key = faqStatusKey(status);
  return <StatusBadge tone={TONES[key]}>{t(key)}</StatusBadge>;
}

/** Publish / unpublish / delete for the FAQ being edited. */
export function FaqActions({ faq, onSaved, onDeleted, onStale }: {
  faq: Faq;
  onSaved: (faq: Faq) => void;
  onDeleted: () => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.faq.actions");
  const submit = useSubmit();
  const toast = useToast();
  const undoDelete = useUndoDelete();
  const [unpublishing, setUnpublishing] = useState(false);
  const [deleting, setDeleting] = useState(false);

  async function run(action: () => Promise<Faq>, success: string) {
    await submit.run(async () => {
      try {
        onSaved(await action());
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, success);
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">
        <FaqStatusBadge status={faq.status} />
        {faq.status !== "Published" ? (
          <Button variant="primary" icon={UploadIcon} loading={submit.busy} onClick={() => void run(() => publishFaq(faq.id, faq.rowVersion), t("published"))}>
            {t("publish")}
          </Button>
        ) : (
          <Button icon={UndoIcon} disabled={submit.busy} onClick={() => setUnpublishing(true)}>
            {t("unpublish")}
          </Button>
        )}
        <Button variant="danger" icon={TrashIcon} onClick={() => setDeleting(true)}>
          {t("delete")}
        </Button>
      </div>
      <FormMessage message={submit.errors.general} />
      {submit.dialog}
      <ConfirmDialog
        open={unpublishing} title={t("unpublish")} body={t("unpublishBody")} confirmLabel={t("unpublish")} danger
        onClose={() => setUnpublishing(false)}
        onConfirm={() => run(() => unpublishFaq(faq.id, faq.rowVersion), t("unpublished"))}
      />
      <ConfirmDialog
        open={deleting} title={t("delete")} body={t("deleteBody")} confirmLabel={t("delete")} danger
        onClose={() => setDeleting(false)}
        onConfirm={async () => {
          try {
            await deleteFaq(faq.id);
            undoDelete({ kind: "faq", id: faq.id, message: t("deleted"), onRestored: onDeleted });
            onDeleted();
          } catch (error) {
            toast(toFormErrors(error, t("failed")).general ?? t("failed"), "error");
            throw error;
          }
        }}
      />
    </div>
  );
}
