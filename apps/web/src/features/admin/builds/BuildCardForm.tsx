"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { USAGES } from "@/lib/usage";
import { formMessage } from "../form-message";
import { FormMessage } from "../kit/PageHeader";
import { WarrantyField } from "../stock/WarrantyField";
import { editBuild } from "./builds-api";
import { cardValid, SELL_MODES, toCardForm, toEditRequest, type CardForm } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";

/** The server field names this form shows next to their inputs; any other field error goes to the form message. */
const SHOWN_FIELDS = ["name", "publicTitleAr", "publicTitleEn", "salePrice", "usage", "sellMode", "warrantyLabel", "copies", "suitableForAr", "suitableForEn"];

/** The build's card: name, public titles (each in its own direction), sale price, usage, sell mode, warranty, copies and "suitable for". */
export function BuildCardForm({ warrantyLabels, canEdit, ...action }: BuildActionProps & { warrantyLabels: readonly string[] | null; canEdit: boolean }) {
  const t = useTranslations("admin.builds.card");
  const usage = useTranslations("usage");
  const { build } = action;
  const { submit, run } = useBuildAction(action);
  const [form, setForm] = useState<CardForm>(() => toCardForm(build));
  const set = (patch: Partial<CardForm>) => setForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];

  async function save() {
    if (!cardValid(form)) return;
    await run(() => editBuild(build.id, toEditRequest(form, build)));
  }

  return (
    <form
      className="flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <fieldset disabled={!canEdit} className="m-0 flex min-w-0 flex-col gap-3 border-0 p-0">
        <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
        <TextField label={t("name")} name="name" value={form.name} onChange={(name) => set({ name })} error={error("name")} />
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("titleAr")} name="publicTitleAr" dir="rtl" value={form.publicTitleAr} onChange={(publicTitleAr) => set({ publicTitleAr })} error={error("publicTitleAr")} />
          <TextField label={t("titleEn")} name="publicTitleEn" dir="ltr" value={form.publicTitleEn} onChange={(publicTitleEn) => set({ publicTitleEn })} error={error("publicTitleEn")} />
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("salePrice")} name="salePrice" type="number" ltr value={form.salePrice} onChange={(salePrice) => set({ salePrice })} error={error("salePrice")} placeholder={t("salePriceHint")} />
          <TextField label={t("copies")} name="copies" type="number" ltr value={form.copies} onChange={(copies) => set({ copies })} error={error("copies")} />
          <SelectField
            label={t("usage")} name="usage" value={form.usage} onChange={(next) => set({ usage: next })} error={error("usage")}
            options={[{ value: "", label: t("noUsage") }, ...USAGES.map((entry) => ({ value: entry, label: usage(entry) })), ...(form.usage && !USAGES.some((entry) => entry === form.usage) ? [{ value: form.usage, label: form.usage }] : [])]}
          />
          <SelectField
            label={t("sellMode")} name="sellMode" value={form.sellMode} onChange={(sellMode) => set({ sellMode })} error={error("sellMode")}
            options={SELL_MODES.map((mode) => ({ value: mode, label: t(`sellModes.${mode}`) }))}
          />
        </div>
        <WarrantyField value={form.warrantyLabel} onChange={(warrantyLabel) => set({ warrantyLabel })} options={warrantyLabels} error={error("warrantyLabel")} emptyLabel={t("eachPartWarranty")} />
        <p className="m-0 text-xs text-ink-muted">{t("warrantyHint")}</p>
        <TextAreaField label={t("suitableAr")} name="suitableForAr" dir="rtl" value={form.suitableForAr} onChange={(suitableForAr) => set({ suitableForAr })} rows={2} error={error("suitableForAr")} />
        <TextAreaField label={t("suitableEn")} name="suitableForEn" dir="ltr" value={form.suitableForEn} onChange={(suitableForEn) => set({ suitableForEn })} rows={2} error={error("suitableForEn")} />
        <FormMessage message={formMessage(submit.errors, SHOWN_FIELDS)} />
        {canEdit ? <button type="submit" disabled={submit.busy || !cardValid(form)} className="h-11 self-start rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button> : null}
      </fieldset>
      {submit.dialog}
    </form>
  );
}
