"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { formatEgp, formatMonth } from "@/lib/format";
import { formMessage } from "../form-message";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import { closeMonth, type MonthClose } from "./partners-api";
import { monthValid, previousMonth } from "./partner-rules";

/** Closes a month for the partners paid from the shop's whole profit (the previous month, Cairo, to begin with): confirm first, then shows the result. */
export function MonthClosePanel({ today, onClosed }: { today: string; onClosed: () => void }) {
  const t = useTranslations("admin.partners.monthClose");
  const locale = useLocale();
  const toast = useToast();
  const [initial] = useState(() => previousMonth(today));
  const [year, setYear] = useState(String(initial.year));
  const [month, setMonth] = useState(String(initial.month));
  const [confirming, setConfirming] = useState(false);
  const [result, setResult] = useState<MonthClose | null>(null);
  const valid = monthValid(Number(year), Number(month));

  async function close() {
    try {
      setResult(await closeMonth(Number(year), Number(month)));
      onClosed();
    } catch (error) {
      toast(formMessage(toFormErrors(error, t("failed"))) ?? t("failed"), "error");
      throw error;
    }
  }

  const rows = result
    ? [
        { key: "net", label: t("storeNet"), value: result.storeNetResult },
        { key: "shares", label: t("financedShares"), value: result.financedShares },
        { key: "base", label: t("base"), value: result.base }
      ]
    : [];

  return (
    <section className="flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <p className="m-0 text-sm text-ink-muted">{t("hint")}</p>
      <form
        className="flex flex-wrap items-end gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          if (valid) setConfirming(true);
        }}
      >
        <TextField label={t("month")} name="closeMonth" type="number" ltr value={month} onChange={setMonth} />
        <TextField label={t("year")} name="closeYear" type="number" ltr value={year} onChange={setYear} />
        <button type="submit" disabled={!valid} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("close")}</button>
      </form>
      <ConfirmDialog
        open={confirming} title={t("confirmTitle", { month: Number(month), year: Number(year) })} body={t("confirmBody")} confirmLabel={t("close")}
        onClose={() => setConfirming(false)} onConfirm={close}
      />
      {result ? (
        <div role="status" className="flex flex-col gap-2 rounded-control border border-orange p-3">
          <h3 className="m-0 text-[15px] font-bold">{t("resultTitle", { month: formatMonth(result.month, locale), year: result.month.slice(0, 4) })}</h3>
          <dl className="m-0 flex flex-col">
            {rows.map((row) => (
              <div key={row.key} className="flex items-center justify-between gap-3 border-t border-line py-1.5 first:border-t-0">
                <dt className="text-sm text-ink-muted">{row.label}</dt>
                <dd className="m-0 whitespace-nowrap text-sm font-bold">{formatEgp(row.value, locale)}</dd>
              </div>
            ))}
          </dl>
          {result.posted.length === 0 ? (
            <p className="m-0 text-sm text-ink-muted">{t("nothingPosted")}</p>
          ) : (
            <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
              {result.posted.map((earning) => (
                <li key={earning.id} className="flex items-center justify-between gap-3">
                  <span dir="auto" className="[unicode-bidi:isolate]">{t("posted", { name: earning.partnerName, percent: earning.percent })}</span>
                  <span className="whitespace-nowrap font-bold">{formatEgp(earning.amount, locale)}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </section>
  );
}
