"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { SelectField } from "@/components/ui/SelectField";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import type { MoneyAccount } from "../expenses/expenses-api";
import { formMessage } from "../form-message";
import { DataTable, type Column } from "../kit/DataTable";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { MoneyFields } from "./MoneyFields";
import { addCapital, fetchCapital, type CapitalEntry } from "./partners-api";
import { capitalKindKey, capitalValid, CAPITAL_KINDS, emptyCapitalForm, payMethodKey, toCapitalRequest, type CapitalForm } from "./partner-rules";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const SHOWN_FIELDS = ["kind", "amount", "entryDate", "method", "moneyAccountId", "note"];

/** The partner's capital movements (append-only: deposits and withdrawals are never edited or deleted) and the form to add one. */
export function PartnerCapital({ partnerId, today, accounts, canPost, onChanged }: {
  partnerId: string;
  today: string;
  accounts: readonly MoneyAccount[] | null;
  canPost: boolean;
  onChanged: () => void;
}) {
  const t = useTranslations("admin.partners.capital");
  const methods = useTranslations("admin.partners.money.methods");
  const locale = useLocale();
  const submit = useSubmit();
  const { state, reload } = useLoad(useCallback(() => fetchCapital(partnerId), [partnerId]));
  const [form, setForm] = useState<CapitalForm>(() => emptyCapitalForm(today));
  const set = (patch: Partial<CapitalForm>) => setForm((current) => ({ ...current, ...patch }));
  const errors = submit.errors.fields;

  async function save() {
    if (!capitalValid(form)) return;
    await submit.run(async () => {
      await addCapital(partnerId, toCapitalRequest(form));
      setForm(emptyCapitalForm(today));
      reload();
      onChanged();
    }, t("saved"));
  }

  const columns: Column<CapitalEntry>[] = [
    { key: "date", header: t("columns.date"), cell: (entry) => <span className="whitespace-nowrap text-xs">{formatDate(entry.entryDate, locale)}</span> },
    { key: "kind", header: t("columns.kind"), cell: (entry) => <span className="whitespace-nowrap">{t(`kinds.${capitalKindKey(entry.kind)}`)}</span> },
    {
      key: "amount", header: t("columns.amount"), align: "end",
      cell: (entry) => <span className={`whitespace-nowrap font-bold ${entry.kind === "Withdrawal" ? "text-error-text" : ""}`}>{formatEgp(entry.amount, locale)}</span>
    },
    { key: "method", header: t("columns.method"), cell: (entry) => (entry.method ? methods(payMethodKey(entry.method)) : <span className="text-ink-muted">{t("none")}</span>) },
    { key: "note", header: t("columns.note"), cell: (entry) => (entry.note ? <span dir="auto" className="[unicode-bidi:isolate]">{entry.note}</span> : <span className="text-ink-muted">{t("none")}</span>) }
  ];

  return (
    <section className="flex min-w-0 flex-col gap-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <DataTable label={t("title")} columns={columns} state={state} onRetry={reload} rowKey={(entry) => entry.id} empty={t("empty")} />
      {canPost ? (
        <form
          className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3"
          onSubmit={(event) => {
            event.preventDefault();
            void save();
          }}
        >
          <h4 className="m-0 text-sm font-bold">{t("formTitle")}</h4>
          <div className="grid gap-3 sm:grid-cols-2">
            <SelectField
              label={t("kind")} name="capitalKind" value={form.kind} onChange={(kind) => set({ kind })} error={errors.kind}
              options={CAPITAL_KINDS.map((kind) => ({ value: kind, label: t(`kinds.${kind}`) }))}
            />
            <TextField label={t("date")} name="capitalDate" type="date" ltr value={form.date} onChange={(date) => set({ date })} error={errors.entryDate} />
            <MoneyFields form={form} accounts={accounts} prefix="capital" errors={errors} onChange={(patch) => set(patch)} />
            <TextField label={t("note")} name="capitalNote" value={form.note} onChange={(note) => set({ note })} error={errors.note} />
          </div>
          <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
          <FormMessage message={formMessage(submit.errors, SHOWN_FIELDS)} />
          <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!capitalValid(form)}>{t("save")}</Button>
        </form>
      ) : null}
      {submit.dialog}
    </section>
  );
}
