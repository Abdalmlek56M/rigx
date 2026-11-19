"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { addressErrors, governorateOptions, type AddressField, type AddressForm } from "./address-form";

interface AddressFormViewProps {
  initial: AddressForm;
  governorates: readonly string[];
  busy: boolean;
  failed: boolean;
  onSubmit: (form: AddressForm) => void;
  onCancel: () => void;
}

/** The add / edit address form; invalid required fields are flagged on submit. */
export function AddressFormView({ initial, governorates, busy, failed, onSubmit, onCancel }: AddressFormViewProps) {
  const t = useTranslations("account.addresses");
  const [form, setForm] = useState(initial);
  const [errors, setErrors] = useState<AddressField[]>([]);
  const set = <K extends keyof AddressForm>(key: K, value: AddressForm[K]) => setForm((current) => ({ ...current, [key]: value }));
  const error = (field: AddressField) => (errors.includes(field) ? t(`errors.${field}`) : null);
  const options = [{ value: "", label: t("governoratePlaceholder") }, ...governorateOptions(governorates, form.governorate)];

  function submit(event: React.FormEvent) {
    event.preventDefault();
    const found = addressErrors(form);
    setErrors(found);
    if (found.length === 0) onSubmit(form);
  }

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-3.5 rounded-panel border border-line bg-surface p-5 shadow-soft md:p-6">
      <div className="grid gap-3.5 sm:grid-cols-2">
        <TextField label={t("label")} name="label" value={form.label} onChange={(v) => set("label", v)} placeholder={t("labelPlaceholder")} required error={error("label")} />
        <TextField label={t("recipientName")} name="recipientName" value={form.recipientName} onChange={(v) => set("recipientName", v)} autoComplete="name" required error={error("recipientName")} />
        <TextField label={t("phone")} name="phone" type="tel" ltr value={form.phone} onChange={(v) => set("phone", v)} autoComplete="tel" placeholder="01xxxxxxxxx" required error={error("phone")} />
        <SelectField label={t("governorate")} name="governorate" value={form.governorate} onChange={(v) => set("governorate", v)} options={options} error={error("governorate")} />
        <TextField label={t("city")} name="city" value={form.city} onChange={(v) => set("city", v)} required error={error("city")} />
        <TextField label={t("street")} name="street" value={form.street} onChange={(v) => set("street", v)} autoComplete="street-address" required error={error("street")} />
        <TextField label={t("building")} name="building" value={form.building} onChange={(v) => set("building", v)} />
        <TextField label={t("floor")} name="floor" value={form.floor} onChange={(v) => set("floor", v)} />
        <TextField label={t("apartment")} name="apartment" value={form.apartment} onChange={(v) => set("apartment", v)} />
        <TextField label={t("landmark")} name="landmark" value={form.landmark} onChange={(v) => set("landmark", v)} />
      </div>
      <label className="flex min-h-11 items-center gap-2.5 text-sm">
        <input type="checkbox" checked={form.isDefault} onChange={(event) => set("isDefault", event.target.checked)} className="h-[18px] w-[18px] accent-orange" />
        {t("makeDefault")}
      </label>
      {failed ? <p role="alert" className="m-0 text-sm text-error">{t("saveFailed")}</p> : null}
      <div className="flex flex-wrap gap-3">
        <button type="submit" disabled={busy} className="h-11 rounded-control bg-orange px-6 font-bold text-on-orange disabled:opacity-60">{t("save")}</button>
        <button type="button" onClick={onCancel} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button>
      </div>
    </form>
  );
}
