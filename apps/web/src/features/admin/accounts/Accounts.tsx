"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { PencilIcon, PlusIcon, TrashIcon } from "@/components/ui/icons";
import { toFormErrors } from "../kit/form-errors";
import { useLoad } from "@/lib/hooks/use-load";
import { formatEgp } from "@/lib/format";
import { Button, IconButton } from "../kit/Button";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { DataTable, type Column } from "../kit/DataTable";
import { PageHeader } from "../kit/PageHeader";
import { SectionCard } from "../kit/SectionCard";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { AccountDialog } from "./AccountDialog";
import { activeBalance } from "./account-rules";
import { deleteAccount, fetchAccounts, type MoneyAccount } from "./accounts-api";

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
      cell: (account) => <span className={`text-sm ${account.isActive ? "font-bold text-success" : "text-ink-muted"}`}>{t(account.isActive ? "on" : "off")}</span>
    },
    ...(canManage ? [{
      key: "actions", header: "",
      cell: (account: MoneyAccount) => (
        <span className="flex items-center justify-end gap-1">
          <IconButton icon={PencilIcon} size="sm" label={t("edit", { name: account.name })} onClick={() => setEditing(account)} />
          <IconButton icon={TrashIcon} size="sm" variant="dangerGhost" label={t("delete", { name: account.name })} onClick={() => setDeleting(account)} />
        </span>
      )
    }] : [])
  ];

  const accounts = state.status === "ready" ? state.data : [];

  return (
    <>
      <PageHeader title={t("title")} />
      <SectionCard
        title={t("listTitle")}
        actions={canManage ? <Button variant="primary" icon={PlusIcon} onClick={() => setEditing("new")}>{t("add")}</Button> : null}
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
      <ConfirmDialog
        open={deleting !== null} title={t("deleteTitle", { name: deleting?.name ?? "" })} body={t("deleteBody")} confirmLabel={t("deleteConfirm")} danger
        onClose={() => setDeleting(null)} onConfirm={() => (deleting ? remove(deleting) : Promise.resolve())}
      />
    </>
  );
}
