"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { DocumentIcon, PencilIcon, PlusIcon, SwapIcon, TrashIcon } from "@/components/ui/icons";
import { toFormErrors } from "../kit/form-errors";
import { useLoad } from "@/lib/hooks/use-load";
import { formatEgp } from "@/lib/format";
import { Button, IconButton } from "../kit/Button";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { PageHeader } from "../kit/PageHeader";
import { SectionCard } from "../kit/SectionCard";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { cairoToday } from "../dashboard/dashboard-rules";
import { AccountDialog } from "./AccountDialog";
import { AccountStatementSheet } from "./AccountStatementSheet";
import { TransferDialog } from "./TransferDialog";
import { activeBalance } from "./account-rules";
import { deleteAccount, fetchAccounts, type MoneyAccount } from "./accounts-api";
import { StatusBadge } from "@/components/ui/StatusBadge";

/** ADM-Accounts: the places money is kept (cash drawer, InstaPay, wallet, bank) with their balances; add, edit, delete or switch off. */
export function Accounts() {
  const t = useTranslations("admin.accounts");
  const locale = useLocale();
  const toast = useToast();
  const { has } = usePermissions();
  const canManage = has("Finance.Disburse");
  const { state, reload } = useLoad(useCallback(() => fetchAccounts(), []));
  const [editing, setEditing] = useState<MoneyAccount | "new" | null>(null);
  const [deleting, setDeleting] = useState<MoneyAccount | null>(null);
  const [transferring, setTransferring] = useState(false);
  const [statementOf, setStatementOf] = useState<MoneyAccount | null>(null);
  const [today] = useState(() => cairoToday(new Date()));

  async function remove(account: MoneyAccount) {
    try {
      await deleteAccount(account.id);
      toast(t("deleted"));
      reload();
    } catch (error) {
      toast(toFormErrors(error, t("deleteFailed")).general ?? t("deleteFailed"), "error");
      throw error;
    }
  }

  const columns: Column<MoneyAccount>[] = [
    { key: "name", header: t("name"), cell: (account) => <span className="font-bold">{account.name}</span> },
    { key: "kind", header: t("kind"), cell: (account) => t(`kinds.${account.kind as "Cash"}`) },
    { key: "balance", header: t("balance"), align: "end", cell: (account) => <span className="whitespace-nowrap font-bold">{formatEgp(account.balance, locale)}</span> },
    {
      key: "status", header: t("status"),
      cell: (account) => <StatusBadge tone={account.isActive ? "success" : "neutral"}>{t(account.isActive ? "on" : "off")}</StatusBadge>
    },
    actionsColumn((account) => (
      <span className="flex items-center gap-1">
        <IconButton icon={DocumentIcon} size="sm" label={t("statement.open", { name: account.name })} onClick={() => setStatementOf(account)} />
        {canManage ? <IconButton icon={PencilIcon} size="sm" label={t("edit", { name: account.name })} onClick={() => setEditing(account)} /> : null}
        {canManage ? <IconButton icon={TrashIcon} size="sm" variant="dangerGhost" label={t("delete", { name: account.name })} onClick={() => setDeleting(account)} /> : null}
      </span>
    ))
  ];

  const accounts = state.status === "ready" ? state.data : [];
  const usable = accounts.filter((account) => account.isActive);

  return (
    <>
      <PageHeader title={t("title")} />
      <SectionCard
        title={t("listTitle")}
        actions={canManage ? (
          <>
            <Button icon={SwapIcon} disabled={usable.length < 2} onClick={() => setTransferring(true)}>{t("transfer.open")}</Button>
            <Button variant="primary" icon={PlusIcon} onClick={() => setEditing("new")}>{t("add")}</Button>
          </>
        ) : null}
      >
        <p className="m-0 text-sm text-ink-muted">{t("hint")}</p>
        <DataTable label={t("title")} columns={columns} state={state} onRetry={reload} rowKey={(account) => account.id} empty={t("empty")} />
        {accounts.length > 0 ? (
          <div className="flex flex-col gap-0.5 border-t border-line pt-3">
            <span className="text-xs text-ink-muted">{t("total")}</span>
            <strong className="text-lg">{formatEgp(activeBalance(accounts), locale)}</strong>
          </div>
        ) : null}
      </SectionCard>
      {editing ? (
        <AccountDialog account={editing === "new" ? null : editing} onSaved={reload} onStale={reload} onClose={() => setEditing(null)} />
      ) : null}
      {transferring ? <TransferDialog accounts={usable} today={today} onDone={reload} onClose={() => setTransferring(false)} /> : null}
      <AccountStatementSheet account={statementOf} onClose={() => setStatementOf(null)} />
      <ConfirmDialog
        open={deleting !== null} title={t("deleteTitle", { name: deleting?.name ?? "" })} body={t("deleteBody")} confirmLabel={t("deleteConfirm")} danger
        onClose={() => setDeleting(null)} onConfirm={() => (deleting ? remove(deleting) : Promise.resolve())}
      />
    </>
  );
}
