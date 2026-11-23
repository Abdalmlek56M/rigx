"use client";

import { useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp, joinValues } from "@/lib/format";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { DateField } from "../kit/DateField";
import { TextField } from "@/components/ui/TextField";
import { FormMessage } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { ExpenseTypeDialog } from "./ExpenseTypeDialog";
import { createExpense, uploadExpenseReceipt, type Expense, type ExpenseType, type MoneyAccount, type Partner } from "./expenses-api";
import { emptyExpenseForm, expenseValid, toExpenseCreate, type ExpenseForm as Form } from "./expense-rules";
import { PlusIcon, SaveIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button } from "../kit/Button";
import { ImageDropPicker } from "../kit/ImageDropPicker";

const NEW_EXPENSE_HASH = "#new-expense";

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
  const [form, setForm] = useState<Form>(() => emptyExpenseForm(today));
  const [file, setFile] = useState<File | null>(null);
  const [newType, setNewType] = useState(false);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const activeTypes = types.filter((type) => type.isActive);
  const payerOptions = [
    { value: "", label: t("defaultAccount") },
    ...accounts.map((account) => ({ value: `account:${account.id}`, label: joinValues([account.name, formatEgp(account.balance, locale)], locale) })),
    ...partners.filter((partner) => partner.isActive).map((partner) => ({ value: `partner:${partner.id}`, label: t("partnerMoney", { name: partner.name }) }))
  ];

  const section = useRef<HTMLElement>(null);

  // The dashboard's "quick expense" arrives on `#new-expense`: the form comes into view and the amount takes the focus.
  useEffect(() => {
    if (window.location.hash !== NEW_EXPENSE_HASH) return;
    section.current?.scrollIntoView({ block: "center" });
    section.current?.querySelector<HTMLInputElement>("input[name=amount]")?.focus({ preventScroll: true });
  }, []);

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
      onCreated(created);
    }, t("saved"));
  }

  return (
    <section ref={section} id={NEW_EXPENSE_HASH.slice(1)} className={cardClass({ className: "gap-3 scroll-mt-24" })}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
        <Button icon={PlusIcon} onClick={() => setNewType(true)}>{t("newType")}</Button>
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
          <DateField label={t("date")} name="expenseDate" max={today} value={form.date} onChange={(date) => set({ date })} error={submit.errors.fields.expenseDate} />
          <SelectField
            label={t("category")} name="category" value={form.category} onChange={(category) => set({ category })}
            options={[{ value: "", label: t("pickCategory") }, ...activeTypes.map((type) => ({ value: type.name, label: type.name }))]}
            error={submit.errors.fields.category}
          />
          {canSeeVaults ? <SelectField label={t("paidFrom")} name="paidFrom" value={form.paidFrom} onChange={(paidFrom) => set({ paidFrom })} options={payerOptions} /> : null}
        </div>
        <TextAreaField label={t("note")} name="note" value={form.note} onChange={(note) => set({ note })} rows={2} maxLength={1000} hint={t("noteHint")} />
        <ImageDropPicker label={t("receipt")} hint={t("receiptHint")} file={file} onFile={setFile} />
        <FormMessage message={submit.errors.general} />
        <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!expenseValid(form)}>{t("save")}</Button>
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
