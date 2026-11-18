"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { parentOptions } from "../categories/category-rules";
import type { Category } from "../categories/categories-api";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { FaqActions } from "./FaqActions";
import { createFaq, updateFaq, type Faq } from "./faq-api";
import { emptyFaqForm, faqForm, faqValid, FAQ_SCOPES, toFaqInput, type FaqForm as Form } from "./faq-rules";
import { FaqExclusions } from "./FaqExclusions";
import { isConflict } from "@/lib/api/conflict";

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex min-h-11 items-center gap-2 text-[13px]">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-4 w-4 accent-orange" />
      {label}
    </label>
  );
}

/** The FAQ editor: bilingual question and answer, where it shows, its exclusions, video and order. */
export function FaqForm({ faq, categories, onSaved, onDeleted, onStale, onCancel }: {
  faq: Faq | null;
  categories: readonly Category[];
  onSaved: (faq: Faq) => void;
  onDeleted: () => void;
  onStale: () => void;
  onCancel: () => void;
}) {
  const t = useTranslations("admin.faq");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => faq ? faqForm(faq) : emptyFaqForm());
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const categoriesOptions = parentOptions(categories, null)
    .map((option) => ({ value: option.id, label: `${"— ".repeat(option.depth)}${option.name}` }));

  async function save() {
    if (!faqValid(form)) return;
    await submit.run(async () => {
      try {
        const saved = faq ? await updateFaq(faq.id, faq.rowVersion, toFaqInput(form)) : await createFaq(toFaqInput(form));
        onSaved(saved);
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <div className="flex flex-col gap-4">
      {faq ? <FaqActions faq={faq} onSaved={onSaved} onDeleted={onDeleted} onStale={onStale} /> : null}
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          void save();
        }}
      >
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("fields.questionAr")} name="questionAr" dir="rtl" value={form.questionAr} onChange={(questionAr) => set({ questionAr })} error={submit.errors.fields.questionAr} required />
          <TextField label={t("fields.questionEn")} name="questionEn" value={form.questionEn} onChange={(questionEn) => set({ questionEn })} ltr error={submit.errors.fields.questionEn} required />
          <TextAreaField label={t("fields.answerAr")} name="answerAr" dir="rtl" value={form.answerAr} onChange={(answerAr) => set({ answerAr })} rows={4} error={submit.errors.fields.answerAr} />
          <TextAreaField label={t("fields.answerEn")} name="answerEn" dir="ltr" value={form.answerEn} onChange={(answerEn) => set({ answerEn })} rows={4} error={submit.errors.fields.answerEn} />
          <TextField label={t("fields.topic")} name="topic" value={form.topic} onChange={(topic) => set({ topic })} error={submit.errors.fields.topic} />
          <TextField label={t("fields.sortOrder")} name="sortOrder" type="number" ltr value={form.sortOrder} onChange={(sortOrder) => set({ sortOrder })} error={submit.errors.fields.sortOrder} />
        </div>

        <SelectField
          label={t("fields.scope")} name="scope" value={form.scope} onChange={(scope) => set({ scope })}
          options={FAQ_SCOPES.map((scope) => ({ value: scope, label: t(`scopes.${scope}`) }))}
          error={submit.errors.fields.scope}
        />
        {form.scope === "Category" ? (
          <SelectField
            label={t("fields.category")} name="categoryId" value={form.categoryId} onChange={(categoryId) => set({ categoryId })}
            options={[{ value: "", label: t("fields.pickCategory") }, ...categoriesOptions]} error={submit.errors.fields.categoryId}
          />
        ) : null}
        {form.scope === "Product" ? (
          <TextField label={t("fields.itemCode")} name="itemCode" value={form.itemCode} onChange={(itemCode) => set({ itemCode: itemCode.toUpperCase() })} ltr placeholder="RX-0154" error={submit.errors.fields.itemCode} />
        ) : null}
        {form.scope === "General" ? <Check label={t("fields.helpPageOnly")} checked={form.helpPageOnly} onChange={(helpPageOnly) => set({ helpPageOnly })} /> : null}

        {form.scope === "General" || form.scope === "Category" ? (
          <FaqExclusions
            categories={categories} currentCategoryId={form.scope === "Category" ? form.categoryId : ""}
            categoryIds={form.excludedCategoryIds} itemCodes={form.excludedItemCodes}
            onChange={(change) => set(change)}
          />
        ) : null}

        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("fields.youTube")} name="youTubeUrl" value={form.youTubeUrl} onChange={(youTubeUrl) => set({ youTubeUrl })} ltr error={submit.errors.fields.youTubeUrl} />
          <p className="m-0 self-end pb-3 text-xs text-ink-muted">{t("fields.youTubeHint")}</p>
        </div>

        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap gap-2">
          <button type="submit" disabled={submit.busy || !faqValid(form)} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
          {faq ? null : <button type="button" onClick={onCancel} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{kit("cancel")}</button>}
        </div>
        {submit.dialog}
      </form>
    </div>
  );
}
