"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { SwapIcon } from "@/components/ui/icons";
import { formatEgp, joinValues } from "@/lib/format";
import { Button } from "../kit/Button";
import { DateField } from "../kit/DateField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { emptyTransferForm, transferRequest, transferValid, type TransferForm } from "./account-rules";
import { transferMoney, type MoneyAccount } from "./accounts-api";

/** Moves money from one account to another on a date. It is recorded as a transfer, never as an expense or an income. */
export function TransferDialog({ accounts, today, onDone, onClose }: { accounts: readonly MoneyAccount[]; today: string; onDone: () => void; onClose: () => void }) {
  const t = useTranslations("admin.accounts.transfer");
  const kit = useTranslations("admin.kit");
  const locale = useLocale();
  const submit = useSubmit();
  const [form, setForm] = useState<TransferForm>(() => emptyTransferForm(today));
  const set = (patch: Partial<TransferForm>) => setForm((current) => ({ ...current, ...patch }));
  const options = (skip: string) => [
    { value: "", label: t("pick") },
    ...accounts.filter((account) => account.id !== skip).map((account) => ({ value: account.id, label: joinValues([account.name, formatEgp(account.balance, locale)], locale) }))
  ];

  const [confirming, setConfirming] = useState(false);
  const nameOf = (id: string) => accounts.find((account) => account.id === id)?.name ?? "";

  async function save() {
    if (!transferValid(form)) return;
    await submit.run(async () => {
      await transferMoney(transferRequest(form));
      onDone();
      onClose();
    }, t("done"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      <ConfirmDialog
        open={confirming}
        title={t("confirm.title")}
        body={t("confirm.body", { amount: formatEgp(Number(form.amount), locale), from: nameOf(form.from), to: nameOf(form.to) })}
        confirmLabel={t("confirm.action")}
        onConfirm={save}
        onClose={() => setConfirming(false)}
      />
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); if (transferValid(form)) setConfirming(true); }}>
        <p className="m-0 text-sm text-ink-muted">{t("hint")}</p>
        <SelectField label={t("from")} name="from" value={form.from} onChange={(from) => set({ from })} options={options(form.to)} error={submit.errors.fields.fromAccountId} />
        <SelectField label={t("to")} name="to" value={form.to} onChange={(to) => set({ to })} options={options(form.from)} error={submit.errors.fields.toAccountId} />
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("amount")} name="amount" type="number" ltr value={form.amount} onChange={(amount) => set({ amount })} error={submit.errors.fields.amount} />
          <DateField label={t("date")} name="transferDate" max={today} value={form.date} onChange={(date) => set({ date })} error={submit.errors.fields.date} />
        </div>
        <TextAreaField label={t("note")} name="note" value={form.note} onChange={(note) => set({ note })} rows={2} maxLength={500} />
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{kit("cancel")}</Button>
          <Button type="submit" variant="primary" icon={SwapIcon} loading={submit.busy} disabled={!transferValid(form)}>{t("save")}</Button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}
