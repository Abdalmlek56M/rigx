"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { DataTable, type Column } from "../kit/DataTable";
import { EditSheet } from "../kit/EditSheet";
import { movementSourceKey } from "./account-rules";
import { fetchMovements, type MoneyAccount, type MoneyMovement } from "./accounts-api";

/** The statement of one account: its newest movements, each with what produced it (a transfer reads as a transfer, in or out). */
export function AccountStatementSheet({ account, onClose }: { account: MoneyAccount | null; onClose: () => void }) {
  const t = useTranslations("admin.accounts.statement");
  const locale = useLocale();
  const accountId = account?.id ?? null;
  const load = useCallback(() => (accountId ? fetchMovements(accountId) : Promise.resolve([] as MoneyMovement[])), [accountId]);
  const { state, reload } = useLoad(load);

  const columns: Column<MoneyMovement>[] = [
    { key: "date", header: t("date"), cell: (movement) => <span dir="ltr" className="whitespace-nowrap text-xs">{formatDate(`${movement.occurredOn}T00:00:00Z`, locale)}</span> },
    {
      key: "source", header: t("source"),
      cell: (movement) => {
        const key = movementSourceKey(movement.source);
        return (
          <span className="flex flex-col items-start gap-0.5">
            <StatusBadge tone={key === "TransferIn" || key === "TransferOut" ? "info" : "neutral"}>{t(`sources.${key}`)}</StatusBadge>
            {movement.reference ? <span dir="ltr" className="font-mono text-xs text-ink-muted">{movement.reference}</span> : null}
          </span>
        );
      }
    },
    { key: "note", header: t("note"), phoneFull: true, cell: (movement) => <span dir="auto" className="[unicode-bidi:isolate]">{movement.note ?? t("none")}</span> },
    {
      key: "amount", header: t("amount"), align: "end",
      cell: (movement) => <span dir="ltr" className={`whitespace-nowrap font-bold ${movement.amount < 0 ? "text-error-text" : ""}`}>{formatEgp(movement.amount, locale)}</span>
    }
  ];

  return (
    <EditSheet open={account !== null} title={t("title", { name: account?.name ?? "" })} onClose={onClose}>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <DataTable label={t("title", { name: account?.name ?? "" })} columns={columns} state={state} onRetry={reload} rowKey={(movement) => movement.id} empty={t("empty")} />
    </EditSheet>
  );
}
