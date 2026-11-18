"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import type { Category } from "../categories/categories-api";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { CategorySelect } from "../stock/CategorySelect";
import { WarrantyField } from "../stock/WarrantyField";
import { updateStockItem, type StockItem } from "./inventory-api";
import { itemFormValid, toItemForm, toUpdateRequest, type StockItemForm as Form } from "./inventory-rules";

const checkbox = "flex min-h-11 items-center gap-2 text-sm font-bold";

/** The item's edit form (model, category, prices, warranty, box, visibility, internal note). Reloads only on a 409, so server field errors stay visible. */
export function StockItemForm({ item, tree, warrantyLabels, canEdit, onSaved, onStale }: {
  item: StockItem;
  tree: readonly Category[];
  warrantyLabels: readonly string[] | null;
  canEdit: boolean;
  onSaved: (item: StockItem) => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.inventory.form");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => toItemForm(item));
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];

  async function save() {
    if (!itemFormValid(form)) return;
    await submit.run(async () => {
      try {
        onSaved(await updateStockItem(item.id, toUpdateRequest(form, item)));
      } catch (failure) {
        if (isConflict(failure)) onStale();
        throw failure;
      }
    });
  }

  return (
    <form
      className="flex flex-col gap-3"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <fieldset disabled={!canEdit} className="m-0 flex min-w-0 flex-col gap-3 border-0 p-0">
        <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
        <TextField label={t("model")} name="modelName" ltr value={form.modelName} onChange={(modelName) => set({ modelName })} error={error("modelName")} />
        <CategorySelect tree={tree} value={form.categoryId} onChange={(categoryId) => set({ categoryId })} error={error("categoryId")} />
        <div className="grid gap-3 sm:grid-cols-3">
          <TextField label={t("price")} name="price" type="number" ltr value={form.price} onChange={(price) => set({ price })} error={error("price")} />
          <TextField label={t("withBoardExtra")} name="withBoardExtra" type="number" ltr value={form.withBoardExtra} onChange={(withBoardExtra) => set({ withBoardExtra })} error={error("withBoardExtra")} />
          <TextField label={t("aloneExtra")} name="aloneExtra" type="number" ltr value={form.aloneExtra} onChange={(aloneExtra) => set({ aloneExtra })} error={error("aloneExtra")} />
        </div>
        <p className="m-0 text-xs text-ink-muted">{t("priceHint")}</p>
        <WarrantyField value={form.warrantyLabel} onChange={(warrantyLabel) => set({ warrantyLabel })} options={warrantyLabels} error={error("warrantyLabel")} />
        <label className={checkbox}>
          <input type="checkbox" checked={form.hasBox} onChange={(event) => set({ hasBox: event.target.checked })} className="h-5 w-5" />
          {t("hasBox")}
        </label>
        <label className={checkbox}>
          <input type="checkbox" checked={form.isHidden} onChange={(event) => set({ isHidden: event.target.checked })} className="h-5 w-5" />
          {t("isHidden")}
        </label>
        <TextAreaField label={t("internalNote")} name="internalNote" value={form.internalNote} onChange={(internalNote) => set({ internalNote })} rows={2} hint={t("internalNoteHint")} error={error("internalNote")} />
        <FormMessage message={submit.errors.general} />
        {canEdit ? (
          <button type="submit" disabled={submit.busy || !itemFormValid(form)} className="h-11 self-start rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
        ) : null}
      </fieldset>
      {submit.dialog}
    </form>
  );
}
