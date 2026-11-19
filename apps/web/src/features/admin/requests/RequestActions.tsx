"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { canClose, canDecide, canFollow, convertAllowed } from "./request-labels";
import { convertRequest, decideRequest, setRequestStatus, type RequestDetail } from "./requests-api";
import { isConflict } from "@/lib/api/conflict";
import { ArrowEndIcon, BanIcon, CheckIcon, CloseIcon, EyeIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

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
          <Button icon={EyeIcon} loading={submit.busy} onClick={() => void move(() => setRequestStatus(request.id, "Following", request.rowVersion), t("actions.followed"))}>
            {t("actions.follow")}
          </Button>
        ) : null}
        {canClose(request.status) ? (
          <Button icon={CloseIcon} loading={submit.busy} onClick={() => void move(() => setRequestStatus(request.id, "Closed", request.rowVersion), t("actions.closed"))}>
            {t("actions.close")}
          </Button>
        ) : null}
        {canDecide(request.status) ? (
          <>
            <Button variant="primary" icon={CheckIcon} loading={submit.busy} onClick={() => void move(() => decideRequest(request.id, "Accepted", request.rowVersion), t("decision.accepted"))}>
              {t("decision.accept")}
            </Button>
            <Button variant="danger" icon={BanIcon} loading={submit.busy} onClick={() => void move(() => decideRequest(request.id, "Rejected", request.rowVersion), t("decision.rejected"))}>
              {t("decision.reject")}
            </Button>
          </>
        ) : null}
        {canConvert && convertAllowed(request.type, request.status) ? (
          <Button variant="primary" icon={ArrowEndIcon} disabled={submit.busy} onClick={() => setConverting(true)}>
            {t("convert.button")}
          </Button>
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
