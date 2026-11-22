"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { Button } from "../kit/Button";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { BrandCategoriesField } from "./BrandCategoriesField";
import type { Category } from "../categories/categories-api";
import { brandForm, brandFormValid, emptyBrandForm, slugFromName, toBrandInput, type BrandForm as Form } from "./brand-rules";
import { createBrand, updateBrand, type Brand } from "./brands-api";

/** A brand's name, link part, visibility and the categories it is suggested for. For a new brand the link part follows the name until the owner edits it. */
export function BrandForm({ brand, tree, canManage, onSaved, onStale, onCancel }: {
  brand: Brand | null;
  tree: readonly Category[];
  canManage: boolean;
  onSaved: (saved: Brand) => void;
  onStale: () => void;
  onCancel: () => void;
}) {
  const t = useTranslations("admin.brands");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => (brand ? brandForm(brand) : emptyBrandForm));
  const [slugEdited, setSlugEdited] = useState(brand !== null);
  const valid = brandFormValid(form);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      try {
        onSaved(brand ? await updateBrand(brand.id, toBrandInput(form, brand)) : await createBrand(toBrandInput(form, null)));
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        void save();
      }}
    >
      <fieldset disabled={!canManage} className="m-0 flex min-w-0 flex-col gap-4 border-0 p-0">
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField
            label={t("fields.name")} name="name" value={form.name} required error={submit.errors.fields.name}
            onChange={(name) => set(slugEdited ? { name } : { name, slug: slugFromName(name) })}
          />
          <TextField
            label={t("fields.slug")} name="slug" value={form.slug} ltr required error={submit.errors.fields.slug}
            onChange={(slug) => {
              setSlugEdited(true);
              set({ slug: slug.toLowerCase() });
            }}
          />
        </div>
        <p className="m-0 text-xs text-ink-muted">{t("slugHint")}</p>
        <BrandCategoriesField tree={tree} value={form.categoryIds} onChange={(categoryIds) => set({ categoryIds })} />
        <label className="flex min-h-11 items-center gap-2 text-sm font-bold">
          <input type="checkbox" checked={form.isActive} onChange={(event) => set({ isActive: event.target.checked })} className="h-5 w-5 accent-orange" />
          {t("fields.active")}
        </label>
        <FormMessage message={submit.errors.general} />
        {canManage ? (
          <div className="flex flex-wrap gap-2">
            <Button type="submit" variant="primary" loading={submit.busy} disabled={!valid}>{t("save")}</Button>
            {brand ? null : <Button onClick={onCancel}>{kit("cancel")}</Button>}
          </div>
        ) : null}
      </fieldset>
      {submit.dialog}
    </form>
  );
}
