"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { cancelOffer, publishOffer, unpublishOffer, type Offer } from "./offers-api";
import { offerStatusKey, type OfferStatusKey } from "./offer-rules";
import { isConflict } from "@/lib/api/conflict";
import { BanIcon, UndoIcon, UploadIcon } from "@/components/ui/icons";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { Button } from "../kit/Button";

const TONES: Record<OfferStatusKey, StatusTone> = { Draft: "neutral", Published: "success", Cancelled: "danger", Other: "neutral" };

export function OfferStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.offers.statuses");
  const key = offerStatusKey(status);
  return <StatusBadge tone={TONES[key]}>{t(key)}</StatusBadge>;
}

/** Publish / unpublish / cancel an offer. */
export function OfferActions({ offer, onSaved, onStale }: { offer: Offer; onSaved: (offer: Offer) => void; onStale: () => void }) {
  const t = useTranslations("admin.offers.actions");
  const submit = useSubmit();
  const toast = useToast();
  const [unpublishing, setUnpublishing] = useState(false);
  const [cancelling, setCancelling] = useState(false);

  async function publish() {
    await submit.run(async () => {
      try {
        onSaved(await publishOffer(offer.id, offer.rowVersion));
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("published"));
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">
        <OfferStatusBadge status={offer.status} />
        {offer.status !== "Published" && offer.status !== "Cancelled" ? (
          <Button variant="primary" icon={UploadIcon} loading={submit.busy} onClick={() => void publish()}>{t("publish")}</Button>
        ) : null}
        {offer.status === "Published" ? (
          <Button icon={UndoIcon} onClick={() => setUnpublishing(true)}>{t("unpublish")}</Button>
        ) : null}
        {offer.status !== "Cancelled" ? (
          <Button variant="danger" icon={BanIcon} onClick={() => setCancelling(true)}>{t("cancel")}</Button>
        ) : null}
      </div>
      <FormMessage message={submit.errors.general} />
      {submit.dialog}
      <ConfirmDialog
        open={unpublishing} title={t("unpublish")} body={t("unpublishBody")} confirmLabel={t("unpublish")} danger
        onClose={() => setUnpublishing(false)}
        onConfirm={async () => {
          try { onSaved(await unpublishOffer(offer.id, offer.rowVersion)); toast(t("unpublished")); }
          catch (error) { if (isConflict(error)) onStale(); toast(toFormErrors(error, t("failed")).general ?? t("failed"), "error"); throw error; }
        }}
      />
      <ConfirmDialog
        open={cancelling} title={t("cancel")} body={t("cancelBody")} confirmLabel={t("cancel")} danger
        onClose={() => setCancelling(false)}
        onConfirm={async () => {
          try { onSaved(await cancelOffer(offer.id, offer.rowVersion)); toast(t("cancelled")); }
          catch (error) { if (isConflict(error)) onStale(); toast(toFormErrors(error, t("failed")).general ?? t("failed"), "error"); throw error; }
        }}
      />
    </div>
  );
}
