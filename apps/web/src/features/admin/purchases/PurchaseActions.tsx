"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { isConflict } from "@/lib/api/conflict";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import type { useSubmit } from "../kit/use-submit";
import { usePermissions } from "../use-permissions";
import { cancelPurchase, createPurchase, receivePurchase, updatePurchase, type Purchase, type PurchaseReceived } from "./purchases-api";
import { toPurchaseRequest, type PurchaseForm } from "./purchase-rules";
import { BanIcon, BoxIcon, SaveIcon } from "@/components/ui/icons";
import { ApiError } from "@rigx/api-client";
import { Button } from "../kit/Button";

/** The first thing the server said was wrong, for a toast (a field error when there is one). */
function failureMessage(error: unknown, fallback: string): string {
  const { general, fields } = toFormErrors(error, fallback);
  return general ?? Object.values(fields)[0] ?? fallback;
}

/**
 * Save the draft (new → opens its page), receive it into stock, or cancel it. Receiving saves what is on screen first, so what is received
 * is what the person sees. Every write sends the purchase's row version; a 409 reads the purchase again.
 */
export function PurchaseActions({ purchase, form, onValidate, onCreated, submit, onUpdated, onReceived, onCancelled, onStale }: {
  purchase: Purchase | null;
  form: PurchaseForm;
  /** Marks the missing fields, opens and focuses the first one; false means the form stays on screen. */
  onValidate: () => boolean;
  /** The server has the new purchase: stop and clear its draft before opening it. */
  onCreated: () => void;
  submit: ReturnType<typeof useSubmit>;
  onUpdated: (purchase: Purchase) => void;
  onReceived: (result: PurchaseReceived) => void;
  onCancelled: (purchase: Purchase) => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.purchases.actions");
  const toast = useToast();
  const router = useRouter();
  const { has } = usePermissions();
  const [confirm, setConfirm] = useState<"receive" | "cancel" | null>(null);

  const guard = async <T,>(action: () => Promise<T>): Promise<T> => {
    try {
      return await action();
    } catch (error) {
      if (isConflict(error)) onStale();
      throw error;
    }
  };

  async function saveDraft() {
    if (!onValidate()) return;
    await submit.run(async () => {
      if (purchase) {
        onUpdated(await guard(() => updatePurchase(purchase.id, toPurchaseRequest(form, purchase.rowVersion))));
      } else {
        const created = await createPurchase(toPurchaseRequest(form));
        onCreated();
        router.push(`/admin/purchases/${created.id}`);
      }
    }, t("saved"));
  }

  async function receive() {
    if (!purchase || !onValidate()) return;
    try {
      // Staff who can only receive cannot save the draft; they receive it as it was saved.
      if (has("Purchasing.CreateOrder")) onUpdated(await guard(() => updatePurchase(purchase.id, toPurchaseRequest(form, purchase.rowVersion))));
      onReceived(await guard(() => receivePurchase(purchase.id)));
      toast(t("receivedToast"));
    } catch (error) {
      const shortOfCash = error instanceof ApiError && error.status === 409 && /balance is not enough/i.test(error.problem.title ?? "");
      toast(shortOfCash ? t("notEnoughCash") : failureMessage(error, t("failed")), "error");
      throw error;
    }
  }

  async function cancel() {
    if (!purchase) return;
    try {
      onCancelled(await guard(() => cancelPurchase(purchase.id, purchase.rowVersion)));
      toast(t("cancelledToast"));
    } catch (error) {
      toast(failureMessage(error, t("failed")), "error");
      throw error;
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {has("Purchasing.CreateOrder") ? (
        <Button variant={purchase && has("Purchasing.ReceiveGoods") ? "secondary" : "primary"} icon={SaveIcon} loading={submit.busy} onClick={() => void saveDraft()}>{t("saveDraft")}</Button>
      ) : null}
      {purchase && has("Purchasing.ReceiveGoods") ? (
        <Button variant="primary" icon={BoxIcon} disabled={submit.busy} onClick={() => { if (onValidate()) setConfirm("receive"); }}>{t("receive")}</Button>
      ) : null}
      {purchase && has("Purchasing.CreateOrder") ? (
        <Button variant="danger" icon={BanIcon} disabled={submit.busy} onClick={() => setConfirm("cancel")}>{t("cancel")}</Button>
      ) : null}
      <ConfirmDialog
        open={confirm === "receive"} title={t("receiveTitle")} body={t("receiveBody")} confirmLabel={t("receive")}
        onClose={() => setConfirm(null)} onConfirm={receive}
      />
      <ConfirmDialog
        open={confirm === "cancel"} title={t("cancelTitle")} body={t("cancelBody")} confirmLabel={t("cancel")} danger
        onClose={() => setConfirm(null)} onConfirm={cancel}
      />
    </div>
  );
}
