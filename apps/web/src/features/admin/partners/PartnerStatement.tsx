"use client";

import { useCallback, useState } from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { useLoad } from "@/lib/hooks/use-load";
import { StatementSheet } from "./StatementSheet";
import { fetchStatement } from "./partners-api";
import { defaultStatementRange, statementRangeValid, type DateRange } from "./partner-rules";
import { EyeIcon, PrinterIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/**
 * The statement for a date range (this month so far to begin with). "PDF" prints the page's print view: a copy of the statement is added to the document
 * as the only printed content (`data-print-invoice`: A4, white paper, whatever the theme), the way the store's invoice prints.
 */
export function PartnerStatement({ partnerId, today, movements }: { partnerId: string; today: string; movements: number }) {
  const t = useTranslations("admin.partners.statement");
  const [draft, setDraft] = useState<DateRange>(() => defaultStatementRange(today));
  const [applied, setApplied] = useState<DateRange>(draft);
  // `movements` is only a trigger: a new capital entry or payout reads the statement again.
  const { state, reload } = useLoad(useCallback(
    () => fetchStatement(partnerId, applied.from, applied.to),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [partnerId, applied, movements]
  ));

  return (
    <section className="flex min-w-0 flex-col gap-3">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <form
        className="flex flex-wrap items-end gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          if (!statementRangeValid(draft)) return;
          // Same range again = read it again (a deposit or payout may have landed since).
          if (draft.from === applied.from && draft.to === applied.to) reload();
          else setApplied(draft);
        }}
      >
        <TextField label={t("from")} name="statementFrom" type="date" ltr value={draft.from} onChange={(from) => setDraft((current) => ({ ...current, from }))} />
        <TextField label={t("to")} name="statementTo" type="date" ltr value={draft.to} onChange={(to) => setDraft((current) => ({ ...current, to }))} />
        <Button type="submit" icon={EyeIcon} disabled={!statementRangeValid(draft)}>{t("show")}</Button>
        <Button variant="primary" icon={PrinterIcon} disabled={state.status !== "ready"} onClick={() => window.print()}>{t("print")}</Button>
      </form>
      {state.status === "loading" ? <div role="status" aria-label={t("loading")} className="h-40 animate-pulse rounded-control bg-line" /> : null}
      {state.status === "error" ? (
        <div role="alert" className="flex flex-wrap items-center gap-3">
          <span className="font-bold">{t("failed")}</span>
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      ) : null}
      {state.status === "ready" ? (
        <>
          <StatementSheet statement={state.data} />
          {createPortal(
            <main data-print-invoice className="hidden bg-surface p-0 text-ink print:block">
              <StatementSheet statement={state.data} />
            </main>,
            document.body
          )}
        </>
      ) : null}
    </section>
  );
}
