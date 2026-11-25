"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { offeredPrices } from "../categories/category-rules";
import type { Brand } from "../brands/brands-api";
import type { Category } from "../categories/categories-api";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { BrandSelect } from "../stock/BrandSelect";
import { CategorySelect } from "../stock/CategorySelect";
import { SpecFields } from "../stock/SpecFields";
import { WarrantyField } from "../stock/WarrantyField";
import { updateStockItem, type StockItem } from "./inventory-api";
import { itemFormValid, itemModes, modesOk, toItemForm, toUpdateRequest, type StockItemForm as Form } from "./inventory-rules";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const checkbox = "flex min-h-11 items-center gap-2 text-sm font-bold";
const MODES = ["alone", "withBoard", "inBuild"] as const;

/** The item's edit form (model, category, brand, the category's specs, prices, warranty, box, visibility, internal note). Reloads only on a 409, so server field errors stay visible. */
export function StockItemForm({ item, tree, brands, warrantyLabels, canEdit, onSaved, onStale }: {
  item: StockItem;
  tree: readonly Category[];
  brands: readonly Brand[];
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
  const categoryModes = offeredPrices(tree, form.categoryId || null);
  const prices = itemModes(form, categoryModes);
  const setSpecs = useCallback((specs: Form["specs"]) => setForm((current) => ({ ...current, specs })), []);

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
        <BrandSelect brands={brands} tree={tree} categoryId={form.categoryId} value={form.brandId} onChange={(brandId) => set({ brandId })} error={error("brandId")} />
        <SpecFields categoryId={form.categoryId} values={form.specs} onChange={setSpecs} namePrefix="spec" error={error("specs")} />
        <div className="grid items-end gap-3 sm:grid-cols-3">
          <TextField label={t("price")} name="price" type="number" ltr value={form.price} onChange={(price) => set({ price })} error={error("price")} />
          {prices.withBoard ? (
            <TextField label={t("withBoardExtra")} name="withBoardExtra" type="number" ltr value={form.withBoardExtra} onChange={(withBoardExtra) => set({ withBoardExtra })} error={error("withBoardExtra")} />
          ) : null}
          {prices.alone ? (
            <TextField label={t("aloneExtra")} name="aloneExtra" type="number" ltr value={form.aloneExtra} onChange={(aloneExtra) => set({ aloneExtra })} error={error("aloneExtra")} />
          ) : null}
        </div>
        {prices.withBoard || prices.alone ? <p className="m-0 text-xs text-ink-muted">{t("priceHint")}</p> : null}
        <fieldset className="m-0 flex min-w-0 flex-col gap-1 rounded-control border border-line bg-paper p-3">
          <legend className="px-1 text-[13px] font-bold">{t("modes.title")}</legend>
          <p className="m-0 text-xs text-ink-muted">{t("modes.hint")}</p>
          <label className={checkbox}>
            <input
              type="checkbox"
              checked={form.ownModes}
              onChange={(event) => set(event.target.checked ? { ownModes: true, modes: { ...categoryModes } } : { ownModes: false })}
              className="h-5 w-5"
            />
            {t("modes.own")}
          </label>
          <div className="flex flex-wrap gap-x-5 ps-7">
            {MODES.map((mode) => (
              <label key={mode} className={`${checkbox} font-medium ${form.ownModes ? "" : "text-ink-muted"}`}>
                <input
                  type="checkbox"
                  checked={prices[mode]}
                  disabled={!form.ownModes}
                  onChange={(event) => set({ modes: { ...form.modes, [mode]: event.target.checked } })}
                  className="h-5 w-5"
                />
                {t(`modes.${mode}`)}
              </label>
            ))}
          </div>
          <FormMessage message={!modesOk(form) ? t("modes.none") : (error("priceOptions") ?? null)} />
        </fieldset>
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
          <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!itemFormValid(form)}>{t("save")}</Button>
        ) : null}
      </fieldset>
      {submit.dialog}
    </form>
  );
}
