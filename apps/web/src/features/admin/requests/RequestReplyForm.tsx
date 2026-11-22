"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { isConflict } from "@/lib/api/conflict";
import { formatDateTime } from "@/lib/format";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { replyAllowed } from "./request-labels";
import { saveRequestReply, type RequestDetail } from "./requests-api";
import { SendIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const MAX_REPLY = 2000;

/**
 * The store's written answer to a complaint, suggestion, support or contact message. The customer reads it in their account (and gets a
 * bell entry) when the request was sent from an account; otherwise the answer goes by WhatsApp, which the panel below prepares.
 */
export function RequestReplyForm({ request, canManage, onSaved, onStale }: { request: RequestDetail; canManage: boolean; onSaved: () => void; onStale: () => void }) {
  const t = useTranslations("admin.requests.reply");
  const locale = useLocale();
  const submit = useSubmit();
  const [reply, setReply] = useState(request.reply ?? "");
  if (!replyAllowed(request.type)) return null;

  async function save() {
    await submit.run(async () => {
      try {
        await saveRequestReply(request.id, reply, request.rowVersion);
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
      <p className="m-0 text-xs text-ink-muted">{request.userAccountId ? t("inAccount") : t("noAccount")}</p>
      {request.reply && request.repliedAtUtc ? <p className="m-0 text-xs text-ink-muted">{t("sentAt", { date: formatDateTime(request.repliedAtUtc, locale) })}</p> : null}
      {canManage ? (
        <form
          className="flex flex-col gap-3"
          onSubmit={(event) => {
            event.preventDefault();
            void save();
          }}
        >
          <TextAreaField
            label={t("label")} name="reply" rows={5} maxLength={MAX_REPLY} value={reply} onChange={setReply} error={submit.errors.fields.reply}
          />
          <FormMessage message={submit.errors.general} />
          <Button type="submit" variant="primary" icon={SendIcon} className="self-start" loading={submit.busy} disabled={reply.trim().length < 2}>
            {request.reply ? t("update") : t("send")}
          </Button>
          {submit.dialog}
        </form>
      ) : request.reply ? (
        <p className="m-0 whitespace-pre-wrap rounded-control bg-paper p-3 text-sm text-ink-muted">{request.reply}</p>
      ) : null}
    </section>
  );
}
