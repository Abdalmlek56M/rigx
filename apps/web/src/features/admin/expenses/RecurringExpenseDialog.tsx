"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp, joinValues } from "@/lib/format";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { isConflict } from "@/lib/api/conflict";
import {
  createRecurringExpense,
  updateRecurringExpense,
  type ExpenseType,
  type MoneyAccount,
  type RecurringExpense
} from "./expenses-api";
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

/** A monthly fixed expense: type, amount, day of the month and the account that pays it. */
export function RecurringExpenseDialog({ template, types, accounts, canSeeVaults, onSaved, onStale, onClose }: {
  template: RecurringExpense | null;
  types: readonly ExpenseType[];
  accounts: readonly MoneyAccount[];
  canSeeVaults: boolean;
  onSaved: () => void;
  onStale: () => void;
  onClose: () => void;
}) {
  const t = useTranslations("admin.expenses.recurring");
  const locale = useLocale();
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const activeTypes = types.filter((type) => type.isActive || type.name === template?.category);
  const [category, setCategory] = useState(template?.category ?? "");
  const [amount, setAmount] = useState(template ? String(template.amount) : "");
  const [day, setDay] = useState(String(template?.dayOfMonth ?? 1));
  const [accountId, setAccountId] = useState(template?.moneyAccountId ?? "");
  const [note, setNote] = useState(template?.note ?? "");
  const [isActive, setIsActive] = useState(template?.isActive ?? true);
  const valid = category !== "" && Number(amount) > 0 && Number(day) >= 1 && Number(day) <= 28;

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      try {
        const body = {
          category,
          amount: Number(amount),
          dayOfMonth: Number(day),
          moneyAccountId: accountId === "" ? null : accountId,
          note: note.trim() === "" ? null : note.trim(),
          isActive,
          startMonth: template?.startMonth ?? null,
          rowVersion: template?.rowVersion ?? null
        };
        if (template) await updateRecurringExpense(template.id, body);
        else await createRecurringExpense(body);
        onSaved();
        onClose();
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <Dialog open onClose={onClose} title={template ? t("editTitle") : t("newTitle")}>
      <form
        className="flex max-h-[70vh] flex-col gap-3 overflow-y-auto pe-1"
        onSubmit={(event) => {
          event.preventDefault();
          void save();
        }}
      >
        <SelectField label={t("category")} name="category" value={category} onChange={setCategory}
          options={[{ value: "", label: t("pickCategory") }, ...activeTypes.map((type) => ({ value: type.name, label: type.name }))]}
          error={submit.errors.fields.category} />
        <div className="grid grid-cols-2 gap-3">
          <TextField label={t("amount")} name="amount" type="number" ltr value={amount} onChange={setAmount} error={submit.errors.fields.amount} />
          <TextField label={t("day")} name="dayOfMonth" type="number" ltr value={day} onChange={setDay} error={submit.errors.fields.dayOfMonth} />
        </div>
        {canSeeVaults ? (
          <SelectField label={t("account")} name="moneyAccountId" value={accountId} onChange={setAccountId}
            options={[{ value: "", label: t("defaultAccount") }, ...accounts.map((account) => ({ value: account.id, label: joinValues([account.name, formatEgp(account.balance, locale)], locale) }))]} />
        ) : null}
        <TextAreaField label={t("note")} name="note" value={note} onChange={setNote} rows={2} maxLength={1000} hint={t("noteHint")} />
        <Check label={t("active")} checked={isActive} onChange={setIsActive} />
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{kit("cancel")}</Button>
          <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!valid}>{t("save")}</Button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}
