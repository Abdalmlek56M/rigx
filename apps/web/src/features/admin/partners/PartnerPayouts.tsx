"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import type { MoneyAccount } from "../expenses/expenses-api";
import { formMessage } from "../form-message";
import { DataTable, type Column } from "../kit/DataTable";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { MoneyFields } from "./MoneyFields";
import { addPayout, fetchPayouts, type Payout } from "./partners-api";
import { emptyPayoutForm, payMethodKey, payoutValid, toPayoutRequest, type PayoutForm } from "./partner-rules";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const SHOWN_FIELDS = ["amount", "method", "moneyAccountId", "reference", "note"];

/** What was paid out to the partner (with the part set off against an earlier adjustment) and the form to pay one. */
export function PartnerPayouts({ partnerId, accounts, canPost, onChanged }: {
  partnerId: string;
  accounts: readonly MoneyAccount[] | null;
  canPost: boolean;
  onChanged: () => void;
}) {
  const t = useTranslations("admin.partners.payouts");
  const methods = useTranslations("admin.partners.money.methods");
  const locale = useLocale();
  const submit = useSubmit();
  const { state, reload } = useLoad(useCallback(() => fetchPayouts(partnerId), [partnerId]));
  const [form, setForm] = useState<PayoutForm>(emptyPayoutForm);
  const set = (patch: Partial<PayoutForm>) => setForm((current) => ({ ...current, ...patch }));
  const errors = submit.errors.fields;
  const none = <span className="text-ink-muted">{t("none")}</span>;

  async function save() {
    if (!payoutValid(form)) return;
    await submit.run(async () => {
      await addPayout(partnerId, toPayoutRequest(form));
      setForm(emptyPayoutForm());
      reload();
      onChanged();
    }, t("saved"));
  }

  const columns: Column<Payout>[] = [
    { key: "date", header: t("columns.date"), cell: (payout) => <span className="whitespace-nowrap text-xs">{formatDate(payout.createdAtUtc, locale)}</span> },
    { key: "amount", header: t("columns.amount"), align: "end", cell: (payout) => <span className="whitespace-nowrap font-bold">{formatEgp(payout.amount, locale)}</span> },
    {
      key: "offset", header: t("columns.offset"), align: "end",
      cell: (payout) => (payout.offsetApplied > 0 ? <span className="whitespace-nowrap">{formatEgp(payout.offsetApplied, locale)}</span> : none)
    },
    { key: "method", header: t("columns.method"), cell: (payout) => methods(payMethodKey(payout.method)) },
    { key: "reference", header: t("columns.reference"), cell: (payout) => (payout.reference ? <span dir="auto" className="[unicode-bidi:isolate]">{payout.reference}</span> : none) },
    { key: "note", header: t("columns.note"), cell: (payout) => (payout.note ? <span dir="auto" className="[unicode-bidi:isolate]">{payout.note}</span> : none) }
  ];

  return (
    <section className="flex min-w-0 flex-col gap-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <DataTable label={t("title")} columns={columns} state={state} onRetry={reload} rowKey={(payout) => payout.id} empty={t("empty")} />
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
            <MoneyFields form={form} accounts={accounts} prefix="payout" errors={errors} onChange={(patch) => set(patch)} />
            <TextField label={t("reference")} name="payoutReference" ltr value={form.reference} onChange={(reference) => set({ reference })} error={errors.reference} />
            <TextField label={t("note")} name="payoutNote" value={form.note} onChange={(note) => set({ note })} error={errors.note} />
          </div>
          <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
          <FormMessage message={formMessage(submit.errors, SHOWN_FIELDS)} />
          <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!payoutValid(form)}>{t("save")}</Button>
        </form>
      ) : null}
      {submit.dialog}
    </section>
  );
}
