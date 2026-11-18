"use client";

import { useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { FormMessage } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { ExpenseTypeDialog } from "./ExpenseTypeDialog";
import { createExpense, uploadExpenseReceipt, type Expense, type ExpenseType, type MoneyAccount, type Partner } from "./expenses-api";
import { emptyExpenseForm, expenseValid, toExpenseCreate, type ExpenseForm as Form } from "./expense-rules";

/** The daily expense form: amount, type, note, who paid and an optional receipt photo. */
export function ExpenseForm({ types, accounts, partners, canSeeVaults, today, onCreated, onTypesChanged }: {
  types: readonly ExpenseType[];
  accounts: readonly MoneyAccount[];
  partners: readonly Partner[];
  canSeeVaults: boolean;
  today: string;
  onCreated: (expense: Expense) => void;
  onTypesChanged: () => void;
}) {
  const t = useTranslations("admin.expenses.form");
  const locale = useLocale();
  const submit = useSubmit();
  const toast = useToast();
  const input = useRef<HTMLInputElement>(null);
  const [form, setForm] = useState<Form>(() => emptyExpenseForm(today));
  const [file, setFile] = useState<File | null>(null);
  const [newType, setNewType] = useState(false);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const activeTypes = types.filter((type) => type.isActive);
  const payerOptions = [
    { value: "", label: t("defaultAccount") },
    ...accounts.map((account) => ({ value: `account:${account.id}`, label: `${account.name} · ${formatEgp(account.balance, locale)}` })),
    ...partners.filter((partner) => partner.isActive).map((partner) => ({ value: `partner:${partner.id}`, label: t("partnerMoney", { name: partner.name }) }))
  ];

  async function save() {
    if (!expenseValid(form)) return;
    await submit.run(async () => {
      const created = await createExpense(toExpenseCreate(form));
      if (file) {
        try {
          await uploadExpenseReceipt(created.id, file);
        } catch {
          toast(t("receiptFailed"), "error");
        }
      }
      setForm(emptyExpenseForm(today));
      setFile(null);
      if (input.current) input.current.value = "";
      onCreated(created);
    }, t("saved"));
  }

  return (
    <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
        <button type="button" onClick={() => setNewType(true)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("newType")}</button>
      </div>
      <form
        className="flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          void save();
        }}
      >
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("amount")} name="amount" type="number" ltr value={form.amount} onChange={(amount) => set({ amount })} error={submit.errors.fields.amount} />
          <TextField label={t("date")} name="expenseDate" type="date" ltr value={form.date} onChange={(date) => set({ date })} error={submit.errors.fields.expenseDate} />
          <SelectField
            label={t("category")} name="category" value={form.category} onChange={(category) => set({ category })}
            options={[{ value: "", label: t("pickCategory") }, ...activeTypes.map((type) => ({ value: type.name, label: type.name }))]}
            error={submit.errors.fields.category}
          />
          {canSeeVaults ? <SelectField label={t("paidFrom")} name="paidFrom" value={form.paidFrom} onChange={(paidFrom) => set({ paidFrom })} options={payerOptions} /> : null}
        </div>
        <TextAreaField label={t("note")} name="note" value={form.note} onChange={(note) => set({ note })} rows={2} hint={t("noteHint")} />
        <label className="flex flex-col gap-1.5 text-[13px] font-bold">
          {t("receipt")}
          <input
            ref={input} type="file" accept="image/jpeg,image/png,image/webp"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            className="text-sm font-normal file:me-3 file:h-10 file:rounded-control file:border file:border-input-line file:bg-surface file:px-3 file:font-bold"
          />
          <span className="text-xs font-normal text-ink-muted">{t("receiptHint")}</span>
        </label>
        <FormMessage message={submit.errors.general} />
        <button type="submit" disabled={submit.busy || !expenseValid(form)} className="h-11 self-start rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
        {submit.dialog}
      </form>
      {newType ? (
        <ExpenseTypeDialog
          type={null}
          onSaved={(saved) => { set({ category: saved.name }); onTypesChanged(); }}
          onStale={onTypesChanged}
          onClose={() => setNewType(false)}
        />
      ) : null}
    </section>
  );
}
