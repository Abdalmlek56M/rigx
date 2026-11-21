"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { SaveIcon } from "@/components/ui/icons";
import { isConflict } from "@/lib/api/conflict";
import { Button } from "../kit/Button";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { CheckField } from "../settings/CheckField";
import { ACCOUNT_KINDS, accountForm, accountFormValid, accountRequest, emptyAccountForm, type AccountForm } from "./account-rules";
import { createAccount, updateAccount, type MoneyAccount } from "./accounts-api";

/** Add an account, or edit one (its kind is fixed once created, since its movements belong to that kind). */
export function AccountDialog({ account, onSaved, onStale, onClose }: {
  account: MoneyAccount | null; onSaved: () => void; onStale: () => void; onClose: () => void;
}) {
  const t = useTranslations("admin.accounts");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [form, setForm] = useState<AccountForm>(() => (account ? accountForm(account) : emptyAccountForm));
  const set = (patch: Partial<AccountForm>) => setForm((current) => ({ ...current, ...patch }));

  async function save() {
    if (!accountFormValid(form)) return;
    await submit.run(async () => {
      try {
        const body = accountRequest(form, account?.rowVersion ?? null);
        if (account) await updateAccount(account.id, body); else await createAccount(body);
        onSaved();
        onClose();
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  return (
    <Dialog open onClose={onClose} title={account ? t("editTitle", { name: account.name }) : t("newTitle")}>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        <TextField label={t("name")} name="name" value={form.name} onChange={(name) => set({ name })} error={submit.errors.fields.name} required />
        <SelectField
          label={t("kind")} name="kind" value={form.kind} onChange={(kind) => set({ kind })} disabled={account !== null}
          options={ACCOUNT_KINDS.map((value) => ({ value, label: t(`kinds.${value}`) }))}
        />
        <TextField
          label={t("openingBalance")} name="openingBalance" type="number" step="0.01" ltr value={form.openingBalance}
          onChange={(openingBalance) => set({ openingBalance })} error={submit.errors.fields.openingBalance}
        />
        <TextField label={t("sortOrder")} name="sortOrder" type="number" ltr value={form.sortOrder} onChange={(sortOrder) => set({ sortOrder })} />
        <CheckField label={t("active")} hint={t("activeHint")} checked={form.isActive} onChange={(isActive) => set({ isActive })} />
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{kit("cancel")}</Button>
          <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!accountFormValid(form)}>{t("save")}</Button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}
