"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { canClose, canDecide, canFollow, convertAllowed } from "./request-labels";
import { convertRequest, decideRequest, setRequestStatus, type RequestDetail } from "./requests-api";
import { isConflict } from "@/lib/api/conflict";

/** Follow / close, accept / reject and "convert to purchase", each only where the API allows it. */
export function RequestActions({ request, canManage, canConvert, onSaved, onStale }: {
  request: RequestDetail;
  canManage: boolean;
  canConvert: boolean;
  onSaved: () => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.requests");
  const submit = useSubmit();
  const toast = useToast();
  const [converting, setConverting] = useState(false);
  if (!canManage) return null;

  async function move(action: () => Promise<unknown>, success: string) {
    await submit.run(async () => {
      try {
        await action();
        onSaved();
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, success);
  }

  return (
    <section className="flex flex-col gap-3 border-t border-line pt-4">
      <h3 className="m-0 text-[15px] font-bold">{t("actions.title")}</h3>
      <div className="flex flex-wrap gap-2">
        {canFollow(request.status) ? (
          <button type="button" disabled={submit.busy} onClick={() => void move(() => setRequestStatus(request.id, "Following", request.rowVersion), t("actions.followed"))} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-50">
            {t("actions.follow")}
          </button>
        ) : null}
        {canClose(request.status) ? (
          <button type="button" disabled={submit.busy} onClick={() => void move(() => setRequestStatus(request.id, "Closed", request.rowVersion), t("actions.closed"))} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-50">
            {t("actions.close")}
          </button>
        ) : null}
        {canDecide(request.status) ? (
          <>
            <button type="button" disabled={submit.busy} onClick={() => void move(() => decideRequest(request.id, "Accepted", request.rowVersion), t("decision.accepted"))} className="h-11 rounded-control bg-success px-4 text-sm font-bold text-surface disabled:opacity-50">
              {t("decision.accept")}
            </button>
            <button type="button" disabled={submit.busy} onClick={() => void move(() => decideRequest(request.id, "Rejected", request.rowVersion), t("decision.rejected"))} className="h-11 rounded-control bg-error px-4 text-sm font-bold text-surface disabled:opacity-50">
              {t("decision.reject")}
            </button>
          </>
        ) : null}
        {canConvert && convertAllowed(request.type, request.status) ? (
          <button type="button" disabled={submit.busy} onClick={() => setConverting(true)} className="h-11 rounded-control bg-orange px-4 text-sm font-bold text-on-orange disabled:opacity-50">
            {t("convert.button")}
          </button>
        ) : null}
      </div>
      {request.purchaseCode ? <p className="m-0 text-sm text-ink-muted">{t("convert.purchase", { code: request.purchaseCode })}</p> : null}
      {submit.dialog}
      <ConfirmDialog
        open={converting} title={t("convert.title")} body={t("convert.body")} confirmLabel={t("convert.button")}
        onClose={() => setConverting(false)}
        onConfirm={async () => {
          try {
            await convertRequest(request.id, request.rowVersion);
            toast(t("convert.done"));
            onSaved();
          } catch (error) {
            if (isConflict(error)) onStale();
            toast(t("convert.failed"), "error");
            throw error;
          }
        }}
      />
    </section>
  );
}
