"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatEgp } from "@/lib/format";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { OfferActions } from "./OfferActions";
import { OfferItems } from "./OfferItems";
import { createOffer, updateOffer, type Offer, type ProductSummary } from "./offers-api";
import { emptyOfferForm, endsBeforeStart, offerForm, offerValid, toOfferInput, type OfferForm as Form } from "./offer-rules";
import { isConflict } from "@/lib/api/conflict";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function Summary({ offer }: { offer: Offer }) {
  const t = useTranslations("admin.offers");
  const locale = useLocale();
  return (
    <div className="flex flex-col gap-3 text-sm">
      <p className="m-0 text-xs font-bold text-error">{t("cancelledNote")}</p>
      <dl className="m-0 grid gap-2 sm:grid-cols-2">
        <div><dt className="text-xs text-ink-muted">{t("fields.titleAr")}</dt><dd className="m-0 font-bold">{offer.titleAr}</dd></div>
        <div><dt className="text-xs text-ink-muted">{t("fields.titleEn")}</dt><dd className="m-0 font-bold">{offer.titleEn}</dd></div>
        <div><dt className="text-xs text-ink-muted">{t("columns.period")}</dt><dd className="m-0">{formatDate(offer.startsAtUtc, locale)}{offer.endsAtUtc ? ` – ${formatDate(offer.endsAtUtc, locale)}` : ""}</dd></div>
        <div><dt className="text-xs text-ink-muted">{t("fields.priority")}</dt><dd className="m-0">{offer.priority}</dd></div>
      </dl>
      <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
        {offer.items.map((item) => (
          <li key={item.productId} className="flex flex-wrap items-center justify-between gap-2 border-b border-line pb-1.5 last:border-b-0">
            <span dir="auto" className="font-bold [unicode-bidi:isolate]">{item.productName}</span>
            <span className="text-xs"><s className="text-ink-muted">{formatEgp(item.basePrice, locale)}</s> · <b>{formatEgp(item.offerPrice, locale)}</b></span>
          </li>
        ))}
      </ul>
    </div>
  );
}

/** The offer form: bilingual card, Cairo start/end dates, priority and the discounted products. */
export function OfferForm({ offer, products, today, onSaved, onStale, onCancel, onReload }: {
  offer: Offer | null;
  products: readonly ProductSummary[];
  today: string;
  onSaved: (offer: Offer) => void;
  onStale: () => void;
  onCancel: () => void;
  onReload: () => void;
}) {
  const t = useTranslations("admin.offers");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => offer ? offerForm(offer) : emptyOfferForm(today));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const editable = offer?.status !== "Cancelled";

  async function save() {
    if (!offerValid(form)) return;
    await submit.run(async () => {
      try {
        const input = toOfferInput(form);
        const saved = offer ? await updateOffer(offer.id, { ...input, rowVersion: offer.rowVersion }) : await createOffer(input);
        onSaved(saved);
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <div className="flex flex-col gap-4">
      {offer ? <OfferActions offer={offer} onSaved={onSaved} onStale={onStale} /> : null}
      {!editable && offer ? <Summary offer={offer} /> : (
        <form
          className="flex flex-col gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            void save();
          }}
        >
          <div className="grid gap-3 sm:grid-cols-2">
            <TextField label={t("fields.titleAr")} name="titleAr" dir="rtl" value={form.titleAr} onChange={(titleAr) => set({ titleAr })} error={submit.errors.fields.titleAr} required />
            <TextField label={t("fields.titleEn")} name="titleEn" value={form.titleEn} onChange={(titleEn) => set({ titleEn })} ltr error={submit.errors.fields.titleEn} required />
            <TextAreaField label={t("fields.descriptionAr")} name="descriptionAr" dir="rtl" value={form.descriptionAr} onChange={(descriptionAr) => set({ descriptionAr })} rows={2} error={submit.errors.fields.descriptionAr} />
            <TextAreaField label={t("fields.descriptionEn")} name="descriptionEn" dir="ltr" value={form.descriptionEn} onChange={(descriptionEn) => set({ descriptionEn })} rows={2} error={submit.errors.fields.descriptionEn} />
            <TextField label={t("fields.startDate")} name="startDate" type="date" ltr value={form.startDate} onChange={(startDate) => set({ startDate })} error={submit.errors.fields.startsAtUtc} required />
            <TextField label={t("fields.endDate")} name="endDate" type="date" ltr value={form.endDate} onChange={(endDate) => set({ endDate })} error={submit.errors.fields.endsAtUtc ?? (endsBeforeStart(form) ? t("endBeforeStart") : undefined)} />
            <TextField label={t("fields.priority")} name="priority" type="number" ltr value={form.priority} onChange={(priority) => set({ priority })} error={submit.errors.fields.priority} />
          </div>
          <p className="m-0 text-xs text-ink-muted">{t("datesHint")}</p>
          <OfferItems items={form.items} products={products} onChange={(items) => set({ items })} onReload={onReload} />
          <FormMessage message={submit.errors.general} />
          <div className="flex flex-wrap gap-2">
            <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!offerValid(form)}>{t("save")}</Button>
            {offer ? null : <Button variant="ghost" onClick={onCancel}>{kit("cancel")}</Button>}
          </div>
          {submit.dialog}
        </form>
      )}
    </div>
  );
}
