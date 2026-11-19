"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { createExpenseType, updateExpenseType, type ExpenseType } from "./expenses-api";
import { isConflict } from "@/lib/api/conflict";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex min-h-11 items-center gap-2 text-[13px]">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-4 w-4 accent-orange" />
      {label}
    </label>
  );
}

/** Add or rename an expense type. */
export function ExpenseTypeDialog({ type, onSaved, onStale, onClose }: { type: ExpenseType | null; onSaved: (type: ExpenseType) => void; onStale: () => void; onClose: () => void }) {
  const t = useTranslations("admin.expenses.types");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [name, setName] = useState(type?.name ?? "");
  const [sortOrder, setSortOrder] = useState(String(type?.sortOrder ?? 0));
  const [isActive, setIsActive] = useState(type?.isActive ?? true);
  const sort = Number.parseInt(sortOrder, 10);

  async function save() {
    if (name.trim() === "") return;
    await submit.run(async () => {
      try {
        const body = { name: name.trim(), isActive, sortOrder: Number.isFinite(sort) ? sort : 0, rowVersion: type?.rowVersion ?? null };
        onSaved(type ? await updateExpenseType(type.id, body) : await createExpenseType(body));
        onClose();
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <Dialog open onClose={onClose} title={type ? t("editTitle") : t("newTitle")}>
      <form
        className="flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          void save();
        }}
      >
        <TextField label={t("name")} name="name" value={name} onChange={setName} error={submit.errors.fields.name} required />
        <TextField label={t("sortOrder")} name="sortOrder" type="number" ltr value={sortOrder} onChange={setSortOrder} />
        <Check label={t("active")} checked={isActive} onChange={setIsActive} />
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{kit("cancel")}</Button>
          <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={name.trim() === ""}>{t("save")}</Button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}
