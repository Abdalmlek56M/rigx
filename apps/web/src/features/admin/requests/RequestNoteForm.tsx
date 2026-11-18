"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { isConflict } from "@/lib/api/conflict";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { saveRequestNote, type RequestDetail } from "./requests-api";

/** The staff-only note on a request; never shown to the customer. */
export function RequestNoteForm({ request, canManage, onSaved, onStale }: { request: RequestDetail; canManage: boolean; onSaved: () => void; onStale: () => void }) {
  const t = useTranslations("admin.requests.note");
  const submit = useSubmit();
  const [note, setNote] = useState(request.internalNote ?? "");

  async function save() {
    await submit.run(async () => {
      try {
        await saveRequestNote(request.id, note, request.rowVersion);
        onSaved();
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <section className="flex flex-col gap-3 border-t border-line pt-4">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      {request.internalNote ? <p className="m-0 whitespace-pre-wrap text-sm text-ink-muted">{request.internalNote}</p> : null}
      {canManage ? (
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            void save();
          }}
        >
          <TextAreaField label={t("title")} name="internalNote" value={note} onChange={setNote} hint={t("hint")} error={submit.errors.fields.internalNote} />
          <FormMessage message={submit.errors.general} />
          <button type="submit" disabled={submit.busy} className="h-11 self-start rounded-control border border-input-line bg-surface px-5 font-bold disabled:opacity-50">
            {t("save")}
          </button>
          {submit.dialog}
        </form>
      ) : null}
    </section>
  );
}
