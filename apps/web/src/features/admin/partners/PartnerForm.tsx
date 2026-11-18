"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { formMessage } from "../form-message";
import type { Partner } from "../expenses/expenses-api";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { createPartner, updatePartner } from "./partners-api";
import { emptyPartnerForm, PARTNER_BASES, partnerFormValid, toPartnerForm, toPartnerRequest, type PartnerForm as Form } from "./partner-rules";

const SHOWN_FIELDS = ["name", "phone", "joinDate", "defaultPercent", "basis", "notes"];
const checkbox = "flex min-h-11 items-center gap-2 text-sm font-bold";

/** A partner's agreement: name, phone, join date, default percent, what the percent is taken from, loss sharing, active and notes. A new partner (no `partner`) is created; an existing one is saved with its row version. */
export function PartnerForm({ partner, today, canEdit, onSaved, onStale, onCancel }: {
  partner: Partner | null;
  today: string;
  canEdit: boolean;
  onSaved: (partner: Partner) => void;
  onStale?: () => void;
  onCancel?: () => void;
}) {
  const t = useTranslations("admin.partners.form");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => (partner ? toPartnerForm(partner) : emptyPartnerForm(today)));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];

  async function save() {
    if (!partnerFormValid(form)) return;
    await submit.run(async () => {
      try {
        onSaved(partner ? await updatePartner(partner.id, toPartnerRequest(form, partner.rowVersion)) : await createPartner(toPartnerRequest(form)));
      } catch (failure) {
        if (isConflict(failure)) onStale?.();
        throw failure;
      }
    }, partner ? undefined : t("created"));
  }

  return (
    <form
      className="flex min-w-0 flex-col gap-3"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <fieldset disabled={!canEdit} className="m-0 flex min-w-0 flex-col gap-3 border-0 p-0">
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("name")} name="name" value={form.name} onChange={(name) => set({ name })} error={error("name")} />
          <TextField label={t("phone")} name="phone" type="tel" ltr value={form.phone} onChange={(phone) => set({ phone })} error={error("phone")} />
          <TextField label={t("joinDate")} name="joinDate" type="date" ltr value={form.joinDate} onChange={(joinDate) => set({ joinDate })} error={error("joinDate")} />
          <TextField label={t("defaultPercent")} name="defaultPercent" type="number" ltr value={form.defaultPercent} onChange={(defaultPercent) => set({ defaultPercent })} error={error("defaultPercent")} />
        </div>
        <SelectField
          label={t("basis")} name="basis" value={form.basis} onChange={(basis) => set({ basis })} error={error("basis")}
          options={PARTNER_BASES.map((basis) => ({ value: basis, label: t(`bases.${basis}`) }))}
        />
        <p className="m-0 text-xs text-ink-muted">{t(`basisHint.${form.basis}`)}</p>
        <label className={checkbox}>
          <input type="checkbox" checked={form.lossSharingEnabled} onChange={(event) => set({ lossSharingEnabled: event.target.checked })} className="h-5 w-5" />
          {t("lossSharing")}
        </label>
        {partner ? (
          <label className={checkbox}>
            <input type="checkbox" checked={form.isActive} onChange={(event) => set({ isActive: event.target.checked })} className="h-5 w-5" />
            {t("isActive")}
          </label>
        ) : null}
        <TextAreaField label={t("notes")} name="notes" value={form.notes} onChange={(notes) => set({ notes })} rows={2} maxLength={1000} error={error("notes")} />
        <FormMessage message={formMessage(submit.errors, SHOWN_FIELDS)} />
        {canEdit ? (
          <div className="flex flex-wrap gap-2">
            <button type="submit" disabled={submit.busy || !partnerFormValid(form)} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{partner ? t("save") : t("create")}</button>
            {onCancel ? <button type="button" onClick={onCancel} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button> : null}
          </div>
        ) : null}
      </fieldset>
      {submit.dialog}
    </form>
  );
}
