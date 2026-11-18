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

const TONES: Record<OfferStatusKey, string> = {
  Draft: "bg-line text-ink-muted",
  Published: "bg-success text-surface",
  Cancelled: "bg-error text-surface",
  Other: "bg-line text-ink-muted"
};

export function OfferStatusBadge({ status }: { status: string }) {
  const t = useTranslations("admin.offers.statuses");
  const key = offerStatusKey(status);
  return <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${TONES[key]}`}>{t(key)}</span>;
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
          <button type="button" disabled={submit.busy} onClick={() => void publish()} className="h-11 rounded-control bg-carbon px-4 text-sm font-bold text-on-carbon disabled:opacity-50">{t("publish")}</button>
        ) : null}
        {offer.status === "Published" ? (
          <button type="button" onClick={() => setUnpublishing(true)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("unpublish")}</button>
        ) : null}
        {offer.status !== "Cancelled" ? (
          <button type="button" onClick={() => setCancelling(true)} className="h-11 rounded-control border border-error/40 bg-surface px-4 text-sm font-bold text-error">{t("cancel")}</button>
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
