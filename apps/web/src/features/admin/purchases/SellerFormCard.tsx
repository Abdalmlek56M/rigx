"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { createSeller, updateSeller, type Seller } from "./purchases-api";
import { emptySeller, isKnownSource, SELLER_KINDS, SELLER_SOURCES, sellerFormValid, sellerFromView, toSellerRequest, type SellerForm } from "./purchase-rules";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

export interface Governorate {
  code: string;
  name: string;
}

/** The seller's details: a new seller (phone prefilled from the lookup) or an existing one being edited. A 409 reads the seller again. */
export function SellerFormCard({ seller, phone, governorates, onSaved, onStale, onCancel }: {
  seller: Seller | null;
  phone: string;
  governorates: readonly Governorate[];
  onSaved: (seller: Seller) => void;
  onStale: () => void;
  onCancel: () => void;
}) {
  const t = useTranslations("admin.purchases.seller");
  const submit = useSubmit();
  const [form, setForm] = useState<SellerForm>(() => (seller ? sellerFromView(seller) : emptySeller(phone)));
  const set = (patch: Partial<SellerForm>) => setForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];
  const sources = form.source && !isKnownSource(form.source) ? [form.source, ...SELLER_SOURCES] : SELLER_SOURCES;

  async function save() {
    if (!sellerFormValid(form)) return;
    await submit.run(async () => {
      try {
        onSaved(seller ? await updateSeller(seller.id, toSellerRequest(form, seller.rowVersion)) : await createSeller(toSellerRequest(form)));
      } catch (failure) {
        if (isConflict(failure)) onStale();
        throw failure;
      }
    }, t(seller ? "updated" : "created"));
  }

  return (
    <div className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
      <h3 className="m-0 text-[15px] font-bold">{t(seller ? "editTitle" : "newTitle")}</h3>
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField label={t("kind")} name="kind" value={form.kind} onChange={(kind) => set({ kind })} options={SELLER_KINDS.map((kind) => ({ value: kind, label: t(`kinds.${kind}`) }))} error={error("kind")} />
        <TextField label={t("name")} name="name" value={form.name} onChange={(name) => set({ name })} error={error("name")} />
        <TextField label={t("phone")} name="phone" type="tel" ltr value={form.phone} onChange={(phone) => set({ phone })} error={error("phone")} />
        <TextField label={t("nationalId")} name="nationalId" ltr value={form.nationalId} onChange={(nationalId) => set({ nationalId })} error={error("nationalId")} />
        <SelectField
          label={t("governorate")} name="governorate" value={form.governorate} onChange={(governorate) => set({ governorate })} error={error("governorate")}
          options={[{ value: "", label: t("pick") }, ...governorates.map((governorate) => ({ value: governorate.code, label: governorate.name }))]}
        />
        <TextField label={t("area")} name="area" value={form.area} onChange={(area) => set({ area })} error={error("area")} />
        <SelectField
          label={t("source")} name="source" value={form.source} onChange={(source) => set({ source })} error={error("source")}
          options={[{ value: "", label: t("pick") }, ...sources.map((source) => ({ value: source, label: isKnownSource(source) ? t(`sources.${source}`) : source }))]}
        />
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("nationalIdHint")}</p>
      <TextAreaField label={t("notes")} name="notes" value={form.notes} onChange={(notes) => set({ notes })} rows={2} maxLength={1000} error={error("notes")} />
      <FormMessage message={submit.errors.general} />
      <div className="flex flex-wrap gap-2">
        <Button variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!sellerFormValid(form)} onClick={() => void save()}>{t("save")}</Button>
        <Button variant="ghost" onClick={onCancel}>{t("cancel")}</Button>
      </div>
      {submit.dialog}
    </div>
  );
}
