"use client";

import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { formatEgp } from "@/lib/format";
import type { MoneyAccount } from "../expenses/expenses-api";
import { PAY_METHODS, type MoneyMoveForm } from "./partner-rules";

/** Amount, method and money box shared by the capital and payout forms (the box list only when the staff member may see the vaults; empty = the method's default box). */
export function MoneyFields({ form, accounts, prefix, errors, onChange }: {
  form: MoneyMoveForm;
  /** Active money boxes, or `null` when the staff member cannot read them. */
  accounts: readonly MoneyAccount[] | null;
  prefix: string;
  errors: Record<string, string>;
  onChange: (patch: Partial<MoneyMoveForm>) => void;
}) {
  const t = useTranslations("admin.partners.money");
  const locale = useLocale();
  return (
    <>
      <TextField label={t("amount")} name={`${prefix}Amount`} type="number" ltr value={form.amount} onChange={(amount) => onChange({ amount })} error={errors.amount} />
      <SelectField
        label={t("method")} name={`${prefix}Method`} value={form.method} onChange={(method) => onChange({ method })} error={errors.method}
        options={PAY_METHODS.map((method) => ({ value: method, label: t(`methods.${method}`) }))}
      />
      {accounts ? (
        <SelectField
          label={t("account")} name={`${prefix}Account`} value={form.accountId} onChange={(accountId) => onChange({ accountId })} error={errors.moneyAccountId}
          options={[{ value: "", label: t("defaultAccount") }, ...accounts.map((account) => ({ value: account.id, label: `${account.name} · ${formatEgp(account.balance, locale)}` }))]}
        />
      ) : null}
    </>
  );
}
