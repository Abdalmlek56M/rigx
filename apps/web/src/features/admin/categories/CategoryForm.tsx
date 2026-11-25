"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import {
  categoryForm,
  categoryFormValid,
  emptyCategoryForm,
  nextSortOrder,
  findPath,
  parentOptions,
  toCategoryInput,
  type CategoryForm as Form
} from "./category-rules";
import { createCategory, updateCategory, type Category } from "./categories-api";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";
import { CategoryIconPicker } from "./CategoryIconPicker";

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex min-h-11 items-center gap-2 text-[13px]">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-4 w-4 accent-orange" />
      {label}
    </label>
  );
}

function ReadOnly({ form, parent }: { form: Form; parent: string | null }) {
  const t = useTranslations("admin.categories");
  return (
    <dl className="m-0 grid gap-2 text-sm sm:grid-cols-2">
      <div><dt className="text-xs text-ink-muted">{t("fields.name")}</dt><dd className="m-0 font-bold">{form.name}</dd></div>
      <div><dt className="text-xs text-ink-muted">{t("fields.nameEn")}</dt><dd dir="ltr" className="m-0 text-start">{form.nameEn || t("none")}</dd></div>
      <div><dt className="text-xs text-ink-muted">{t("fields.slug")}</dt><dd dir="ltr" className="m-0 text-start font-mono text-[13px]">{form.slug}</dd></div>
      <div><dt className="text-xs text-ink-muted">{t("fields.parent")}</dt><dd className="m-0">{parent ?? t("root")}</dd></div>
      <div><dt className="text-xs text-ink-muted">{t("fields.code")}</dt><dd dir="ltr" className="m-0 text-start font-mono text-[13px]">{form.code || t("none")}</dd></div>
      <div className="sm:col-span-2"><dt className="text-xs text-ink-muted">{t("fields.description")}</dt><dd className="m-0">{form.description || t("none")}</dd></div>
    </dl>
  );
}

/** The name/slug/parent/code/description/sort/visibility fields and their save; the image and specs are separate cards. */
export function CategoryForm({ tree, category, parentId, canManage, onSaved, onStale, onCancel }: {
  tree: readonly Category[];
  category: Category | null;
  parentId: string | null;
  canManage: boolean;
  onSaved: (saved: Category) => void;
  onStale: () => void;
  onCancel: () => void;
}) {
  const t = useTranslations("admin.categories");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => category ? categoryForm(category) : emptyCategoryForm(parentId, nextSortOrder(tree, parentId)));
  const parent = category ? findPath(tree, category.id).at(-2)?.name ?? null : parentId ? findPath(tree, parentId).at(-1)?.name ?? null : null;
  const valid = categoryFormValid(form);

  if (!canManage) return <ReadOnly form={form} parent={parent} />;

  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      try {
        const saved = category
          ? await updateCategory(category.id, toCategoryInput(form, category))
          : await createCategory(toCategoryInput(form, null));
        onSaved(saved);
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
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("fields.name")} name="name" value={form.name} onChange={(name) => set({ name })} required error={submit.errors.fields.name} />
        <TextField label={t("fields.nameEn")} name="nameEn" ltr value={form.nameEn} onChange={(nameEn) => set({ nameEn })} error={submit.errors.fields.nameEn} />
        <TextField label={t("fields.slug")} name="slug" value={form.slug} onChange={(slug) => set({ slug })} ltr required error={submit.errors.fields.slug} />
        <SelectField
          label={t("fields.parent")} name="parentId" value={form.parentId} onChange={(next) => set({ parentId: next })}
          options={[
            { value: "", label: t("root") },
            ...parentOptions(tree, category?.id ?? null).map((option) => ({ value: option.id, label: `${"— ".repeat(option.depth)}${option.name}` }))
          ]}
          error={submit.errors.fields.parentId}
        />
        <TextField label={t("fields.code")} name="code" value={form.code} onChange={(code) => set({ code: code.toUpperCase() })} ltr error={submit.errors.fields.code} />
      </div>
      <CategoryIconPicker value={form.iconKey} slug={form.slug} onChange={(iconKey) => set({ iconKey })} />
      <TextAreaField label={t("fields.description")} name="description" value={form.description} onChange={(description) => set({ description })} rows={3} error={submit.errors.fields.description} />
      <div className="flex flex-wrap gap-x-5">
        <Check label={t("fields.active")} checked={form.isActive} onChange={(isActive) => set({ isActive })} />
        <Check label={t("fields.showInMenu")} checked={form.showInMenu} onChange={(showInMenu) => set({ showInMenu })} />
        <Check label={t("fields.showInBuilder")} checked={form.showInBuilder} onChange={(showInBuilder) => set({ showInBuilder })} />
      </div>
      <fieldset className="m-0 flex flex-col gap-1 rounded-control border border-line bg-paper p-3">
        <legend className="px-1 text-[13px] font-bold">{t("fields.priceTitle")}</legend>
        <p className="m-0 text-xs text-ink-muted">{t("fields.priceHint")}</p>
        <div className="flex flex-wrap gap-x-5">
          <Check label={t("fields.priceAlone")} checked={form.priceAlone} onChange={(priceAlone) => set({ priceAlone })} />
          <Check label={t("fields.priceWithBoard")} checked={form.priceWithBoard} onChange={(priceWithBoard) => set({ priceWithBoard })} />
          <Check label={t("fields.priceInBuild")} checked={form.priceInBuild} onChange={(priceInBuild) => set({ priceInBuild })} />
        </div>
        <FormMessage message={submit.errors.fields.priceOptions ?? null} />
      </fieldset>
      <FormMessage message={submit.errors.general} />
      <div className="flex flex-wrap gap-2">
        <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!valid}>{t("save")}</Button>
        {category ? null : <Button variant="ghost" onClick={onCancel}>{kit("cancel")}</Button>}
      </div>
      {submit.dialog}
    </form>
  );
}
